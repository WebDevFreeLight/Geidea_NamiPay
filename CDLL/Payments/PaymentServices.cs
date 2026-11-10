// =====================================================================================
// EN: Builds the payment services from app.config, in one place:
//       TerminalMode            Real (madaapi DLLs) | Simulator (fake terminal, NO real payments)
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

namespace CDLL.Payments
{
    public sealed class PaymentServices : IDisposable
    {
        public PaymentServices(IPaymentTerminalService terminal, PaymentStorage storage, IHisNotifier his, bool isSimulator)
        {
            if (terminal == null) throw new ArgumentNullException("terminal");
            if (storage == null) throw new ArgumentNullException("storage");
            Terminal = terminal;
            Storage = storage;
            His = his ?? new NullHisNotifier();
            IsSimulator = isSimulator;
        }

        public IPaymentTerminalService Terminal { get; private set; }
        public PaymentStorage Storage { get; private set; }
        public IHisNotifier His { get; private set; }
        public bool IsSimulator { get; private set; }

        public static PaymentServices FromConfig()
        {
            bool simulator = string.Equals(Setting("TerminalMode", "Real"), "Simulator", StringComparison.OrdinalIgnoreCase);
            IMadaNativeApi api = simulator ? (IMadaNativeApi)new SimulatorMadaApi() : new NativeMadaApi();

            int timeoutSeconds = Seconds("TerminalTimeoutSeconds", 120, 10, 600);
            int statusTimeoutSeconds = Seconds("TerminalStatusTimeoutSeconds", 20, 5, 120);

            // EN: Real terminal: check the COM port before each USB call. The simulator has no real port.
            // AR: الجهاز الفعلي: فحص منفذ COM قبل كل استدعاء USB. المحاكي ليس له منفذ فعلي.
            var terminal = new MadaTerminalService(api, OutcomeClassifier.FromConfig(),
                TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromSeconds(statusTimeoutSeconds),
                simulator ? null : new SerialPortProbe());

            string dataFolder = Environment.ExpandEnvironmentVariables(Setting("Storage.DataFolder", @"%ProgramData%\CDLL\Payments"));
            ITransactionStore store = CreateStore(Setting("Storage.Provider", "Sqlite"), dataFolder);

            return new PaymentServices(terminal, new PaymentStorage(store, dataFolder), new NullHisNotifier(), simulator);
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
