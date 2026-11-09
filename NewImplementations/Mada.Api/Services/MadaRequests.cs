using System.Globalization;

namespace Mada.Api.Services;

/// <summary>
/// EN: Builds the request strings sent to the terminal. Formats are taken from the old sample (CDLL\Form1.cs).
/// AR: يبني نصوص الطلبات المُرسلة إلى الجهاز. الصيغ مأخوذة من المثال القديم (CDLL\Form1.cs).
/// </summary>
public static class MadaRequests
{
    public const string CheckStatus = "04!";     // EN: check status        AR: فحص الحالة
    public const string TerminalId = "07!";      // EN: terminal ID         AR: رقم الجهاز
    public const string Reconciliation = "09;0!"; // EN: reconciliation     AR: التسوية
    public const string MerchantDetails = "0A!"; // EN: merchant details    AR: بيانات التاجر
    public const string RepeatLast = "0B!";      // EN: repeat last trxn    AR: إعادة آخر عملية

    /// <summary>EN: trnxType values passed to api_Request*Trxn.  AR: قيم trnxType التي تُمرَّر إلى api_Request*Trxn.</summary>
    public const int PurchaseType = 0;
    public const int RefundType = 2;

    // EN: purAmount buffer is 13 bytes = 12 digits + '\0'.   AR: مخزن purAmount بحجم 13 بايت = 12 رقماً + '\0'.
    private const long MaxHalalas = 999_999_999_999;

    /// <summary>
    /// EN: "&lt;halalas&gt;;0;1!". The meaning of the trailing "0;1" flags is not documented in the sample,
    ///     so they are kept exactly as the old sample sent them. Confirm with the vendor spec.
    /// AR: "&lt;الهللات&gt;;0;1!". معنى العلامتين "0;1" غير موثّق في المثال، لذلك أُبقيتا كما أرسلهما المثال
    ///     القديم تماماً. تأكد منهما من وثيقة المورّد.
    /// </summary>
    public static string Purchase(decimal amountSar) => $"{ToHalalas(amountSar)};0;1!";

    /// <summary>
    /// EN: "&lt;halalas&gt;;&lt;original RRN&gt;;&lt;DDMMYYYY&gt;;1;1!". Ends with "!" like Refund_USB in the old sample
    ///     (Refund_TCP there had no "!", which looked like a typo).
    /// AR: "&lt;الهللات&gt;;&lt;الرقم المرجعي الأصلي&gt;;&lt;DDMMYYYY&gt;;1;1!". ينتهي بـ "!" مثل Refund_USB في المثال القديم
    ///     (زر Refund_TCP هناك كان بدون "!"، ويبدو أنه خطأ مطبعي).
    /// </summary>
    public static string Refund(decimal amountSar, string originalRrn, DateOnly originalDate)
    {
        if (originalRrn is null || originalRrn.Length != 12 || !originalRrn.All(char.IsAsciiDigit))
            throw new ArgumentException("OriginalRrn must be exactly 12 digits. / الرقم المرجعي يجب أن يكون 12 رقماً بالضبط.");

        // EN: InvariantCulture is REQUIRED. On a Windows machine set to Arabic (Saudi Arabia) the default
        //     calendar is Umm al-Qura (Hijri), so a plain ToString("ddMMyyyy") would send a Hijri date.
        // AR: استخدام InvariantCulture ضروري. على جهاز Windows بإعدادات العربية (السعودية) التقويم الافتراضي
        //     هو أم القرى (هجري)، فاستدعاء ToString("ddMMyyyy") العادي سيرسل تاريخاً هجرياً.
        string date = originalDate.ToString("ddMMyyyy", CultureInfo.InvariantCulture);
        return $"{ToHalalas(amountSar)};{originalRrn};{date};1;1!";
    }

    /// <summary>
    /// EN: SAR -> halalas (43.71 -> 4371). Rejects zero/negative, more than 2 decimals, or more than 12 digits.
    /// AR: تحويل الريال إلى هللات (43.71 -> 4371). يرفض الصفر والسالب، وأكثر من خانتين عشريتين، وأكثر من 12 رقماً.
    /// </summary>
    public static long ToHalalas(decimal amountSar)
    {
        if (amountSar <= 0)
            throw new ArgumentException("Amount must be greater than zero. / المبلغ يجب أن يكون أكبر من صفر.");

        decimal halalas = amountSar * 100m;
        if (halalas != decimal.Truncate(halalas))
            throw new ArgumentException("Amount can have at most 2 decimal places. / المبلغ يقبل خانتين عشريتين كحد أقصى.");
        if (halalas > MaxHalalas)
            throw new ArgumentException("Amount is too large. / المبلغ كبير جداً.");

        return (long)halalas;
    }
}
