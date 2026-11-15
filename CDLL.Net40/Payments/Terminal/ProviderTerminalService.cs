// =====================================================================================
// EN: The service the screens use: sends every call to the provider of the terminal (connection.Provider):
//       Geidea  -> MadaTerminalService      (madaapi DLLs)
//       NamiPay -> SkybandTerminalService   (SkyBand ECR.dll)
//     Both share the same pipeline, gate, storage and audit, so Form1 / the mapping screen never care which
//     company the terminal belongs to.
//     DetectAsync(): "which company is on this port?" -> tries Geidea Check status, then NamiPay Check status.
// AR: الخدمة التي تستخدمها الشاشات: ترسل كل استدعاء إلى مزوّد الجهاز (connection.Provider): Geidea أو NamiPay.
//     الاثنان يشتركان في نفس المسار والقفل والتخزين والتدقيق، لذلك لا تهتم الشاشات بشركة الجهاز.
//     DetectAsync(): "أي شركة على هذا المنفذ؟" -> يجرّب فحص حالة Geidea ثم فحص حالة NamiPay.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDLL.Payments.Terminal
{
    /// <summary>EN: Result of "which provider is connected".  AR: نتيجة "أي مزوّد متصل".</summary>
    public sealed class ProviderDetection
    {
        /// <summary>EN: null = neither answered.  AR: null = لم يرد أي منهما.</summary>
        public TerminalProvider? Provider { get; set; }

        /// <summary>EN: Every Check status that was tried (each one is saved like any operation).  AR: كل فحوصات الحالة التي جُرّبت.</summary>
        public IList<TerminalResult> Attempts { get; } = new List<TerminalResult>();

        public string Describe()
        {
            if (Provider.HasValue)
                return "Detected / تم الكشف: " + Provider.Value;
            var parts = new List<string>();
            foreach (TerminalResult attempt in Attempts)
                parts.Add(attempt.Provider + ": " + attempt.Outcome + (attempt.ErrorMessage == null ? "" : " (" + attempt.ErrorMessage + ")"));
            return "No Geidea or NamiPay terminal answered / لم يرد أي جهاز Geidea أو NamiPay. " + string.Join(" | ", parts);
        }
    }

    public interface ITerminalDetector
    {
        Task<ProviderDetection> DetectAsync(TerminalConnection connection);
    }

    public sealed class ProviderTerminalService : IPaymentTerminalService, ITerminalDetector
    {
        private readonly IPaymentTerminalService _geidea;
        private readonly IPaymentTerminalService _namiPay;

        public ProviderTerminalService(IPaymentTerminalService geidea, IPaymentTerminalService namiPay)
        {
            if (geidea == null) throw new ArgumentNullException("geidea");
            if (namiPay == null) throw new ArgumentNullException("namiPay");
            _geidea = geidea;
            _namiPay = namiPay;
        }

        public Task<TerminalResult> CheckStatusAsync(TerminalConnection connection) { return For(connection).CheckStatusAsync(connection); }
        public Task<TerminalResult> PurchaseAsync(TerminalConnection connection, decimal amountSar) { return For(connection).PurchaseAsync(connection, amountSar); }
        public Task<TerminalResult> RefundAsync(TerminalConnection connection, decimal amountSar, string originalRrn, DateTime originalDate) { return For(connection).RefundAsync(connection, amountSar, originalRrn, originalDate); }
        public Task<TerminalResult> RepeatLastAsync(TerminalConnection connection) { return For(connection).RepeatLastAsync(connection); }
        public Task<TerminalResult> ReconcileAsync(TerminalConnection connection) { return For(connection).ReconcileAsync(connection); }
        public Task<TerminalResult> GetMerchantInfoAsync(TerminalConnection connection) { return For(connection).GetMerchantInfoAsync(connection); }
        public Task<TerminalResult> GetTerminalIdAsync(TerminalConnection connection) { return For(connection).GetTerminalIdAsync(connection); }

        /// <summary>
        /// EN: Geidea first (its DLL gives up after ~5 s on a non-Geidea terminal), then NamiPay (needs the cash
        ///     register number; it registers + starts a session + checks status).
        /// AR: Geidea أولاً (مكتبته تتوقف بعد ~5 ثوانٍ مع جهاز آخر)، ثم NamiPay (يحتاج رقم الصندوق).
        /// </summary>
        public async Task<ProviderDetection> DetectAsync(TerminalConnection connection)
        {
            var detection = new ProviderDetection();

            TerminalResult geidea = await _geidea.CheckStatusAsync(As(connection, TerminalProvider.Geidea));
            detection.Attempts.Add(geidea);
            if (geidea.IsSuccess)
            {
                detection.Provider = TerminalProvider.Geidea;
                return detection;
            }

            TerminalResult namiPay = await _namiPay.CheckStatusAsync(As(connection, TerminalProvider.NamiPay));
            detection.Attempts.Add(namiPay);
            if (namiPay.IsSuccess)
                detection.Provider = TerminalProvider.NamiPay;
            return detection;
        }

        private IPaymentTerminalService For(TerminalConnection connection)
        {
            return connection != null && connection.Provider == TerminalProvider.NamiPay ? _namiPay : _geidea;
        }

        private static TerminalConnection As(TerminalConnection c, TerminalProvider provider)
        {
            if (c == null)
                return null;
            return new TerminalConnection
            {
                Transport = c.Transport, ComPort = c.ComPort, BaudRate = c.BaudRate, Parity = c.Parity, DataBits = c.DataBits,
                StopBits = c.StopBits, TcpIp = c.TcpIp, TcpPort = c.TcpPort, CashRegisterNumber = c.CashRegisterNumber,
                Provider = provider
            };
        }
    }
}
