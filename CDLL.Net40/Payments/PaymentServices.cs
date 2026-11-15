// =====================================================================================
// EN: Builds the payment services from app.config, in one place:
//       TerminalMode            Real (madaapi + ECR.dll) | Simulator (fake Geidea + NamiPay terminals, NO real payments)
//       TerminalProvider        Geidea | NamiPay: used when this PC has no terminal mapping
//       NamiPay.CashRegisterNumber / NamiPay.TerminalPrinter   NamiPay defaults (8 digits / print on terminal)
//       TerminalTimeoutSeconds  watchdog for one payment / refund / repeat / reconciliation call (default 120)
//       TerminalStatusTimeoutSeconds  watchdog for Check status / Terminal ID / Merchant info (default 20)
//       Storage.Provider        Sqlite | SqlServer | Json
//       Storage.DataFolder      SQLite file, JSON files, pending\ fallback and logs\ (default %ProgramData%\CDLL\Payments)
//       connectionStrings "PaymentsSqlServer"  used when Storage.Provider = SqlServer
//     Screens receive a PaymentServices object and never create services themselves.
// AR: يبني خدمات الدفع من app.config في مكان واحد. الشاشات تستلم كائن PaymentServices ولا تنشئ الخدمات بنفسها.
// =====================================================================================

using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using CDLL.Payments.Integration;
using CDLL.Payments.Storage;
using CDLL.Payments.Terminal;
using CDLL.Payments.Terminal.Skyband;

namespace CDLL.Payments
{
    public sealed class PaymentServices : IDisposable
    {
        public PaymentServices(IPaymentTerminalService terminal, PaymentStorage storage, IHisNotifier his, bool isSimulator, ITerminalDetector detector = null)
        {
            if (terminal == null) throw new ArgumentNullException("terminal");
            if (storage == null) throw new ArgumentNullException("storage");
            Terminal = terminal;
            Storage = storage;
            His = his ?? new NullHisNotifier();
            IsSimulator = isSimulator;
            Detector = detector ?? terminal as ITerminalDetector;
        }

        /// <summary>EN: Geidea + NamiPay behind one interface (chosen by connection.Provider).  AR: Geidea و NamiPay خلف واجهة واحدة.</summary>
        public IPaymentTerminalService Terminal { get; private set; }
        public PaymentStorage Storage { get; private set; }
        public IHisNotifier His { get; private set; }
        public bool IsSimulator { get; private set; }

        /// <summary>EN: "Which provider is on this port?" (mapping screen).  AR: "أي مزوّد على هذا المنفذ؟".</summary>
        public ITerminalDetector Detector { get; private set; }

        public static PaymentServices FromConfig()
        {
            bool simulator = string.Equals(Setting("TerminalMode", "Real"), "Simulator", StringComparison.OrdinalIgnoreCase);
            IMadaNativeApi api = simulator ? (IMadaNativeApi)new SimulatorMadaApi() : new NativeMadaApi();

            TimeSpan timeout = TimeSpan.FromSeconds(Seconds("TerminalTimeoutSeconds", 120, 10, 600));
            TimeSpan statusTimeout = TimeSpan.FromSeconds(Seconds("TerminalStatusTimeoutSeconds", 20, 5, 120));
            OutcomeClassifier classifier = OutcomeClassifier.FromConfig();
            string dataFolder = Environment.ExpandEnvironmentVariables(Setting("Storage.DataFolder", @"%ProgramData%\CDLL\Payments"));

            // EN: Real terminal: check the COM port before each USB call (the simulator has no real port). Both providers
            //     share ONE gate: they may be on the same COM port.
            // AR: الجهاز الفعلي: فحص منفذ COM قبل كل استدعاء. المزودان يشتركان في قفل واحد لأنهما قد يستخدمان نفس المنفذ.
            IComPortProbe probe = simulator ? null : new SerialPortProbe();
            var gate = new System.Threading.SemaphoreSlim(1, 1);

            var geidea = new MadaTerminalService(api, classifier, timeout, statusTimeout, probe, gate);

            var skybandSimulator = new SkybandSimulator();
            ISkybandCodec codec = simulator ? (ISkybandCodec)skybandSimulator : new NativeSkybandCodec();
            ISkybandLink link = simulator ? (ISkybandLink)skybandSimulator : new SkybandLink();
            bool terminalPrinter = string.Equals(Setting("NamiPay.TerminalPrinter", "false"), "true", StringComparison.OrdinalIgnoreCase);
            var namiPay = new SkybandTerminalService(codec, link, new SkybandStateStore(dataFolder), classifier,
                timeout, statusTimeout, probe, gate, terminalPrinter);

            var terminal = new ProviderTerminalService(geidea, namiPay);
            ITransactionStore store = CreateStore(Setting("Storage.Provider", "Sqlite"), dataFolder);

            return new PaymentServices(terminal, new PaymentStorage(store, dataFolder), new NullHisNotifier(), simulator, terminal);
        }

        /// <summary>EN: Provider used when this PC has no terminal mapping (app.config "TerminalProvider").  AR: المزوّد عند عدم وجود ربط.</summary>
        public static TerminalProvider DefaultProvider
        {
            get
            {
                TerminalProvider provider;
                return Enum.TryParse(Setting("TerminalProvider", "Geidea"), true, out provider) ? provider : TerminalProvider.Geidea;
            }
        }

        /// <summary>EN: NamiPay cash register number used when the mapping has none.  AR: رقم الصندوق عند عدم وجوده في الربط.</summary>
        public static string DefaultCashRegisterNumber
        {
            get { return Setting("NamiPay.CashRegisterNumber", ""); }
        }

        public static ITransactionStore CreateStore(string provider, string dataFolder)
        {
            switch ((provider ?? "").Trim().ToLowerInvariant())
            {
                case "sqlite":
                    return new SqliteTransactionStore(Path.Combine(dataFolder, Setting("Storage.SqliteFile", "payments.db")));

                case "sqlserver":
                    ConnectionStringSettings connection = ConfigurationManager.ConnectionStrings["PaymentsSqlServer"];
                    if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
                        throw new ConfigurationErrorsException("Storage.Provider is SqlServer but connectionStrings \"PaymentsSqlServer\" is missing. / نص الاتصال PaymentsSqlServer غير موجود.");
                    return new SqlServerTransactionStore(connection.ConnectionString);

                case "json":
                    return new JsonTransactionStore(Path.Combine(dataFolder, "json"));

                default:
                    throw new ConfigurationErrorsException("Storage.Provider must be Sqlite, SqlServer or Json. / يجب أن يكون Storage.Provider أحد: Sqlite أو SqlServer أو Json.");
            }
        }

        private static int Seconds(string key, int fallback, int min, int max)
        {
            int value;
            if (!int.TryParse(Setting(key, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.None, CultureInfo.InvariantCulture, out value))
                value = fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        private static string Setting(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        public void Dispose()
        {
            Storage.Dispose();
        }
    }
}
