// TCP/IP Integration Sample Code
// =====================================================================================
// EN: Sample Windows Forms app (ECR = Electronic Cash Register) that talks to a mada
//     payment terminal through the vendor's native DLLs (madaapi_v1_9.dll / madaapi_v1_8.dll).
//     Each button on the form runs one terminal operation over either:
//       - USB / serial (COM port)  -> functions with "COM" in the name
//       - TCP/IP (network)         -> functions with "TCPIP" in the name
// AR: تطبيق تجريبي (Windows Forms) يمثّل نظام نقاط البيع (ECR) ويتواصل مع جهاز الدفع "مدى"
//     عبر مكتبات المورّد الأصلية (madaapi_v1_9.dll و madaapi_v1_8.dll).
//     كل زر في النافذة ينفّذ عملية واحدة على الجهاز عبر إحدى الطريقتين:
//       - USB / منفذ تسلسلي (COM)   -> الدوال التي تحتوي أسماؤها على "COM"
//       - شبكة TCP/IP               -> الدوال التي تحتوي أسماؤها على "TCPIP"
//
// -------------------------------------------------------------------------------------
// EN: Button -> handler -> operation map (button texts come from Form1.Designer.cs)
// AR: خريطة الأزرار -> الدالة -> العملية (نصوص الأزرار من ملف Form1.Designer.cs)
//
//   Button text         Handler                  Operation (EN)                 العملية (AR)
//   -----------------   ----------------------   ----------------------------   -----------------------
//   Check status        button6_Click            Terminal status (COM)          فحص حالة الجهاز (COM)
//   TCP_Checkstatus     CheckStatus_button_Click Terminal status (TCP)          فحص حالة الجهاز (TCP)
//   Purchase_USB        button2_Click            Purchase (COM)                 عملية شراء (COM)
//   Purchase_TCP        button3_Click_1          Purchase (TCP)                 عملية شراء (TCP)
//   Recon_USB           Recon_TCP_Click  (!)     Reconciliation (COM)           التسوية / إقفال اليوم (COM)
//   Recon_TCP           Recn_button_Click        Reconciliation (TCP)           التسوية / إقفال اليوم (TCP)
//   Refund_USB          button8_Click            Refund (COM)                   عملية استرجاع (COM)
//   Refund_TCP          button7_Click            Refund (TCP)                   عملية استرجاع (TCP)
//   Repeat_USB          button10_Click           Repeat last trxn (COM)         إعادة آخر عملية (COM)
//   Repeat_TCP          button9_Click            Repeat last trxn (TCP)         إعادة آخر عملية (TCP)
//   Get Merchant        button4_Click            Merchant details (COM)         بيانات التاجر (COM)
//   Terminal ID         button5_Click            Terminal ID (TCP)              رقم الجهاز (TCP)
//
//   (!) EN: The method named "Recon_TCP_Click" is wired to the button labelled "Recon_USB"
//           and really uses the COM function. The names are swapped, the behaviour is USB.
//       AR: الدالة المسمّاة "Recon_TCP_Click" مربوطة بالزر "Recon_USB" وتستخدم فعلياً دالة COM.
//           الأسماء معكوسة، لكن السلوك الفعلي هو USB.
//
// -------------------------------------------------------------------------------------
// EN: SHARED PARAMETERS used by the DLL functions
// AR: المعاملات المشتركة المستخدمة في دوال المكتبة
//
//   COM (USB/serial) functions take:  (int port, int Rate, int x, int y, int z, ...)
//     port  EN: COM port number (5 => "COM5"). Check Device Manager for the terminal's port.
//           AR: رقم المنفذ التسلسلي (5 تعني "COM5"). راجع "إدارة الأجهزة" لمعرفة منفذ الجهاز.
//     Rate  EN: Baud rate. Always 38400 in this sample.
//           AR: سرعة النقل (Baud Rate). القيمة دائماً 38400 في هذا المثال.
//     x     EN: Parity   (0 = none, Win32 NOPARITY).
//           AR: التحقق من التماثل Parity  (0 = بدون).
//     y     EN: Data bits (8).
//           AR: عدد بتات البيانات (8).
//     z     EN: Stop bits (0 = one stop bit, Win32 ONESTOPBIT).
//           AR: بتات التوقف (0 = بت توقف واحد).
//     EN: Order matches the unused locals in Recn_button_Click: baudSpeed, parity, dataBits, stopBits.
//     AR: الترتيب مطابق للمتغيرات غير المستخدمة في Recn_button_Click: baudSpeed, parity, dataBits, stopBits.
//
//   TCP/IP functions take:  (byte[] ip, byte[] port, ...)
//     EN: IP address and TCP port of the terminal as ASCII bytes, e.g. "192.168.116.166" and "6100".
//         The IPs are hard-coded per button below. Change them to your terminal's IP.
//     AR: عنوان IP ومنفذ TCP للجهاز كبايتات ASCII، مثل "192.168.116.166" و "6100".
//         العناوين مكتوبة مباشرة داخل كل زر أدناه. غيّرها إلى عنوان جهازك.
//
//   Request (input) parameters / معاملات الطلب (المدخلات):
//     inOutBuff  EN: The request string as ASCII bytes. Requests end with "!".
//                AR: نص الطلب كبايتات ASCII. الطلبات تنتهي بالرمز "!".
//     intval     EN: 1-byte array holding the length of inOutBuff (so a request is at most 255 bytes).
//                AR: مصفوفة ببايت واحد تحمل طول inOutBuff (لذلك الحد الأقصى للطلب 255 بايت).
//     trnxType   EN: Transaction type. Used in this sample: 0 = Purchase, 2 = Refund.
//                AR: نوع العملية. المستخدم في هذا المثال: 0 = شراء، 2 = استرجاع.
//
//   Request strings used in this file / نصوص الطلبات المستخدمة في هذا الملف:
//     "04!"                             EN: Check status          AR: فحص الحالة
//     "07!"                             EN: Get terminal ID       AR: طلب رقم الجهاز
//     "09;0!"  / "09;0;1!"              EN: Reconciliation        AR: التسوية
//     "0A!"                             EN: Get merchant details  AR: طلب بيانات التاجر
//     "0B!"                             EN: Repeat last trxn      AR: إعادة آخر عملية
//     "<amount>;0;1!"                   EN: Purchase              AR: شراء
//     "<amount>;<origRRN>;<DDMMYYYY>;1;1!"  EN: Refund            AR: استرجاع
//
//     EN: <amount> is in halalas, the smallest currency unit: "4371" = 43.71 SAR, "50" = 0.50 SAR.
//         <origRRN> is the 12-digit RRN of the original purchase. <DDMMYYYY> is the original date.
//         The meaning of the trailing "0" / "1" flags is NOT stated in this code.
//         Confirm them in the mada ECR integration document from the vendor.
//     AR: <amount> بالهللة (أصغر وحدة للعملة): "4371" = 43.71 ريال، "50" = 0.50 ريال.
//         <origRRN> هو الرقم المرجعي (12 رقماً) لعملية الشراء الأصلية، و <DDMMYYYY> تاريخها.
//         معنى الأعلام الأخيرة "0" / "1" غير موضّح في هذا الكود.
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
//     EN: Every function returns a status code as IntPtr. The sample shows it in a MessageBox.
//         The numeric meanings are defined by the vendor. The DLL log (bin\Debug\ECRLOG_*.txt)
//         also prints text such as "API_TERM_BUSY" or "communication timeout".
//     AR: كل دالة تُعيد رمز حالة من النوع IntPtr، والمثال يعرضه في رسالة MessageBox.
//         معاني الأرقام يحددها المورّد. سجل المكتبة (bin\Debug\ECRLOG_*.txt) يطبع أيضاً نصوصاً
//         مثل "API_TERM_BUSY" (الجهاز مشغول) أو "communication timeout" (انتهت مهلة الاتصال).
//
// -------------------------------------------------------------------------------------
// EN: KNOWN ISSUES in this sample (documented only, not changed)
// AR: ملاحظات ومشاكل معروفة في هذا المثال (موثّقة فقط، لم يتم تعديلها)
//   1. EN: COM port numbers are hard-coded and differ per button (5, 9, 15, 17). Only button6
//          reads "PaymentMachinePort" from app.config.
//      AR: أرقام منافذ COM مكتوبة مباشرة وتختلف بين الأزرار (5، 9، 15، 17). فقط button6 يقرأ
//          "PaymentMachinePort" من ملف app.config.
//   2. EN: TCP IP addresses are hard-coded and differ per button.
//      AR: عناوين IP مكتوبة مباشرة وتختلف بين الأزرار.
//   3. EN: Encoding.UTF8.GetString(buffer) keeps the trailing '\0' bytes. Use .TrimEnd('\0') for clean text.
//      AR: Encoding.UTF8.GetString(buffer) يُبقي بايتات '\0' في النهاية. استخدم .TrimEnd('\0') لنص نظيف.
//   4. EN: Calls are synchronous on the UI thread, so the window freezes until the terminal answers.
//      AR: الاستدعاءات متزامنة على خيط الواجهة، لذلك تتجمّد النافذة حتى يرد الجهاز.
//   5. EN: Repeat functions load madaapi_v1_8.dll, all others load madaapi_v1_9.dll. Both DLLs
//          must sit next to CDLL.exe (bin\Debug).
//      AR: دوال "إعادة آخر عملية" تحمّل madaapi_v1_8.dll وبقية الدوال تحمّل madaapi_v1_9.dll.
//          يجب أن تكون المكتبتان بجانب CDLL.exe (داخل bin\Debug).
// =====================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;   // EN: DllImport / MarshalAs for calling the native DLL   AR: لاستدعاء المكتبة الأصلية
using System.Reflection;
using System.IO;
using System.Configuration;             // EN: ConfigurationManager, reads app.config   AR: لقراءة إعدادات app.config


namespace CDLL
{
    /// <summary>
    /// EN: Main (and only) window. Declares the native mada API functions and one click handler per operation.
    /// AR: النافذة الرئيسية (والوحيدة). تعرّف دوال مكتبة مدى الأصلية ودالة نقر لكل عملية.
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
        ///     Used by: button2_Click (Purchase_USB), button8_Click (Refund_USB).
        /// AR: يرسل عملية مالية (شراء trnxType=0، استرجاع trnxType=2) عبر USB/COM
        ///     ويملأ مخازن المخرجات بنتيجة البطاقة والعملية.
        ///     مستخدمة في: button2_Click (Purchase_USB) و button8_Click (Refund_USB).
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Same as api_RequestCOMTrxn but over TCP/IP (ip + port instead of COM settings).
        ///     Used by: button3_Click_1 (Purchase_TCP), button7_Click (Refund_TCP).
        /// AR: مثل api_RequestCOMTrxn لكن عبر شبكة TCP/IP (عنوان IP ومنفذ بدلاً من إعدادات COM).
        ///     مستخدمة في: button3_Click_1 (Purchase_TCP) و button7_Click (Refund_TCP).
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestTCPIPTrxn(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Reads the merchant name and address stored in the terminal, in English (byte[])
        ///     and Arabic (StringBuilder, Unicode). Request string: "0A!". USB/COM only.
        ///     Used by: button4_Click (Get Merchant).
        /// AR: يقرأ اسم التاجر وعنوانه المخزّنين في الجهاز بالإنجليزية (byte[]) وبالعربية
        ///     (StringBuilder بترميز يونيكود). نص الطلب: "0A!". عبر USB/COM فقط.
        ///     مستخدمة في: button4_Click (Get Merchant).
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_GetMerchantDetails(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] merchantNameEng, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantNameArb, byte[] merchantAddEng1, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantAddArb1, byte[] merchantAddopt1, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder merchantAddopt2);

        /// <summary>
        /// EN: Reads the Terminal ID over TCP/IP. Request string: "07!".
        ///     Used by: button5_Click (Terminal ID).
        /// AR: يقرأ رقم الجهاز (Terminal ID) عبر TCP/IP. نص الطلب: "07!".
        ///     مستخدمة في: button5_Click (Terminal ID).
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_TCPIPGetTerminalID(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval, byte[] terminalId);

        /// <summary>
        /// EN: Checks that the terminal is connected and ready (USB/COM). Request string: "04!".
        ///     Only the return code tells you the result.
        ///     Used by: button6_Click (Check status).
        /// AR: يتحقق من أن الجهاز متصل وجاهز (USB/COM). نص الطلب: "04!".
        ///     النتيجة تُعرف من رمز الإرجاع فقط.
        ///     مستخدمة في: button6_Click (Check status).
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_CheckStatus(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval);

        /// <summary>
        /// EN: Same as api_CheckStatus but over TCP/IP.
        ///     Used by: CheckStatus_button_Click (TCP_Checkstatus).
        /// AR: مثل api_CheckStatus لكن عبر TCP/IP.
        ///     مستخدمة في: CheckStatus_button_Click (TCP_Checkstatus).
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_TCPIPCheckStatus(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval);

        /// <summary>
        /// EN: Reconciliation (end-of-day settlement) over TCP/IP, WITHOUT printing a receipt ("WORcpt").
        ///     Returns terminal totals vs host (bank) totals so you can compare them.
        ///     NOTE: the request length is passed by reference (ref int inReqlen), not as a 1-byte array.
        ///     Used by: Recn_button_Click (Recon_TCP).
        /// AR: التسوية (إقفال نهاية اليوم) عبر TCP/IP بدون طباعة إيصال ("WORcpt" = Without Receipt).
        ///     تُعيد مجاميع الجهاز ومجاميع البنك (المضيف) للمقارنة بينهما.
        ///     ملاحظة: طول الطلب يُمرَّر بالمرجع (ref int inReqlen) وليس كمصفوفة ببايت واحد.
        ///     مستخدمة في: Recn_button_Click (Recon_TCP).
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestReconTCPIPWORcpt(byte[] ip, byte[] port, byte[] inOutBuff, ref int inReqlen, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        /// <summary>
        /// EN: Reconciliation (end-of-day settlement) over USB/COM, without printing a receipt.
        ///     Used by: Recon_TCP_Click (button labelled "Recon_USB").
        /// AR: التسوية (إقفال نهاية اليوم) عبر USB/COM بدون طباعة إيصال.
        ///     مستخدمة في: Recon_TCP_Click (الزر المكتوب عليه "Recon_USB").
        /// </summary>
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]

        public static extern IntPtr api_RequestReconCOMWORcpt(int port, int Rate, int x, int y, int z, byte[] inOutBuff, byte[] intval, byte[] rspCode, byte[] terminalTotal, byte[] hostTotal, byte[] stanNo, byte[] dataTime, byte[] terminalId, byte[] merchantId);

        /// <summary>
        /// EN: Repeat: asks the terminal again for the result of the LAST transaction over TCP/IP.
        ///     Use it when the ECR lost the response (timeout, crash) and you must know whether
        ///     the customer was charged before retrying. Request string: "0B!".
        ///     Loaded from the OLDER madaapi_v1_8.dll. Has an extra Cardscheme output buffer.
        ///     Used by: button9_Click (Repeat_TCP).
        /// AR: إعادة: يطلب من الجهاز مرة أخرى نتيجة آخر عملية عبر TCP/IP.
        ///     تُستخدم عندما يفقد نظام نقاط البيع الاستجابة (انتهاء المهلة أو تعطّل البرنامج) ويجب
        ///     معرفة هل تم الخصم من العميل قبل إعادة المحاولة. نص الطلب: "0B!".
        ///     تُحمَّل من المكتبة الأقدم madaapi_v1_8.dll، وفيها مخزن إضافي Cardscheme لنوع البطاقة.
        ///     مستخدمة في: button9_Click (Repeat_TCP).
        /// </summary>
        [DllImport(@"madaapi_v1_8.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestTCPIPRepeat(byte[] ip, byte[] port, byte[] inOutBuff, byte[] intval,byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, byte[] Cardscheme, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        /// <summary>
        /// EN: Repeat last transaction over USB/COM (see api_RequestTCPIPRepeat). From madaapi_v1_8.dll.
        ///     Used by: button10_Click (Repeat_USB).
        /// AR: إعادة آخر عملية عبر USB/COM (راجع api_RequestTCPIPRepeat). من المكتبة madaapi_v1_8.dll.
        ///     مستخدمة في: button10_Click (Repeat_USB).
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
        /// EN: Runs when the window opens. Empty: nothing is initialised here.
        /// AR: تُنفَّذ عند فتح النافذة. فارغة: لا يتم تهيئة أي شيء هنا.
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {

        }


        /// <summary>
        /// EN: [Purchase_USB] PURCHASE of 43.71 SAR over USB/COM port 5.
        ///     Shows the return code, then PAN, card scheme and RRN in the text box.
        /// AR: [Purchase_USB] عملية شراء بمبلغ 43.71 ريال عبر USB/COM على المنفذ 5.
        ///     تعرض رمز الإرجاع، ثم رقم البطاقة ونوعها والرقم المرجعي في مربع النص.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            string TextString;
            // EN: Output buffers: the DLL fills them. Size = max length + 1 (null terminator).
            //     See the table at the top of this file for each field's meaning.
            // AR: مخازن المخرجات: المكتبة تملؤها. الحجم = أقصى طول + 1 (حرف النهاية null).
            //     راجع الجدول أعلى الملف لمعنى كل حقل.
            byte[] intval = new byte[1];        // EN: request length (input)     AR: طول الطلب (مدخل)
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

            // EN: 0 = Purchase.   AR: 0 = شراء.
            int trnxType = 0;
            // EN: "<amount in halalas>;0;1!" -> 4371 = 43.71 SAR.   AR: "<المبلغ بالهللة>;0;1!" -> 4371 = 43.71 ريال.
            TextString = "4371;0;1!";
            // EN: Convert request to ASCII bytes and store its length in intval[0].
            // AR: تحويل الطلب إلى بايتات ASCII وتخزين طوله في intval[0].
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

            // EN: Previous call on COM9, kept for reference.   AR: استدعاء سابق على COM9، محفوظ للرجوع إليه.
            //var Result = api_RequestCOMTrxn(9, 38400, 0, 8, 0, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            // EN: COM5, 38400 baud, no parity, 8 data bits, 1 stop bit. Blocks until the customer
            //     taps/inserts the card and the terminal answers.
            // AR: المنفذ COM5، سرعة 38400، بدون Parity، 8 بتات بيانات، بت توقف واحد. ينتظر حتى
            //     يمرّر العميل بطاقته ويرد الجهاز.
            var Result = api_RequestCOMTrxn(5, 38400, 0, 8, 0, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            // EN: Show the DLL status code.   AR: عرض رمز الحالة من المكتبة.
            MessageBox.Show(Result.ToString());

            // EN: Decode the filled buffers to text and display them.
            // AR: تحويل المخازن المملوءة إلى نص وعرضها.
            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(schemeId);
            Trans_richTextBox.AppendText("\nCARD ID: " + result);

            result = System.Text.Encoding.UTF8.GetString(trxRrn);
            Trans_richTextBox.AppendText("\nTrxRrn : " + result);

        }

        /// <summary>
        /// EN: [Purchase_TCP] PURCHASE of 0.50 SAR over TCP/IP to 192.168.116.166:6100.
        ///     Shows the return code, then PAN and ECR reference number.
        /// AR: [Purchase_TCP] عملية شراء بمبلغ 0.50 ريال عبر TCP/IP إلى 192.168.116.166:6100.
        ///     تعرض رمز الإرجاع، ثم رقم البطاقة والرقم المرجعي لنقاط البيع.
        /// </summary>
        private void button3_Click_1(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            // EN: Output buffers (same layout as button2_Click).   AR: مخازن المخرجات (نفس ترتيب button2_Click).
            byte[] intval = new byte[1];
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

            // EN: 0 = Purchase, amount 50 halalas = 0.50 SAR.   AR: 0 = شراء، المبلغ 50 هللة = 0.50 ريال.
            int trnxType = 0;
            TextString = "50;0;1!";
            // EN: Terminal network address. Change to your terminal's IP.
            // AR: عنوان الجهاز على الشبكة. غيّره إلى عنوان IP الخاص بجهازك.
            ip = "192.168.116.166";
            port = "6100";
            // EN: The DLL expects ASCII bytes for request, IP and port.
            // AR: المكتبة تتوقع بايتات ASCII للطلب وعنوان IP والمنفذ.
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_RequestTCPIPTrxn(b_ip, b_port, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            // EN: No "\n" before this label, so it prints on the same line as the PAN.
            // AR: لا يوجد "\n" قبل هذا النص، لذلك يُطبع على نفس سطر رقم البطاقة.
            result = System.Text.Encoding.UTF8.GetString(ecrrefno);
            Trans_richTextBox.AppendText("ECR REF NO: " + result);
        }

        /// <summary>
        /// EN: [Get Merchant] Reads merchant name/address (English + Arabic) over USB/COM port 15.
        /// AR: [Get Merchant] يقرأ اسم التاجر وعنوانه (إنجليزي + عربي) عبر USB/COM على المنفذ 15.
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            string TextString;
            byte[] intval = new byte[1];
            // EN: English fields come back as single-byte text in byte[] buffers.
            // AR: الحقول الإنجليزية تعود كنص أحادي البايت داخل مصفوفات byte[].
            byte[] merchantNameEng = new byte[500];
            byte[] merchantAddEng1 = new byte[500];
            byte[] merchantAddopt1 = new byte[500];
            // EN: Arabic fields need Unicode, so the DLL writes them into wide-char StringBuilders.
            // AR: الحقول العربية تحتاج يونيكود، لذلك تكتبها المكتبة داخل StringBuilder بأحرف عريضة.
            StringBuilder merchantNameArb = new StringBuilder(500);
            StringBuilder merchantAddArb1 = new StringBuilder(500);
            StringBuilder merchantAddopt2 = new StringBuilder(500);

            // EN: Not used by api_GetMerchantDetails.   AR: غير مستخدم في api_GetMerchantDetails.
            int trnxType = 0;
            // EN: "0A!" = get merchant details command.   AR: "0A!" = أمر طلب بيانات التاجر.
            TextString = "0A!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

            // EN: COM15, 38400 8N1.   AR: المنفذ COM15، الإعدادات 38400 8N1.
            var Result = api_GetMerchantDetails(15, 38400, 0, 8, 0, inOutBuff, intval,merchantNameEng, merchantNameArb, merchantAddEng1, merchantAddArb1, merchantAddopt1, merchantAddopt2);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(merchantNameEng);
            Trans_richTextBox.AppendText("MerchantNameEng: " + result);

            // EN: StringBuilder already holds .NET text, so just call ToString().
            // AR: StringBuilder يحتوي نص ‎.NET جاهز، لذلك يكفي استدعاء ToString().
            result = merchantNameArb.ToString();
            Trans_richTextBox.AppendText("\nMerchantNameArb: " + result);

            result = System.Text.Encoding.UTF8.GetString(merchantAddEng1);
            Trans_richTextBox.AppendText("\nMerchantAddEng1: " + result);

            result = merchantAddArb1.ToString();
            Trans_richTextBox.AppendText("\nMerchantAddArb1: " + result);
        }

        /// <summary>
        /// EN: [Terminal ID] Reads the Terminal ID over TCP/IP from 192.168.208.157:6100.
        /// AR: [Terminal ID] يقرأ رقم الجهاز عبر TCP/IP من 192.168.208.157:6100.
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            byte[] intval = new byte[1];
            // EN: Output: 16-char Terminal ID + null.   AR: المخرج: رقم الجهاز 16 حرفاً + حرف النهاية.
            byte[] terminalId = new byte[17];

            // EN: Not used by this call.   AR: غير مستخدم في هذا الاستدعاء.
            int trnxType = 0;
            // EN: "07!" = get terminal ID command.   AR: "07!" = أمر طلب رقم الجهاز.
            TextString = "07!";
            ip = "192.168.208.157";
            port = "6100";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_TCPIPGetTerminalID(b_ip, b_port, inOutBuff, intval, terminalId);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(terminalId);
            Trans_richTextBox.AppendText("Terminal ID: " + result);
        }

        /// <summary>
        /// EN: [Check status] Checks the terminal over USB/COM. The ONLY handler that reads the
        ///     COM port from app.config ("PaymentMachinePort"), so you can change the port without recompiling.
        /// AR: [Check status] يفحص حالة الجهاز عبر USB/COM. هذه الدالة الوحيدة التي تقرأ رقم المنفذ
        ///     من app.config ("PaymentMachinePort")، فيمكن تغيير المنفذ بدون إعادة البناء.
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            string TextString;
            // EN: ip, port, terminalId, trnxType are declared but unused here.
            // AR: المتغيرات ip و port و terminalId و trnxType معرّفة لكنها غير مستخدمة هنا.
            string ip;
            string port;
            byte[] intval = new byte[1];
            byte[] terminalId = new byte[17];

            int trnxType = 0;
            // EN: "04!" = check status command.   AR: "04!" = أمر فحص الحالة.
            TextString = "04!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;
            // EN: Read COM port number from app.config -> <appSettings><add key="PaymentMachinePort" .../>.
            //     Throws FormatException if the value is missing or not a number.
            // AR: قراءة رقم منفذ COM من app.config -> <appSettings><add key="PaymentMachinePort" .../>.
            //     يرمي استثناء FormatException إذا كانت القيمة مفقودة أو ليست رقماً.
            int portNumber = Convert.ToInt32(ConfigurationManager.AppSettings["PaymentMachinePort"]);
            var Result = api_CheckStatus(portNumber, 38400, 0, 8, 0, inOutBuff, intval);
            // EN: Old hard-coded COM15 call.   AR: الاستدعاء القديم بمنفذ COM15 الثابت.
            //var Result = api_CheckStatus(15, 38400, 0, 8, 0, inOutBuff, intval);
            MessageBox.Show(Result.ToString());
        }

        /// <summary>
        /// EN: [Refund_TCP] REFUND of 1.00 SAR over TCP/IP to 192.168.241.157:6100.
        ///     Needs the original purchase's RRN and date.
        /// AR: [Refund_TCP] عملية استرجاع بمبلغ 1.00 ريال عبر TCP/IP إلى 192.168.241.157:6100.
        ///     تحتاج الرقم المرجعي (RRN) وتاريخ عملية الشراء الأصلية.
        /// </summary>
        private void button7_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            // EN: Output buffers (same layout as button2_Click).   AR: مخازن المخرجات (نفس ترتيب button2_Click).
            byte[] intval = new byte[1];
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

            // EN: 2 = Refund.   AR: 2 = استرجاع.
            int trnxType = 2;
            // EN: "<amount>;<original RRN>;<original date DDMMYYYY>;1;1"
            //     = 1.00 SAR, RRN 123456781234, date 13/11/2018 (test values).
            //     NOTE: no trailing "!" here, unlike Refund_USB. Check the vendor spec.
            // AR: "<المبلغ>;<الرقم المرجعي الأصلي>;<التاريخ الأصلي DDMMYYYY>;1;1"
            //     = 1.00 ريال، الرقم المرجعي 123456781234، التاريخ 13/11/2018 (قيم تجريبية).
            //     ملاحظة: لا يوجد "!" في النهاية هنا بخلاف Refund_USB. راجع وثيقة المورّد.
            TextString = "100;123456781234;13112018;1;1";
            ip = "192.168.241.157";
            port = "6100";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_RequestTCPIPTrxn(b_ip, b_port, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(ecrrefno);
            Trans_richTextBox.AppendText("ECR REF NO: " + result);
        }

        /// <summary>
        /// EN: [TCP_Checkstatus] Checks the terminal over TCP/IP at 192.168.208.157:6100.
        /// AR: [TCP_Checkstatus] يفحص حالة الجهاز عبر TCP/IP على 192.168.208.157:6100.
        /// </summary>
        private void CheckStatus_button_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;

            ip = "192.168.208.157";
            port = "6100";
            byte[] intval = new byte[1];

            // EN: Not used by this call.   AR: غير مستخدم في هذا الاستدعاء.
            int trnxType = 0;
            // EN: "04!" = check status command.   AR: "04!" = أمر فحص الحالة.
            TextString = "04!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_TCPIPCheckStatus(b_ip, b_port, inOutBuff, intval);
            MessageBox.Show(Result.ToString());
        }

        /// <summary>
        /// EN: [Recon_TCP] RECONCILIATION (end-of-day settlement) over TCP/IP to 192.168.116.166:6100.
        ///     Displays terminal totals and host (bank) totals. They should match.
        ///     The return code is NOT shown in a MessageBox here.
        /// AR: [Recon_TCP] التسوية (إقفال نهاية اليوم) عبر TCP/IP إلى 192.168.116.166:6100.
        ///     تعرض مجاميع الجهاز ومجاميع البنك (المضيف)، ويجب أن تتطابق.
        ///     رمز الإرجاع لا يُعرض في رسالة هنا.
        /// </summary>
        private void Recn_button_Click(object sender, EventArgs e)
        {
            // EN: Request buffer. Re-assigned below, so the 500-byte allocation is discarded.
            // AR: مخزن الطلب. يُعاد إسناده لاحقاً، لذلك يتم تجاهل الحجز الأولي (500 بايت).
            byte[] inReqbuff = new byte[100 * 5];
            string ip;
            string port;
            byte[] intval = new byte[1];
            // EN: Recon output buffers (sized generously, not exact field lengths).
            // AR: مخازن مخرجات التسوية (بأحجام كبيرة احتياطاً، وليست أطوال الحقول الدقيقة).
            byte[] rspCode = new byte[30];        // EN: response code      AR: رمز الاستجابة
            byte[] terminalTotal = new byte[500]; // EN: terminal totals    AR: مجاميع الجهاز
            byte[] hostTotal = new byte[500];     // EN: host/bank totals   AR: مجاميع البنك
            byte[] stanNo = new byte[30];
            byte[] dataTime = new byte[30];
            byte[] terminalId = new byte[30];
            byte[] merchantId = new byte[30];
            // EN: Unused.   AR: غير مستخدم.
            byte[] outresp = new byte[20000];

            // EN: outRsplen, baudSpeed, parity, dataBits, stopBits are unused (leftovers from a COM version).
            // AR: المتغيرات outRsplen و baudSpeed و parity و dataBits و stopBits غير مستخدمة (بقايا من نسخة COM).
            int inReqlen = 0, outRsplen = 0;
            uint baudSpeed = 0, parity = 0, dataBits = 0, stopBits = 0;

            Array.Clear(outresp, 0, outresp.Length);
            Array.Clear(inReqbuff, 0, inReqbuff.Length);

            // EN: THIS is the request actually sent: "09;0!" (reconciliation command).
            // AR: هذا هو الطلب المُرسل فعلياً: "09;0!" (أمر التسوية).
            string send_buff = "";
            send_buff += "09;0!";
            inReqbuff = Encoding.ASCII.GetBytes(send_buff);
            inReqlen = inReqbuff.Length;
            // EN: TextString / inOutBuff / intval below are built but NOT passed to the DLL.
            // AR: المتغيرات TextString و inOutBuff و intval أدناه تُجهَّز لكنها لا تُمرَّر إلى المكتبة.
            string TextString;
            TextString = "09;0;1!";
            ip = "192.168.116.166";
            port = "6100";

            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;


            // EN: inReqlen is passed by reference ("ref"), the DLL may update it.
            // AR: inReqlen يُمرَّر بالمرجع ("ref")، وقد تقوم المكتبة بتعديله.
            var Result  = api_RequestReconTCPIPWORcpt(b_ip, b_port, inReqbuff, ref inReqlen, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

            string result = System.Text.Encoding.UTF8.GetString(terminalTotal);
            Trans_richTextBox.AppendText("\nTerminalTotal: " + result);

            result = System.Text.Encoding.UTF8.GetString(hostTotal);
            Trans_richTextBox.AppendText("\nHostTotal: " + result);
        }

        /// <summary>
        /// EN: [Recon_USB] RECONCILIATION over USB/COM port 9.
        ///     Despite the method name "Recon_TCP_Click", this is the USB button and uses the COM function.
        /// AR: [Recon_USB] التسوية عبر USB/COM على المنفذ 9.
        ///     رغم أن اسم الدالة "Recon_TCP_Click"، فهذا زر USB ويستخدم دالة COM.
        /// </summary>
        private void Recon_TCP_Click(object sender, EventArgs e)
        {
            // EN: inReqbuff, outresp, outRespLen, outResp are unused here.
            // AR: المتغيرات inReqbuff و outresp و outRespLen و outResp غير مستخدمة هنا.
            byte[] inReqbuff = new byte[100 * 5];
            byte[] intval = new byte[1];
            byte[] rspCode = new byte[30];
            byte[] terminalTotal = new byte[500];
            byte[] hostTotal = new byte[500];
            byte[] stanNo = new byte[30];
            byte[] dataTime = new byte[30];
            byte[] terminalId = new byte[30];
            byte[] merchantId = new byte[30];
            byte[] outresp = new byte[20000];
            byte[] outRespLen = new byte[1];
            StringBuilder outResp = new StringBuilder(15000);

            int inReqlen = 0;
            Array.Clear(outresp, 0, outresp.Length);
            Array.Clear(inReqbuff, 0, inReqbuff.Length);

            // EN: "09;0!" = reconciliation command.   AR: "09;0!" = أمر التسوية.
            string TextString = "09;0!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

            // EN: COM9, 38400 8N1.   AR: المنفذ COM9، الإعدادات 38400 8N1.
            var Result = api_RequestReconCOMWORcpt(9, 38400, 0, 8, 0, inOutBuff, intval, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

            string result = System.Text.Encoding.UTF8.GetString(terminalTotal);
            Trans_richTextBox.AppendText("\nTerminalTotal: " + result);

            result = System.Text.Encoding.UTF8.GetString(hostTotal);
            Trans_richTextBox.AppendText("\nHostTotal: " + result);

        }

        /// <summary>
        /// EN: [Refund_USB] REFUND of 0.50 SAR over USB/COM port 5, for original RRN 434615455456 dated 11/12/2024.
        /// AR: [Refund_USB] عملية استرجاع بمبلغ 0.50 ريال عبر USB/COM على المنفذ 5، للعملية الأصلية
        ///     ذات الرقم المرجعي 434615455456 بتاريخ 11/12/2024.
        /// </summary>
        private void button8_Click(object sender, EventArgs e)
        {
            string TextString;
            // EN: Output buffers (same layout as button2_Click).   AR: مخازن المخرجات (نفس ترتيب button2_Click).
            byte[] intval = new byte[1];
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

            // EN: 2 = Refund.   AR: 2 = استرجاع.
            int trnxType = 2;
            // EN: "<amount>;<original RRN>;<original date DDMMYYYY>;1;1!"
            // AR: "<المبلغ>;<الرقم المرجعي الأصلي>;<التاريخ الأصلي DDMMYYYY>;1;1!"
            TextString = "50;434615455456;11122024;1;1!";


            // EN: Older test request with an extra field, kept for reference.
            // AR: طلب تجريبي قديم فيه حقل إضافي، محفوظ للرجوع إليه.
            //TextString = "100;123456781234;13112018;4153820015307984;0;1";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;

            // EN: Previous call on COM9, kept for reference.   AR: استدعاء سابق على COM9، محفوظ للرجوع إليه.
            //var Result = api_RequestCOMTrxn(9, 38400, 0, 8, 0, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            var Result = api_RequestCOMTrxn(5, 38400, 0, 8, 0, inOutBuff, intval, trnxType, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(schemeId);
            Trans_richTextBox.AppendText("\nCARD ID: " + result);
        }

        /// <summary>
        /// EN: [Repeat_TCP] Re-fetches the LAST transaction result over TCP/IP from 192.168.8.174:6100.
        ///     Uses madaapi_v1_8.dll.
        /// AR: [Repeat_TCP] يعيد جلب نتيجة آخر عملية عبر TCP/IP من 192.168.8.174:6100.
        ///     يستخدم المكتبة madaapi_v1_8.dll.
        /// </summary>
        private void button9_Click(object sender, EventArgs e)
        {
            string TextString;
            string ip;
            string port;
            // EN: Output buffers (same layout as button2_Click) + cardscheme.
            // AR: مخازن المخرجات (نفس ترتيب button2_Click) بالإضافة إلى cardscheme.
            byte[] intval = new byte[1];
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


            // EN: "0B!" = repeat last transaction command.   AR: "0B!" = أمر إعادة آخر عملية.
            TextString = "0B!";
            ip = "192.168.8.174";
            port = "6100";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            byte[] b_ip = System.Text.Encoding.ASCII.GetBytes(ip);
            byte[] b_port = System.Text.Encoding.ASCII.GetBytes(port);
            intval[0] = (byte)inOutBuff.Length;

            var Result = api_RequestTCPIPRepeat(b_ip, b_port, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, cardscheme, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("\nPAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(ecrrefno);
            Trans_richTextBox.AppendText("\nECR REF NO: " + result);
        }

        /// <summary>
        /// EN: [Repeat_USB] Re-fetches the LAST transaction result over USB/COM port 17.
        ///     Uses madaapi_v1_8.dll.
        /// AR: [Repeat_USB] يعيد جلب نتيجة آخر عملية عبر USB/COM على المنفذ 17.
        ///     يستخدم المكتبة madaapi_v1_8.dll.
        /// </summary>
        private void button10_Click(object sender, EventArgs e)
        {
            string TextString;
            // EN: Output buffers (same layout as button2_Click).   AR: مخازن المخرجات (نفس ترتيب button2_Click).
            byte[] intval = new byte[1];
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


            // EN: "0B!" = repeat last transaction command.   AR: "0B!" = أمر إعادة آخر عملية.
            TextString = "0B!";
            byte[] inOutBuff = System.Text.Encoding.ASCII.GetBytes(TextString);
            intval[0] = (byte)inOutBuff.Length;


            // EN: COM17, 38400 8N1.   AR: المنفذ COM17، الإعدادات 38400 8N1.
            var Result = api_RequestCOMRepeat(17, 38400, 0, 8, 0, inOutBuff, intval, panNo, purAmount, stanNo, dataTime, expDate, trxRrn, authCode, rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno, version, outResp, outRespLen);
            MessageBox.Show(Result.ToString());

            string result = System.Text.Encoding.UTF8.GetString(panNo);
            Trans_richTextBox.AppendText("PAN NO: " + result);

            result = System.Text.Encoding.UTF8.GetString(schemeId);
            Trans_richTextBox.AppendText("\nCARD ID: " + result);

            result = System.Text.Encoding.UTF8.GetString(trxRrn);
            Trans_richTextBox.AppendText("\nTrxRrn : " + result);
        }
    }
}
