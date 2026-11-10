// =====================================================================================
// EN: Data types shared by the payment pipeline:
//       - how to reach a terminal (TerminalConnection) and which terminal belongs to which PC (TerminalMapping),
//       - what an operation returned (TerminalResult) and how it ended (TransactionOutcome),
//       - the rows saved to SQLite / SQL Server / JSON (PaymentTransactionRecord, ErrorLogRecord).
//     SECURITY: card data is stored MASKED only (first 6 + last 4 digits). The expiry date is never stored.
// AR: الأنواع المشتركة في مسار الدفع:
//       - كيفية الوصول إلى الجهاز (TerminalConnection) وأي جهاز مرتبط بأي حاسوب (TerminalMapping)،
//       - ما أعادته العملية (TerminalResult) وكيف انتهت (TransactionOutcome)،
//       - السجلات المحفوظة في SQLite / SQL Server / JSON (PaymentTransactionRecord و ErrorLogRecord).
//     الأمان: بيانات البطاقة تُحفظ مُقنّعة فقط (أول 6 وآخر 4 أرقام)، وتاريخ انتهاء البطاقة لا يُحفظ أبداً.
// =====================================================================================

using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;

namespace CDLL.Payments
{
    /// <summary>EN: How the PC talks to the terminal.  AR: طريقة اتصال الحاسوب بالجهاز.</summary>
    public enum TerminalTransport { Com, Tcp }

    /// <summary>EN: The operation that was run.  AR: العملية التي تم تنفيذها.</summary>
    public enum TerminalOperation { CheckStatus, Purchase, Refund, RepeatLast, Reconciliation, MerchantInfo, TerminalId, CashPayment }

    /// <summary>EN: Card on the terminal, or cash taken at the desk.  AR: بطاقة عبر الجهاز، أو نقداً عند الاستقبال.</summary>
    public enum PaymentMethod { Card, Cash }

    /// <summary>
    /// EN: How an operation ended. Saved in the Outcome column of every transaction row.
    /// AR: كيف انتهت العملية. تُحفظ في عمود Outcome لكل سجل.
    /// </summary>
    public enum TransactionOutcome
    {
        Ok,          // EN: non-financial call succeeded (status, terminal ID, merchant, reconciliation)   AR: عملية غير مالية نجحت
        Approved,    // EN: payment / refund approved (or cash recorded)                                    AR: الدفع أو الاسترجاع مقبول
        Declined,    // EN: refused by the bank or the terminal                                             AR: مرفوض من البنك أو الجهاز
        Cancelled,   // EN: cancelled on the terminal by the customer or cashier                            AR: أُلغيت على الجهاز
        Timeout,     // EN: the terminal did not answer in time                                             AR: انتهت مهلة الانتظار
        CancelledOrTimeout, // EN: DLL return code 8: cancelled on the terminal OR no card in time (the code does not say which)   AR: رمز 8: أُلغيت على الجهاز أو انتهت مهلة البطاقة
        NoResponse,  // EN: no terminal reached (port could not be opened)                                  AR: لا يوجد رد من الجهاز
        Failed,      // EN: the terminal / DLL returned an error code                                       AR: الجهاز أو المكتبة أعاد رمز خطأ
        Error        // EN: exception in this program or the DLL (DLL missing, crash, bad input)            AR: استثناء في البرنامج أو المكتبة
    }

    /// <summary>
    /// EN: Everything needed to reach one terminal: COM settings or IP + TCP port.
    /// AR: كل ما يلزم للوصول إلى جهاز واحد: إعدادات COM أو عنوان IP ومنفذ TCP.
    /// </summary>
    public sealed class TerminalConnection
    {
        public TerminalTransport Transport { get; set; }
        public int ComPort { get; set; }
        public int BaudRate { get; set; } = 38400;
        public int Parity { get; set; }
        public int DataBits { get; set; } = 8;
        public int StopBits { get; set; }
        public string TcpIp { get; set; }
        public string TcpPort { get; set; }

        /// <summary>EN: Short text for logs, e.g. "COM8 38400 baud..." or "192.168.1.25:6100".  AR: نص مختصر للسجل.</summary>
        public string Describe()
        {
            return Transport == TerminalTransport.Com
                ? string.Format(CultureInfo.InvariantCulture, "COM{0} {1} baud, parity {2}, {3} data bits, stop bits {4}",
                    ComPort, BaudRate, Parity, DataBits, StopBits)
                : TcpIp + ":" + TcpPort;
        }

        /// <summary>
        /// EN: Returns null when the settings are usable, otherwise a message that says what is wrong.
        /// AR: تُعيد null عندما تكون الإعدادات صالحة، وإلا رسالة توضح الخطأ.
        /// </summary>
        public string Validate()
        {
            if (Transport == TerminalTransport.Com)
            {
                return ComPort >= 1 && ComPort <= 255
                    ? null
                    : "COM port must be a number from 1 to 255. / منفذ COM يجب أن يكون رقماً من 1 إلى 255.";
            }

            IPAddress address;
            string ip = (TcpIp ?? "").Trim();
            if (ip.Split('.').Length != 4 || !IPAddress.TryParse(ip, out address) || address.AddressFamily != AddressFamily.InterNetwork)
                return "Enter a valid IPv4 address, e.g. 192.168.1.10. / أدخل عنوان IP صحيحاً، مثل 192.168.1.10.";

            int port;
            if (!int.TryParse((TcpPort ?? "").Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535)
                return "TCP port must be a number from 1 to 65535. / منفذ TCP يجب أن يكون رقماً من 1 إلى 65535.";

            return null;
        }
    }

    /// <summary>
    /// EN: Which terminal (SPAN / mada machine) is assigned to which workstation, and how to reach it.
    ///     One row per workstation. Reassigning a terminal to a new PC = saving a row for the new PC name.
    /// AR: أي جهاز (SPAN / مدى) مرتبط بأي حاسوب، وكيفية الوصول إليه.
    ///     سجل واحد لكل حاسوب. نقل الجهاز إلى حاسوب جديد = حفظ سجل باسم الحاسوب الجديد.
    /// </summary>
    public sealed class TerminalMapping
    {
        public string Workstation { get; set; }
        public string TerminalName { get; set; }
        public string Department { get; set; }
        public TerminalTransport Transport { get; set; }
        public int ComPort { get; set; }
        public int BaudRate { get; set; } = 38400;
        public int Parity { get; set; }
        public int DataBits { get; set; } = 8;
        public int StopBits { get; set; }
        public string TcpIp { get; set; }
        public string TcpPort { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string UpdatedBy { get; set; }

        public TerminalConnection ToConnection()
        {
            return new TerminalConnection
            {
                Transport = Transport, ComPort = ComPort, BaudRate = BaudRate, Parity = Parity,
                DataBits = DataBits, StopBits = StopBits, TcpIp = TcpIp, TcpPort = TcpPort
            };
        }
    }

    /// <summary>
    /// EN: What one terminal operation returned: the request, the DLL return code, the outcome, and every
    ///     field the machine wrote back. Card number is already masked here; expiry is never read.
    /// AR: ما أعادته عملية واحدة على الجهاز: الطلب، رمز الإرجاع، النتيجة، وكل حقل كتبه الجهاز.
    ///     رقم البطاقة مُقنّع هنا مسبقاً، وتاريخ الانتهاء لا يُقرأ أبداً.
    /// </summary>
    public sealed class TerminalResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public long DurationMs { get; set; }
        public TerminalOperation Operation { get; set; }
        public TerminalTransport Transport { get; set; }
        public string ConnectionTarget { get; set; }
        public string Request { get; set; }
        public long? AmountHalalas { get; set; }
        public string OriginalRrn { get; set; }
        public string OriginalDate { get; set; }

        /// <summary>EN: DLL return code (null when the DLL never returned).  AR: رمز الإرجاع من المكتبة (null إذا لم تُرجع).</summary>
        public int? ReturnCode { get; set; }
        public TransactionOutcome Outcome { get; set; }
        public string ErrorMessage { get; set; }

        // EN: Fields written by the terminal.   AR: الحقول التي كتبها الجهاز.
        public string ResponseCode { get; set; }
        public string MaskedPan { get; set; }
        public string SchemeId { get; set; }
        public string CardScheme { get; set; }
        public string TerminalAmount { get; set; }
        public string Stan { get; set; }
        public string TerminalDateTime { get; set; }
        public string Rrn { get; set; }
        public string AuthCode { get; set; }
        public string TerminalId { get; set; }
        public string MerchantId { get; set; }
        public string AdditionalAmount { get; set; }
        public string EcrRefNo { get; set; }
        public string Version { get; set; }
        public string TerminalTotal { get; set; }
        public string HostTotal { get; set; }
        public string MerchantNameEn { get; set; }
        public string MerchantNameAr { get; set; }
        public string MerchantAddressEn { get; set; }
        public string MerchantAddressAr { get; set; }
        public string RawResponse { get; set; }

        public bool IsSuccess
        {
            get { return Outcome == TransactionOutcome.Ok || Outcome == TransactionOutcome.Approved; }
        }
    }

    /// <summary>
    /// EN: Who / where / for which bill an operation was done. Links a payment to the patient's bill in HIS.
    /// AR: من نفّذ العملية وأين ولأي فاتورة. يربط الدفع بفاتورة المريض في نظام HIS.
    /// </summary>
    public sealed class PaymentContext
    {
        public string Workstation { get; set; } = Environment.MachineName;
        public string WindowsUser { get; set; } = CurrentWindowsUser();
        public string TerminalName { get; set; }
        public string Department { get; set; }
        public string PatientMrn { get; set; }
        public string InvoiceNo { get; set; }

        public static string CurrentWindowsUser()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                return identity.Name;
        }
    }

    /// <summary>
    /// EN: One row in PaymentTransactions: the terminal result + who/where/which bill + payment method.
    ///     Every operation is saved, whatever the outcome (approved, declined, cancelled, timeout, error...).
    /// AR: سجل واحد في PaymentTransactions: نتيجة الجهاز + من/أين/أي فاتورة + طريقة الدفع.
    ///     كل عملية تُحفظ مهما كانت نتيجتها (مقبولة، مرفوضة، ملغاة، انتهت المهلة، خطأ...).
    /// </summary>
    public sealed class PaymentTransactionRecord
    {
        public string Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public long DurationMs { get; set; }
        public string Workstation { get; set; }
        public string WindowsUser { get; set; }
        public string TerminalName { get; set; }
        public string Department { get; set; }
        public string PatientMrn { get; set; }
        public string InvoiceNo { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public TerminalOperation Operation { get; set; }
        public TerminalTransport Transport { get; set; }
        public string ConnectionTarget { get; set; }
        public string Request { get; set; }
        public long? AmountHalalas { get; set; }
        public string OriginalRrn { get; set; }
        public string OriginalDate { get; set; }
        public int? ReturnCode { get; set; }
        public TransactionOutcome Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string ResponseCode { get; set; }
        public string MaskedPan { get; set; }
        public string SchemeId { get; set; }
        public string CardScheme { get; set; }
        public string TerminalAmount { get; set; }
        public string Stan { get; set; }
        public string TerminalDateTime { get; set; }
        public string Rrn { get; set; }
        public string AuthCode { get; set; }
        public string TerminalId { get; set; }
        public string MerchantId { get; set; }
        public string AdditionalAmount { get; set; }
        public string EcrRefNo { get; set; }
        public string Version { get; set; }
        public string TerminalTotal { get; set; }
        public string HostTotal { get; set; }
        public string MerchantNameEn { get; set; }
        public string MerchantNameAr { get; set; }
        public string MerchantAddressEn { get; set; }
        public string MerchantAddressAr { get; set; }
        public string RawResponse { get; set; }

        /// <summary>EN: Builds the row for a terminal operation.  AR: يبني السجل لعملية على الجهاز.</summary>
        public static PaymentTransactionRecord FromResult(TerminalResult r, PaymentContext context)
        {
            return new PaymentTransactionRecord
            {
                Id = r.Id, CreatedAtUtc = r.StartedAtUtc, DurationMs = r.DurationMs,
                Workstation = context.Workstation, WindowsUser = context.WindowsUser,
                TerminalName = context.TerminalName, Department = context.Department,
                PatientMrn = context.PatientMrn, InvoiceNo = context.InvoiceNo,
                PaymentMethod = PaymentMethod.Card,
                Operation = r.Operation, Transport = r.Transport, ConnectionTarget = r.ConnectionTarget, Request = r.Request,
                AmountHalalas = r.AmountHalalas, OriginalRrn = r.OriginalRrn, OriginalDate = r.OriginalDate,
                ReturnCode = r.ReturnCode, Outcome = r.Outcome, ErrorMessage = r.ErrorMessage,
                ResponseCode = r.ResponseCode, MaskedPan = r.MaskedPan, SchemeId = r.SchemeId, CardScheme = r.CardScheme,
                TerminalAmount = r.TerminalAmount, Stan = r.Stan, TerminalDateTime = r.TerminalDateTime, Rrn = r.Rrn,
                AuthCode = r.AuthCode, TerminalId = r.TerminalId, MerchantId = r.MerchantId,
                AdditionalAmount = r.AdditionalAmount, EcrRefNo = r.EcrRefNo, Version = r.Version,
                TerminalTotal = r.TerminalTotal, HostTotal = r.HostTotal,
                MerchantNameEn = r.MerchantNameEn, MerchantNameAr = r.MerchantNameAr,
                MerchantAddressEn = r.MerchantAddressEn, MerchantAddressAr = r.MerchantAddressAr,
                RawResponse = r.RawResponse
            };
        }

        /// <summary>
        /// EN: A cash payment taken at the desk, outside the terminal (kept so reception can still bill in cash).
        /// AR: دفع نقدي عند الاستقبال خارج الجهاز (حتى يبقى خيار الدفع النقدي متاحاً للاستقبال).
        /// </summary>
        public static PaymentTransactionRecord Cash(long amountHalalas, PaymentContext context)
        {
            return new PaymentTransactionRecord
            {
                Id = Guid.NewGuid().ToString("N"), CreatedAtUtc = DateTime.UtcNow,
                Workstation = context.Workstation, WindowsUser = context.WindowsUser,
                TerminalName = context.TerminalName, Department = context.Department,
                PatientMrn = context.PatientMrn, InvoiceNo = context.InvoiceNo,
                PaymentMethod = PaymentMethod.Cash, Operation = TerminalOperation.CashPayment,
                AmountHalalas = amountHalalas, Outcome = TransactionOutcome.Approved
            };
        }
    }

    /// <summary>
    /// EN: One row in ErrorLog: any exception or failure, from the UI, the terminal service or storage.
    /// AR: سجل واحد في ErrorLog: أي استثناء أو فشل من الواجهة أو خدمة الجهاز أو التخزين.
    /// </summary>
    public sealed class ErrorLogRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string Workstation { get; set; } = Environment.MachineName;
        public string WindowsUser { get; set; } = PaymentContext.CurrentWindowsUser();
        public string Source { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }

        public static ErrorLogRecord FromException(string source, Exception ex)
        {
            return new ErrorLogRecord
            {
                Source = source,
                Message = ex.GetType().Name + ": " + ex.Message,
                Details = CardDataMasker.MaskText(ex.ToString())
            };
        }
    }
}
