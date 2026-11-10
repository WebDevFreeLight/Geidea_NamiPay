// =====================================================================================
// EN: Builds the payment services from app.config, in one place:
//       TerminalMode            Real (madaapi DLLs) | Simulator (fake terminal, NO real payments)
//       TerminalTimeoutSeconds  watchdog for one terminal call (default 120)
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

            int timeoutSeconds;
            if (!int.TryParse(Setting("TerminalTimeoutSeconds", "120"), NumberStyles.None, CultureInfo.InvariantCulture, out timeoutSeconds))
                timeoutSeconds = 120;
            timeoutSeconds = Math.Max(10, Math.Min(600, timeoutSeconds));

            var terminal = new MadaTerminalService(api, OutcomeClassifier.FromConfig(), TimeSpan.FromSeconds(timeoutSeconds));

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
