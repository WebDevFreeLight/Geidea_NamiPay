// TCP/IP Integration Sample Code
// =====================================================================================
// EN: Sample Windows Forms app (ECR = Electronic Cash Register) that talks to a mada
//     payment terminal through the vendor's native DLLs (madaapi_v1_9.dll / madaapi_v1_8.dll).
//     Each button on the form runs one terminal operation over either:
//       - USB / serial (COM port)  -> functions with "COM" in the name
//       - TCP/IP (network)         -> functions with "TCPIP" in the name
//     Every value the buttons use (COM port, serial settings, IP, TCP port, amounts, RRN, date)
//     is typed on the form. The starting values come from app.config, and "Save defaults"
//     writes the current values back, so nothing is hard-coded in the buttons any more.
// AR: تطبيق تجريبي (Windows Forms) يمثّل نظام نقاط البيع (ECR) ويتواصل مع جهاز الدفع "مدى"
//     عبر مكتبات المورّد الأصلية (madaapi_v1_9.dll و madaapi_v1_8.dll).
//     كل زر في النافذة ينفّذ عملية واحدة على الجهاز عبر إحدى الطريقتين:
//       - USB / منفذ تسلسلي (COM)   -> الدوال التي تحتوي أسماؤها على "COM"
//       - شبكة TCP/IP               -> الدوال التي تحتوي أسماؤها على "TCPIP"
//     كل قيمة تستخدمها الأزرار (منفذ COM، إعدادات المنفذ التسلسلي، IP، منفذ TCP، المبالغ، الرقم
//     المرجعي، التاريخ) تُكتب في النافذة. القيم الأولية تأتي من app.config، وزر "حفظ الافتراضي"
//     يكتب القيم الحالية فيه، لذلك لم تعد هناك قيم مكتوبة مباشرة داخل الأزرار.
//
// -------------------------------------------------------------------------------------
// EN: Button -> handler -> operation map (button texts come from Form1.Designer.cs)
// AR: خريطة الأزرار -> الدالة -> العملية (نصوص الأزرار من ملف Form1.Designer.cs)
//
//   Button                      Handler                Request             العملية (AR)
//   -------------------------   --------------------   -----------------   -----------------------
//   Check status (USB/TCP)      btnStatusUsb/Tcp       "04!"               فحص حالة الجهاز
//   Purchase (USB/TCP)          btnPurchaseUsb/Tcp     "<amount>;0;1!"     عملية شراء
//   Refund (USB/TCP)            btnRefundUsb/Tcp       "<amount>;<RRN>;<DDMMYYYY>;1;1!"   عملية استرجاع
//   Repeat last (USB/TCP)       btnRepeatUsb/Tcp       "0B!"               إعادة آخر عملية
//   Reconciliation (USB/TCP)    btnReconUsb/Tcp        "09;0!"             التسوية / إقفال اليوم
//   Merchant info (USB)         btnMerchant            "0A!"               بيانات التاجر (COM فقط)
//   Terminal ID (TCP)           btnTerminalId          "07!"               رقم الجهاز (TCP فقط)
//   Clear log / Save defaults   btnClearLog / btnSaveDefaults              مسح السجل / حفظ الافتراضي
//
// -------------------------------------------------------------------------------------
// EN: SHARED PARAMETERS used by the DLL functions (all taken from the form)
// AR: المعاملات المشتركة المستخدمة في دوال المكتبة (كلها مأخوذة من النافذة)
//
//   COM (USB/serial) functions take:  (int port, int Rate, int x, int y, int z, ...)
//     port  EN: "COM port" box (5 => "COM5"). Check Device Manager for the terminal's port.
//           AR: خانة "منفذ COM" (5 تعني "COM5"). راجع "إدارة الأجهزة" لمعرفة منفذ الجهاز.
//     Rate  EN: "Baud rate" box (default 38400).
//           AR: خانة "سرعة النقل" (الافتراضي 38400).
//     x     EN: "Parity" box (0 = none, Win32 NOPARITY).
//           AR: خانة "التماثل" (0 = بدون).
//     y     EN: "Data bits" box (default 8).
//           AR: خانة "بتات البيانات" (الافتراضي 8).
//     z     EN: "Stop bits" box (0 = one stop bit, Win32 ONESTOPBIT).
//           AR: خانة "بتات التوقف" (0 = بت توقف واحد).
//
//   TCP/IP functions take:  (byte[] ip, byte[] port, ...)
//     EN: "Terminal IP" and "TCP port" boxes, sent as ASCII bytes + '\0', e.g. "192.168.116.166" and "6100".
//     AR: خانتا "عنوان IP" و"منفذ TCP"، تُرسلان كبايتات ASCII مع '\0'، مثل "192.168.116.166" و "6100".
//
//   Request (input) parameters / معاملات الطلب (المدخلات):
//     inOutBuff  EN: The request string as ASCII bytes. Requests end with "!".
//                AR: نص الطلب كبايتات ASCII. الطلبات تنتهي بالرمز "!".
//     intval     EN: 1-byte array holding the length of inOutBuff (so a request is at most 255 bytes).
//                AR: مصفوفة ببايت واحد تحمل طول inOutBuff (لذلك الحد الأقصى للطلب 255 بايت).
//     trnxType   EN: Transaction type. Used here: 0 = Purchase, 2 = Refund.
//                AR: نوع العملية. المستخدم هنا: 0 = شراء، 2 = استرجاع.
//
//     EN: <amount> is in halalas, the smallest currency unit: 43.71 SAR on the form -> "4371".
//         <RRN> is the 12-digit RRN of the original purchase. <DDMMYYYY> is its (Gregorian) date.
//         The meaning of the trailing "0;1" / "1;1" flags is NOT stated in this code.
//         Confirm them in the mada ECR integration document from the vendor.
//     AR: <amount> بالهللة (أصغر وحدة للعملة): 43.71 ريال في النافذة -> "4371".
//         <RRN> هو الرقم المرجعي (12 رقماً) لعملية الشراء الأصلية، و <DDMMYYYY> تاريخها (الميلادي).
//         معنى العلامات الأخيرة "0;1" / "1;1" غير موضّح في هذا الكود.
//         يجب التأكد منها من وثيقة ربط ECR الخاصة بمدى من المورّد.
//
//   Output (response) buffers / مخازن الاستجابة (المخرجات):
//     EN: The DLL WRITES into these arrays. Each size = max field length + 1 for the null terminator.
//     AR: المكتبة تكتب النتائج داخل هذه المصفوفات. كل حجم = أقصى طول للحقل + 1 لحرف النهاية (null).
//
//     panNo       [23]  EN: Card number (PAN, normally masked)   AR: رقم البطاقة (عادةً مُقنّع)
//     purAmount   [13]  EN: Purchase amount                      AR: مبلغ العملية
//     stanNo      [7]   EN: STAN, 6-digit trace number           AR: رقم تتبع العملية (6 أرقام)
//     dataTime    [13]  EN: Transaction date/time                AR: تاريخ ووقت العملية
//     expDate     [5]   EN: Card expiry (4 digits)               AR: تاريخ انتهاء البطاقة (4 أرقام)
//     trxRrn      [13]  EN: RRN, 12-digit retrieval ref number   AR: الرقم المرجعي للعملية (12 رقماً)
//     authCode    [7]   EN: Authorization (approval) code        AR: رمز الموافقة
//     rspCode     [4]   EN: Host response code                   AR: رمز الاستجابة من البنك
//     terminalId  [17]  EN: Terminal ID                          AR: رقم الجهاز (Terminal ID)
//     schemeId    [3]   EN: Card scheme ID (mada, Visa, MC...)   AR: نوع شبكة البطاقة (مدى، فيزا، ماستر...)
//     merchantId  [16]  EN: Merchant ID (15 digits)              AR: رقم التاجر (15 رقماً)
//     addtlAmount [13]  EN: Additional amount                    AR: مبلغ إضافي
//     ecrrefno    [17]  EN: ECR reference number                 AR: الرقم المرجعي لنظام نقاط البيع
//     version     [10]  EN: Terminal/API version                 AR: إصدار الجهاز/المكتبة
//     outResp     (StringBuilder 15000) EN: Full raw response (UTF-16)   AR: الاستجابة الكاملة الخام (UTF-16)
//     outRespLen  [1]   EN: Length of outResp                    AR: طول outResp
//
//   Return value / القيمة المُعادة:
//     EN: Every function returns a status code as IntPtr. It is shown in a MessageBox and in the log.
//         The numeric meanings are defined by the vendor (with no terminal attached the DLL returned -3).
//         The DLL log (bin\Debug\ECRLOG_*.txt) also prints text such as "API_TERM_BUSY".
//     AR: كل دالة تُعيد رمز حالة من النوع IntPtr، ويُعرض في رسالة وفي السجل.
//         معاني الأرقام يحددها المورّد (بدون جهاز متصل أعادت المكتبة -3).
//         سجل المكتبة (bin\Debug\ECRLOG_*.txt) يطبع أيضاً نصوصاً مثل "API_TERM_BUSY" (الجهاز مشغول).
//
// -------------------------------------------------------------------------------------
// EN: NOTES
// AR: ملاحظات
//   1. EN: Calls are synchronous on the UI thread, so the window freezes until the terminal answers.
//      AR: الاستدعاءات متزامنة على خيط الواجهة، لذلك تتجمّد النافذة حتى يرد الجهاز.
//   2. EN: Repeat functions load madaapi_v1_8.dll, all others load madaapi_v1_9.dll. Both DLLs
//          must sit next to CDLL.exe (bin\Debug).
//      AR: دوال "إعادة آخر عملية" تحمّل madaapi_v1_8.dll وبقية الدوال تحمّل madaapi_v1_9.dll.
//          يجب أن تكون المكتبتان بجانب CDLL.exe (داخل bin\Debug).
//   3. EN: Changes from the original sample: one shared COM port / IP for all buttons (they used
//          5, 9, 15, 17 and four IPs), Refund (TCP) now ends with "!" like Refund (USB), and
//          results are printed without the trailing '\0' characters.
//      AR: تغييرات عن المثال الأصلي: منفذ COM وعنوان IP واحد لكل الأزرار (كانت تستخدم 5، 9، 15، 17
//          وأربعة عناوين)، الاسترجاع عبر TCP ينتهي الآن بـ "!" مثل USB، والنتائج تُطبع بدون أحرف '\0'.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;             // EN: ConfigurationManager, reads/writes app.config   AR: لقراءة وكتابة إعدادات app.config
using System.Data;
using System.Drawing;
using System.Globalization;             // EN: InvariantCulture for numbers and dates   AR: لتنسيق الأرقام والتواريخ
using System.IO;
using System.Net;                       // EN: IPAddress validation   AR: للتحقق من عنوان IP
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;   // EN: DllImport / MarshalAs for calling the native DLL   AR: لاستدعاء المكتبة الأصلية
using System.Text;
using System.Windows.Forms;


namespace CDLL
{
    /// <summary>
    /// EN: Main window for the REAL terminal. Declares the native mada API functions, one click handler
    ///     per operation, and the settings/transaction inputs that every handler reads.
    /// AR: النافذة الرئيسية للجهاز الفعلي. تعرّف دوال مكتبة مدى الأصلية، ودالة نقر لكل عملية،
    ///     وخانات الإعدادات وقيم العملية التي تقرأ منها كل الدوال.
    /// </summary>
    public partial class Form1 : Form
    {
        // =============================================================================
        // EN: NATIVE DLL DECLARATIONS (P/Invoke)
        //     [DllImport] tells .NET to load the named DLL at runtime and call the exported
        //     C function with the same name. StdCall = Win32 calling convention.
        //     The DLL is looked up next to CDLL.exe first, then in the system PATH.
        //     byte[] is passed as a char* the DLL can write into; StringBuilder with
        //     LPWStr is passed as a wchar_t* (wide/Unicode) buffer.
        // AR: تعريفات دوال المكتبة الأصلية (P/Invoke)
        //     الخاصية [DllImport] تجعل ‎.NET تحمّل المكتبة المذكورة وقت التشغيل وتستدعي دالة C
        //     المُصدَّرة بنفس الاسم. StdCall هي طريقة الاستدعاء القياسية في Win32.
        //     يتم البحث عن المكتبة أولاً بجانب CDLL.exe ثم في مسار النظام PATH.
        //     المصفوفة byte[] تُمرَّر كمؤشر char* تكتب فيه المكتبة، و StringBuilder مع LPWStr
        //     تُمرَّر كمخزن wchar_t* (نص يونيكود).
        // =============================================================================

       // EN: Old commented-out copy of the declaration below, kept by the original author.
       // AR: نسخة قديمة معطّلة من التعريف التالي، أبقاها المطوّر الأصلي.
       // [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
       // public static extern IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Sends a financial transaction (Purchase trnxType=0, Refund trnxType=2) over USB/COM
        ///     and fills the output buffers with the card/transaction result.
        ///     Used by: btnPurchaseUsb_Click, btnRefundUsb_Click.
        /// AR: يرسل عملية مالية (شراء trnxType=0، استرجاع trnxType=2) عبر USB/COM
        ///     ويملأ مخازن المخرجات بنتيجة البطاقة والعملية.
        ///     مستخدمة في: btnPurchaseUsb_Click و btnRefundUsb_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Same as api_RequestCOMTrxn but over TCP/IP (ip + port instead of COM settings).
        ///     Used by: btnPurchaseTcp_Click, btnRefundTcp_Click.
        /// AR: مثل api_RequestCOMTrxn لكن عبر شبكة TCP/IP (عنوان IP ومنفذ بدلاً من إعدادات COM).
        ///     مستخدمة في: btnPurchaseTcp_Click و btnRefundTcp_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Reads the merchant name and address stored in the terminal, in English (byte[])
        ///     and Arabic (StringBuilder, Unicode). Request string: "0A!". USB/COM only.
        ///     Used by: btnMerchant_Click.
        /// AR: يقرأ اسم التاجر وعنوانه المخزّنين في الجهاز بالإنجليزية (byte[]) وبالعربية
        ///     (StringBuilder بترميز يونيكود). نص الطلب: "0A!". عبر USB/COM فقط.
        ///     مستخدمة في: btnMerchant_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantNameArb, byte[] merchantAddEng1, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantAddArb1, byte[] merchantAddopt1, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantAddopt2);

        /// <summary>
        /// EN: Reads the Terminal ID over TCP/IP. Request string: "07!".
        ///     Used by: btnTerminalId_Click.
        /// AR: يقرأ رقم الجهاز (Terminal ID) عبر TCP/IP. نص الطلب: "07!".
        ///     مستخدمة في: btnTerminalId_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId);

        /// <summary>
        /// EN: Checks that the terminal is connected and ready (USB/COM). Request string: "04!".
        ///     Only the return code tells you the result.
        ///     Used by: btnStatusUsb_Click.
        /// AR: يتحقق من أن الجهاز متصل وجاهز (USB/COM). نص الطلب: "04!".
        ///     النتيجة تُعرف من رمز الإرجاع فقط.
        ///     مستخدمة في: btnStatusUsb_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval);

        /// <summary>
        /// EN: Same as api_CheckStatus but over TCP/IP.
        ///     Used by: btnStatusTcp_Click.
        /// AR: مثل api_CheckStatus لكن عبر TCP/IP.
        ///     مستخدمة في: btnStatusTcp_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval);

        /// <summary>
        /// EN: Reconciliation (end-of-day settlement) over TCP/IP, WITHOUT printing a receipt ("WORcpt").
        ///     Returns terminal totals vs host (bank) totals so you can compare them.
        ///     NOTE: the request length is passed by reference (ref int inReqlen), not as a 1-byte array.
        ///     Used by: btnReconTcp_Click.
        /// AR: التسوية (إقفال نهاية اليوم) عبر TCP/IP بدون طباعة إيصال ("WORcpt" = Without Receipt).
        ///     تُعيد مجاميع الجهاز ومجاميع البنك (المضيف) للمقارنة بينهما.
        ///     ملاحظة: طول الطلب يُمرَّر بالمرجع (ref int inReqlen) وليس كمصفوفة ببايت واحد.
        ///     مستخدمة في: btnReconTcp_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        /// <summary>
        /// EN: Reconciliation (end-of-day settlement) over USB/COM, without printing a receipt.
        ///     Used by: btnReconUsb_Click.
        /// AR: التسوية (إقفال نهاية اليوم) عبر USB/COM بدون طباعة إيصال.
        ///     مستخدمة في: btnReconUsb_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]

        public static extern IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        /// <summary>
        /// EN: Repeat: asks the terminal again for the result of the LAST transaction over TCP/IP.
        ///     Use it when the ECR lost the response (timeout, crash) and you must know whether
        ///     the customer was charged before retrying. Request string: "0B!".
        ///     Loaded from the OLDER madaapi_v1_8.dll. Has an extra Cardscheme output buffer.
        ///     Used by: btnRepeatTcp_Click.
        /// AR: إعادة: يطلب من الجهاز مرة أخرى نتيجة آخر عملية عبر TCP/IP.
        ///     تُستخدم عندما يفقد نظام نقاط البيع الاستجابة (انتهاء المهلة أو تعطّل البرنامج) ويجب
        ///     معرفة هل تم الخصم من العميل قبل إعادة المحاولة. نص الطلب: "0B!".
        ///     تُحمَّل من المكتبة الأقدم madaapi_v1_8.dll، وفيها مخزن إضافي Cardscheme لنوع البطاقة.
        ///     مستخدمة في: btnRepeatTcp_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_8.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval,byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Repeat last transaction over USB/COM (see api_RequestTCPIPRepeat). From madaapi_v1_8.dll.
        ///     Used by: btnRepeatUsb_Click.
        /// AR: إعادة آخر عملية عبر USB/COM (راجع api_RequestTCPIPRepeat). من المكتبة madaapi_v1_8.dll.
        ///     مستخدمة في: btnRepeatUsb_Click.
        /// </summary>
        [DllImport(@"madaapi_v1_8.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestCOMRepeat(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval,  byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Constructor. Builds the controls defined in Form1.Designer.cs.
        /// AR: المُنشئ. يبني عناصر الواجهة المعرّفة في Form1.Designer.cs.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// EN: Runs when the window opens: fills every input with its default from app.config
        ///     (or the built-in default below when the key is missing or invalid).
        /// AR: تُنفَّذ عند فتح النافذة: تملأ كل خانة بقيمتها الافتراضية من app.config
        ///     (أو القيمة المدمجة أدناه عندما يكون المفتاح مفقوداً أو غير صالح).
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            SetNumber(numComPort, "PaymentMachinePort", 5);
            SetNumber(numBaudRate, "BaudRate", 38400);
            SetNumber(numParity, "Parity", 0);
            SetNumber(numDataBits, "DataBits", 8);
            SetNumber(numStopBits, "StopBits", 0);
            txtIp.Text = Setting("TerminalIp", "192.168.116.166");
            txtTcpPort.Text = Setting("TerminalTcpPort", "6100");

            SetNumber(numPurchaseAmount, "PurchaseAmount", 43.71m);
            SetNumber(numRefundAmount, "RefundAmount", 0.50m);
            txtOriginalRrn.Text = Setting("OriginalRrn", "434615455456");
            DateTime originalDate;
            dtpOriginalDate.Value = DateTime.TryParseExact(Setting("OriginalDate", ""), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out originalDate)
                ? originalDate
                : new DateTime(2024, 12, 11);
        }

        // =============================================================================
        // EN: STATUS / الحالة
        // =============================================================================

        /// <summary>
        /// EN: [Check status (USB)] Checks the terminal over the COM port. Only the return code tells the result.
        /// AR: [فحص الحالة (USB)] يفحص الجهاز عبر منفذ COM. النتيجة تُعرف من رمز الإرجاع فقط.
        /// </summary>
        private void btnStatusUsb_Click(object sender, EventArgs e)
        {
            string TextString = "04!";   // EN: check status command   AR: أمر فحص الحالة
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Check status (USB) / فحص الحالة", ComTarget(), TextString);
            var Result = api_CheckStatus(ComPort, BaudRate, Parity, DataBits, StopBits, inOutBuff, intval);
            LogResult(Result);
        }

        /// <summary>
        /// EN: [Check status (TCP)] Checks the terminal over the network.
        /// AR: [فحص الحالة (TCP)] يفحص الجهاز عبر الشبكة.
        /// </summary>
        private void btnStatusTcp_Click(object sender, EventArgs e)
        {
            byte[] b_ip, b_port;
            if (!TryGetTcpAddress(out b_ip, out b_port))
                return;

            string TextString = "04!";
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Check status (TCP) / فحص الحالة", TcpTarget(), TextString);
            var Result = api_TCPIPCheckStatus(b_ip, b_port, inOutBuff, intval);
            LogResult(Result);
        }

        // =============================================================================
        // EN: PURCHASE / الشراء
        // =============================================================================

        /// <summary>
        /// EN: [Purchase (USB)] Purchase of the "Purchase amount" over the COM port. Blocks until the card is used.
        /// AR: [شراء (USB)] عملية شراء بقيمة "مبلغ الشراء" عبر منفذ COM. ينتظر حتى تُستخدم البطاقة.
        /// </summary>
        private void btnPurchaseUsb_Click(object sender, EventArgs e)
        {
            // EN: Output buffers: the DLL fills them. Size = max length + 1 (null terminator).
            // AR: مخازن المخرجات: المكتبة تملؤها. الحجم = أقصى طول + 1 (حرف النهاية null).
            byte[] panNo = new byte[23];        // EN: card number                AR: رقم البطاقة
            byte[] purAmount = new byte[13];    // EN: amount                     AR: المبلغ
            byte[] stanNo = new byte[7];        // EN: STAN trace number          AR: رقم التتبع
            byte[] dataTime = new byte[13];     // EN: date/time                  AR: التاريخ والوقت
            byte[] expDate = new byte[5];       // EN: card expiry                AR: انتهاء البطاقة
            byte[] trxRrn = new byte[13];       // EN: RRN reference              AR: الرقم المرجعي
            byte[] authCode = new byte[7];      // EN: approval code              AR: رمز الموافقة
            byte[] rspCode = new byte[4];       // EN: response code              AR: رمز الاستجابة
            byte[] terminalId = new byte[17];   // EN: terminal ID                AR: رقم الجهاز
            byte[] schemeId = new byte[3];      // EN: card scheme                AR: نوع البطاقة
            byte[] merchantId = new byte[16];   // EN: merchant ID                AR: رقم التاجر
            byte[] addtlAmount = new byte[13];  // EN: additional amount          AR: مبلغ إضافي
            byte[] ecrrefno = new byte[17];     // EN: ECR reference              AR: مرجع نقاط البيع
            byte[] version = new byte[10];      // EN: version                    AR: الإصدار
            byte[] outRespLen = new byte[1];    // EN: raw response length        AR: طول الاستجابة الخام
            StringBuilder outResp = new StringBuilder(15000);   // EN: raw full response   AR: الاستجابة الكاملة الخام

            int trnxType = 0;                   // EN: 0 = Purchase   AR: 0 = شراء
            string TextString = PurchaseRequest();
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Purchase (USB) / شراء", ComTarget(), TextString);
            var Result = api_RequestCOMTrxn(ComPort, BaudRate, Parity, DataBits, StopBits, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            LogResult(Result);
            LogTransaction(panNo, schemeId, trxRrn, purAmount, stanNo, dataTime, authCode, rspCode, terminalId, ecrrefno);
        }

        /// <summary>
        /// EN: [Purchase (TCP)] Purchase of the "Purchase amount" over the network.
        /// AR: [شراء (TCP)] عملية شراء بقيمة "مبلغ الشراء" عبر الشبكة.
        /// </summary>
        private void btnPurchaseTcp_Click(object sender, EventArgs e)
        {
            byte[] b_ip, b_port;
            if (!TryGetTcpAddress(out b_ip, out b_port))
                return;

            // EN: Output buffers (same layout as btnPurchaseUsb_Click).   AR: مخازن المخرجات (نفس ترتيب btnPurchaseUsb_Click).
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int trnxType = 0;                   // EN: 0 = Purchase   AR: 0 = شراء
            string TextString = PurchaseRequest();
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Purchase (TCP) / شراء", TcpTarget(), TextString);
            var Result = api_RequestTCPIPTrxn(b_ip, b_port, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            LogResult(Result);
            LogTransaction(panNo, schemeId, trxRrn, purAmount, stanNo, dataTime, authCode, rspCode, terminalId, ecrrefno);
        }

        // =============================================================================
        // EN: REFUND / الاسترجاع
        // =============================================================================

        /// <summary>
        /// EN: [Refund (USB)] Refund of the "Refund amount" for the "Original RRN" / "Original date", over the COM port.
        /// AR: [استرجاع (USB)] استرجاع "مبلغ الاسترجاع" للعملية ذات "الرقم المرجعي" و"التاريخ" الأصليين، عبر منفذ COM.
        /// </summary>
        private void btnRefundUsb_Click(object sender, EventArgs e)
        {
            string TextString;
            if (!TryBuildRefundRequest(out TextString))
                return;

            // EN: Output buffers (same layout as btnPurchaseUsb_Click).   AR: مخازن المخرجات (نفس ترتيب btnPurchaseUsb_Click).
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int trnxType = 2;                   // EN: 2 = Refund   AR: 2 = استرجاع
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Refund (USB) / استرجاع", ComTarget(), TextString);
            var Result = api_RequestCOMTrxn(ComPort, BaudRate, Parity, DataBits, StopBits, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            LogResult(Result);
            LogTransaction(panNo, schemeId, trxRrn, purAmount, stanNo, dataTime, authCode, rspCode, terminalId, ecrrefno);
        }

        /// <summary>
        /// EN: [Refund (TCP)] Same as Refund (USB) but over the network.
        /// AR: [استرجاع (TCP)] مثل الاسترجاع عبر USB لكن عبر الشبكة.
        /// </summary>
        private void btnRefundTcp_Click(object sender, EventArgs e)
        {
            byte[] b_ip, b_port;
            string TextString;
            if (!TryGetTcpAddress(out b_ip, out b_port) || !TryBuildRefundRequest(out TextString))
                return;

            // EN: Output buffers (same layout as btnPurchaseUsb_Click).   AR: مخازن المخرجات (نفس ترتيب btnPurchaseUsb_Click).
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int trnxType = 2;                   // EN: 2 = Refund   AR: 2 = استرجاع
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Refund (TCP) / استرجاع", TcpTarget(), TextString);
            var Result = api_RequestTCPIPTrxn(b_ip, b_port, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            LogResult(Result);
            LogTransaction(panNo, schemeId, trxRrn, purAmount, stanNo, dataTime, authCode, rspCode, terminalId, ecrrefno);
        }

        // =============================================================================
        // EN: REPEAT LAST TRANSACTION / إعادة آخر عملية
        // =============================================================================

        /// <summary>
        /// EN: [Repeat last (USB)] Re-fetches the LAST transaction result over the COM port (madaapi_v1_8.dll).
        ///     Use it when the answer of a payment was lost, before trying the payment again.
        /// AR: [إعادة آخر عملية (USB)] يعيد جلب نتيجة آخر عملية عبر منفذ COM (madaapi_v1_8.dll).
        ///     استخدمه عندما يضيع رد عملية دفع، قبل إعادة محاولة الدفع.
        /// </summary>
        private void btnRepeatUsb_Click(object sender, EventArgs e)
        {
            // EN: Output buffers (same layout as btnPurchaseUsb_Click).   AR: مخازن المخرجات (نفس ترتيب btnPurchaseUsb_Click).
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            string TextString = "0B!";          // EN: repeat last transaction command   AR: أمر إعادة آخر عملية
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Repeat last (USB) / إعادة آخر عملية", ComTarget(), TextString);
            var Result = api_RequestCOMRepeat(ComPort, BaudRate, Parity, DataBits, StopBits, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            LogResult(Result);
            LogTransaction(panNo, schemeId, trxRrn, purAmount, stanNo, dataTime, authCode, rspCode, terminalId, ecrrefno);
        }

        /// <summary>
        /// EN: [Repeat last (TCP)] Same over the network; also returns the card scheme name.
        /// AR: [إعادة آخر عملية (TCP)] نفس العملية عبر الشبكة، وتُعيد أيضاً اسم شبكة البطاقة.
        /// </summary>
        private void btnRepeatTcp_Click(object sender, EventArgs e)
        {
            byte[] b_ip, b_port;
            if (!TryGetTcpAddress(out b_ip, out b_port))
                return;

            // EN: Output buffers (same layout as btnPurchaseUsb_Click) + cardscheme.
            // AR: مخازن المخرجات (نفس ترتيب btnPurchaseUsb_Click) بالإضافة إلى cardscheme.
            byte[] panNo = new byte[23];
            byte[] purAmount = new byte[13];
            byte[] stanNo = new byte[7];
            byte[] dataTime = new byte[13];
            byte[] expDate = new byte[5];
            byte[] trxRrn = new byte[13];
            byte[] authCode = new byte[7];
            byte[] rspCode = new byte[4];
            byte[] terminalId = new byte[17];
            byte[] schemeId = new byte[3];
            byte[] merchantId = new byte[16];
            byte[] addtlAmount = new byte[13];
            byte[] ecrrefno = new byte[17];
            byte[] version = new byte[10];
            byte[] cardscheme = new byte[10];   // EN: card scheme name (TCP repeat only)   AR: اسم شبكة البطاقة (في إعادة TCP فقط)
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            string TextString = "0B!";          // EN: repeat last transaction command   AR: أمر إعادة آخر عملية
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Repeat last (TCP) / إعادة آخر عملية", TcpTarget(), TextString);
            var Result = api_RequestTCPIPRepeat(b_ip, b_port, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, cardscheme, outResp, outRespLen);
            LogResult(Result);
            LogTransaction(panNo, schemeId, trxRrn, purAmount, stanNo, dataTime, authCode, rspCode, terminalId, ecrrefno);
            LogField("Card scheme / شبكة البطاقة", cardscheme);
        }

        // =============================================================================
        // EN: RECONCILIATION (END OF DAY) / التسوية (إقفال اليوم)
        // =============================================================================

        /// <summary>
        /// EN: [Reconciliation (USB)] End-of-day settlement over the COM port. Terminal and host totals should match.
        /// AR: [التسوية (USB)] إقفال نهاية اليوم عبر منفذ COM. يجب أن تتطابق مجاميع الجهاز مع مجاميع البنك.
        /// </summary>
        private void btnReconUsb_Click(object sender, EventArgs e)
        {
            // EN: Recon output buffers (sized generously, as in the original sample).
            // AR: مخازن مخرجات التسوية (بأحجام كبيرة احتياطاً، كما في المثال الأصلي).
            byte[] rspCode = new byte[30];        // EN: response code      AR: رمز الاستجابة
            byte[] terminalTotal = new byte[500]; // EN: terminal totals    AR: مجاميع الجهاز
            byte[] hostTotal = new byte[500];     // EN: host/bank totals   AR: مجاميع البنك
            byte[] stanNo = new byte[30];
            byte[] dataTime = new byte[30];
            byte[] terminalId = new byte[30];
            byte[] merchantId = new byte[30];

            string TextString = "09;0!";          // EN: reconciliation command   AR: أمر التسوية
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Reconciliation (USB) / التسوية", ComTarget(), TextString);
            var Result = api_RequestReconCOMWORcpt(ComPort, BaudRate, Parity, DataBits, StopBits, inOutBuff, intval, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);
            LogResult(Result);
            LogRecon(rspCode, terminalTotal, hostTotal);
        }

        /// <summary>
        /// EN: [Reconciliation (TCP)] Same over the network. This DLL function takes the request length by reference.
        /// AR: [التسوية (TCP)] نفس العملية عبر الشبكة. دالة المكتبة هذه تأخذ طول الطلب بالمرجع.
        /// </summary>
        private void btnReconTcp_Click(object sender, EventArgs e)
        {
            byte[] b_ip, b_port;
            if (!TryGetTcpAddress(out b_ip, out b_port))
                return;

            byte[] rspCode = new byte[30];
            byte[] terminalTotal = new byte[500];
            byte[] hostTotal = new byte[500];
            byte[] stanNo = new byte[30];
            byte[] dataTime = new byte[30];
            byte[] terminalId = new byte[30];
            byte[] merchantId = new byte[30];

            string send_buff = "09;0!";           // EN: reconciliation command   AR: أمر التسوية
            byte[] inReqbuff = Encoding.ASCII.GetBytes(send_buff);
            int inReqlen = inReqbuff.Length;      // EN: passed by "ref", the DLL may update it   AR: يُمرَّر بالمرجع وقد تعدّله المكتبة

            LogStart("Reconciliation (TCP) / التسوية", TcpTarget(), send_buff);
            var Result = api_RequestReconTCPIPWORcpt(b_ip, b_port, inReqbuff, ref inReqlen, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);
            LogResult(Result);
            LogRecon(rspCode, terminalTotal, hostTotal);
        }

        // =============================================================================
        // EN: TERMINAL INFO / معلومات الجهاز
        // =============================================================================

        /// <summary>
        /// EN: [Merchant info (USB)] Reads merchant name/address (English + Arabic). The DLL has no TCP version.
        /// AR: [بيانات التاجر (USB)] يقرأ اسم التاجر وعنوانه (إنجليزي + عربي). المكتبة لا توفر نسخة TCP.
        /// </summary>
        private void btnMerchant_Click(object sender, EventArgs e)
        {
            // EN: English fields come back in byte[]; Arabic fields need Unicode, so they use StringBuilder.
            // AR: الحقول الإنجليزية تعود في byte[]، والعربية تحتاج يونيكود لذلك تستخدم StringBuilder.
            byte[] merchantNameEng = new byte[500];
            byte[] merchantAddEng1 = new byte[500];
            byte[] merchantAddopt1 = new byte[500];
            StringBuilder merchantNameArb = new StringBuilder(500);
            StringBuilder merchantAddArb1 = new StringBuilder(500);
            StringBuilder merchantAddopt2 = new StringBuilder(500);

            string TextString = "0A!";            // EN: get merchant details command   AR: أمر طلب بيانات التاجر
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Merchant info (USB) / بيانات التاجر", ComTarget(), TextString);
            var Result = api_GetMerchantDetails(ComPort, BaudRate, Parity, DataBits, StopBits, inOutBuff, intval, merchantNameEng, merchantNameArb, merchantAddEng1, merchantAddArb1, merchantAddopt1, merchantAddopt2);
            LogResult(Result);
            LogField("Merchant name EN / اسم التاجر EN", merchantNameEng);
            LogText("Merchant name AR / اسم التاجر AR", merchantNameArb.ToString());
            LogField("Address EN / العنوان EN", merchantAddEng1);
            LogText("Address AR / العنوان AR", merchantAddArb1.ToString());
        }

        /// <summary>
        /// EN: [Terminal ID (TCP)] Reads the Terminal ID over the network. The DLL has no COM version.
        /// AR: [رقم الجهاز (TCP)] يقرأ رقم الجهاز عبر الشبكة. المكتبة لا توفر نسخة COM.
        /// </summary>
        private void btnTerminalId_Click(object sender, EventArgs e)
        {
            byte[] b_ip, b_port;
            if (!TryGetTcpAddress(out b_ip, out b_port))
                return;

            byte[] terminalId = new byte[17];     // EN: 16-char Terminal ID + null   AR: رقم الجهاز 16 حرفاً + حرف النهاية

            string TextString = "07!";            // EN: get terminal ID command   AR: أمر طلب رقم الجهاز
            byte[] inOutBuff = Encoding.ASCII.GetBytes(TextString);
            byte[] intval = new byte[] { (byte)inOutBuff.Length };

            LogStart("Terminal ID (TCP) / رقم الجهاز", TcpTarget(), TextString);
            var Result = api_TCPIPGetTerminalID(b_ip, b_port, inOutBuff, intval, terminalId);
            LogResult(Result);
            LogField("Terminal ID / رقم الجهاز", terminalId);
        }

        // =============================================================================
        // EN: CLEAR / SAVE DEFAULTS / مسح السجل وحفظ الافتراضي
        // =============================================================================

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            Trans_richTextBox.Clear();
        }

        /// <summary>
        /// EN: Writes the current form values into CDLL.exe.config so they become the defaults next time.
        ///     Note: rebuilding the project copies app.config over CDLL.exe.config again. To change the
        ///     defaults permanently, edit app.config too.
        /// AR: يكتب القيم الحالية في CDLL.exe.config لتصبح القيم الافتراضية في المرة القادمة.
        ///     ملاحظة: إعادة بناء المشروع تنسخ app.config فوق CDLL.exe.config مرة أخرى. لتغيير القيم
        ///     الافتراضية بشكل دائم عدّل app.config أيضاً.
        /// </summary>
        private void btnSaveDefaults_Click(object sender, EventArgs e)
        {
            byte[] ip, port;
            string refundRequest;
            if (!TryGetTcpAddress(out ip, out port) || !TryBuildRefundRequest(out refundRequest))
                return;

            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                SaveSetting(config, "PaymentMachinePort", numComPort.Value);
                SaveSetting(config, "BaudRate", numBaudRate.Value);
                SaveSetting(config, "Parity", numParity.Value);
                SaveSetting(config, "DataBits", numDataBits.Value);
                SaveSetting(config, "StopBits", numStopBits.Value);
                SaveSetting(config, "TerminalIp", txtIp.Text.Trim());
                SaveSetting(config, "TerminalTcpPort", txtTcpPort.Text.Trim());
                SaveSetting(config, "PurchaseAmount", numPurchaseAmount.Value);
                SaveSetting(config, "RefundAmount", numRefundAmount.Value);
                SaveSetting(config, "OriginalRrn", txtOriginalRrn.Text.Trim());
                SaveSetting(config, "OriginalDate", dtpOriginalDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                MessageBox.Show("Defaults saved to / تم حفظ القيم الافتراضية في:\n" + config.FilePath, Text);
            }
            catch (ConfigurationErrorsException ex)
            {
                // EN: e.g. the exe folder is read-only (Program Files without admin rights).
                // AR: مثلاً مجلد البرنامج للقراءة فقط (Program Files بدون صلاحيات المسؤول).
                MessageBox.Show("Could not save defaults / تعذر حفظ القيم الافتراضية:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =============================================================================
        // EN: VALUES READ FROM THE FORM / القيم المقروءة من النافذة
        // =============================================================================

        private int ComPort { get { return (int)numComPort.Value; } }
        private int BaudRate { get { return (int)numBaudRate.Value; } }
        private int Parity { get { return (int)numParity.Value; } }
        private int DataBits { get { return (int)numDataBits.Value; } }
        private int StopBits { get { return (int)numStopBits.Value; } }

        /// <summary>
        /// EN: Validates the IP and TCP port boxes and returns them as ASCII bytes + '\0'.
        ///     The original sample sent them without '\0', which only worked if the memory after the
        ///     array happened to be zero; the DLL reads them as C strings.
        /// AR: يتحقق من خانتي IP ومنفذ TCP ويُعيدهما كبايتات ASCII مع '\0'.
        ///     المثال الأصلي كان يرسلهما بدون '\0'، وكان يعمل فقط إذا صادف أن الذاكرة بعد المصفوفة أصفار،
        ///     لأن المكتبة تقرأهما كنصوص C.
        /// </summary>
        private bool TryGetTcpAddress(out byte[] ip, out byte[] port)
        {
            ip = null;
            port = null;
            string ipText = txtIp.Text.Trim();
            string portText = txtTcpPort.Text.Trim();

            IPAddress address;
            if (ipText.Split('.').Length != 4 || !IPAddress.TryParse(ipText, out address) || address.AddressFamily != AddressFamily.InterNetwork)
            {
                ShowInputError(txtIp, "Enter a valid IPv4 address, e.g. 192.168.1.10. / أدخل عنوان IP صحيحاً، مثل 192.168.1.10.");
                return false;
            }

            int portNumber;
            if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out portNumber) || portNumber < 1 || portNumber > 65535)
            {
                ShowInputError(txtTcpPort, "TCP port must be a number from 1 to 65535. / منفذ TCP يجب أن يكون رقماً من 1 إلى 65535.");
                return false;
            }

            ip = Encoding.ASCII.GetBytes(ipText + "\0");
            port = Encoding.ASCII.GetBytes(portText + "\0");
            return true;
        }

        /// <summary>EN: "&lt;halalas&gt;;0;1!" from the Purchase amount box.  AR: "&lt;الهللات&gt;;0;1!" من خانة مبلغ الشراء.</summary>
        private string PurchaseRequest()
        {
            return ToHalalas(numPurchaseAmount.Value) + ";0;1!";
        }

        /// <summary>
        /// EN: "&lt;halalas&gt;;&lt;RRN&gt;;&lt;DDMMYYYY&gt;;1;1!" from the Refund boxes. Returns false (and says why) if the RRN is not 12 digits.
        /// AR: "&lt;الهللات&gt;;&lt;الرقم المرجعي&gt;;&lt;DDMMYYYY&gt;;1;1!" من خانات الاسترجاع. تُعيد false (مع السبب) إذا لم يكن الرقم المرجعي 12 رقماً.
        /// </summary>
        private bool TryBuildRefundRequest(out string request)
        {
            request = null;
            string rrn = txtOriginalRrn.Text.Trim();
            if (rrn.Length != 12 || !IsDigits(rrn))
            {
                ShowInputError(txtOriginalRrn, "Original RRN must be exactly 12 digits. / الرقم المرجعي يجب أن يكون 12 رقماً بالضبط.");
                return false;
            }

            // EN: InvariantCulture: on Arabic (Saudi) Windows the default calendar is Hijri.
            // AR: InvariantCulture: على Windows العربي (السعودية) التقويم الافتراضي هجري.
            string date = dtpOriginalDate.Value.ToString("ddMMyyyy", CultureInfo.InvariantCulture);
            request = ToHalalas(numRefundAmount.Value) + ";" + rrn + ";" + date + ";1;1!";
            return true;
        }

        /// <summary>EN: SAR -> halalas: 43.71 -> "4371".  AR: ريال -> هللة: 43.71 -> "4371".</summary>
        private static string ToHalalas(decimal amountSar)
        {
            decimal halalas = Math.Round(amountSar, 2, MidpointRounding.AwayFromZero) * 100m;
            return ((long)halalas).ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsDigits(string value)
        {
            foreach (char c in value)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }

        private string ComTarget()
        {
            return string.Format(CultureInfo.InvariantCulture, "COM{0} {1} baud, parity {2}, {3} data bits, stop bits {4}",
                ComPort, BaudRate, Parity, DataBits, StopBits);
        }

        private string TcpTarget()
        {
            return txtIp.Text.Trim() + ":" + txtTcpPort.Text.Trim();
        }

        private void ShowInputError(Control control, string message)
        {
            MessageBox.Show(message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            control.Focus();
        }

        // ---- app.config defaults / القيم الافتراضية من app.config

        private static string Setting(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        /// <summary>EN: Sets a number box from app.config, clamped to the box's allowed range.  AR: يضبط خانة رقمية من app.config ضمن المدى المسموح.</summary>
        private static void SetNumber(NumericUpDown box, string key, decimal fallback)
        {
            decimal value;
            if (!decimal.TryParse(Setting(key, ""), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                value = fallback;
            box.Value = Math.Max(box.Minimum, Math.Min(box.Maximum, value));
        }

        private static void SaveSetting(Configuration config, string key, object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (config.AppSettings.Settings[key] == null)
                config.AppSettings.Settings.Add(key, text);
            else
                config.AppSettings.Settings[key].Value = text;
        }

        // =============================================================================
        // EN: OUTPUT / عرض النتائج
        // =============================================================================

        private void LogStart(string operation, string target, string request)
        {
            Trans_richTextBox.AppendText(
                "[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] " + operation + Environment.NewLine +
                "  -> " + target + Environment.NewLine +
                "  Request / الطلب: " + request + Environment.NewLine);
        }

        /// <summary>
        /// EN: Shows the DLL return code in the log and in a message box (as the original sample did).
        /// AR: يعرض رمز الإرجاع من المكتبة في السجل وفي رسالة (كما كان يفعل المثال الأصلي).
        /// </summary>
        private void LogResult(IntPtr result)
        {
            LogText("Return code / رمز الإرجاع", result.ToString());
            MessageBox.Show("Return code / رمز الإرجاع: " + result, Text);
        }

        private void LogTransaction(byte[] panNo, byte[] schemeId, byte[] trxRrn, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] ecrrefno)
        {
            LogField("PAN NO / رقم البطاقة", panNo);
            LogField("CARD ID / نوع البطاقة", schemeId);
            LogField("TrxRrn / الرقم المرجعي", trxRrn);
            LogField("Amount / المبلغ", purAmount);
            LogField("STAN / رقم التتبع", stanNo);
            LogField("Date-time / التاريخ والوقت", dataTime);
            LogField("Auth code / رمز الموافقة", authCode);
            LogField("Response code / رمز الاستجابة", rspCode);
            LogField("Terminal ID / رقم الجهاز", terminalId);
            LogField("ECR REF NO / مرجع نقاط البيع", ecrrefno);
        }

        private void LogRecon(byte[] rspCode, byte[] terminalTotal, byte[] hostTotal)
        {
            LogField("Response code / رمز الاستجابة", rspCode);
            LogField("Terminal total / مجاميع الجهاز", terminalTotal);
            LogField("Host total / مجاميع البنك", hostTotal);
        }

        private void LogField(string label, byte[] buffer)
        {
            LogText(label, Read(buffer));
        }

        private void LogText(string label, string value)
        {
            Trans_richTextBox.AppendText("  " + label + ": " + value + Environment.NewLine);
            Trans_richTextBox.ScrollToCaret();
        }

        /// <summary>
        /// EN: Text the DLL wrote into a buffer, up to the first '\0' (the original printed the trailing '\0's too).
        /// AR: النص الذي كتبته المكتبة في المخزن حتى أول '\0' (الأصل كان يطبع أحرف '\0' الزائدة أيضاً).
        /// </summary>
        private static string Read(byte[] buffer)
        {
            int end = Array.IndexOf(buffer, (byte)0);
            return Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end).Trim();
        }
    }
}
