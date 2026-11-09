namespace Mada.Contracts;

// EN: StatusCode in every result = the raw return value of the mada DLL function.
//     Its meanings are defined by the vendor; this code does not interpret it.
// AR: StatusCode في كل نتيجة = القيمة الخام المُعادة من دالة مكتبة مدى.
//     معانيها يحددها المورّد، وهذا الكود لا يفسّرها.

/// <summary>EN: Result of a terminal status check.  AR: نتيجة فحص حالة الجهاز.</summary>
public sealed record StatusResult(int StatusCode, Transport Transport);

/// <summary>EN: Terminal ID (TCP only, the DLL has no COM variant).  AR: رقم الجهاز (عبر TCP فقط، المكتبة لا توفر نسخة COM).</summary>
public sealed record TerminalIdResult(int StatusCode, string TerminalId);

/// <summary>
/// EN: Merchant name/address stored in the terminal, in English and Arabic (COM only, the DLL has no TCP variant).
/// AR: اسم التاجر وعنوانه المخزّنين في الجهاز بالإنجليزية والعربية (عبر COM فقط، المكتبة لا توفر نسخة TCP).
/// </summary>
public sealed record MerchantResult(
    int StatusCode,
    string NameEnglish,
    string NameArabic,
    string AddressEnglish,
    string AddressArabic,
    string AddressOptional1,
    string AddressOptional2);

/// <summary>
/// EN: Result of a purchase, refund or repeat. Field values are returned exactly as the terminal wrote them.
/// AR: نتيجة الشراء أو الاسترجاع أو إعادة آخر عملية. القيم تُعاد كما كتبها الجهاز تماماً.
/// </summary>
public sealed record TransactionResult(
    int StatusCode,
    Transport Transport,
    string Pan,                 // EN: card number, masked by the terminal   AR: رقم البطاقة (مُقنّع من الجهاز)
    string Amount,
    string Stan,                // EN: trace number                          AR: رقم التتبع
    string DateTime,
    string ExpiryDate,
    string Rrn,                 // EN: 12-digit reference, needed for refunds AR: الرقم المرجعي (12 رقماً)، مطلوب للاسترجاع
    string AuthCode,            // EN: approval code                         AR: رمز الموافقة
    string ResponseCode,        // EN: host response code                    AR: رمز الاستجابة من البنك
    string TerminalId,
    string SchemeId,            // EN: card scheme ID                        AR: نوع شبكة البطاقة
    string MerchantId,
    string AdditionalAmount,
    string EcrRefNo,
    string Version,
    string? CardScheme,         // EN: only filled by TCP repeat             AR: يُملأ فقط في إعادة العملية عبر TCP
    string? RawResponse)        // EN: null unless "Terminal:IncludeRawResponse" is true   AR: فارغ ما لم يتم تفعيل "Terminal:IncludeRawResponse"
{
    /// <summary>
    /// EN: True when the host response code is "000". mada uses 3-digit codes where 000 = approved.
    ///     Confirm the full code list with the vendor before relying on anything beyond this.
    /// AR: صحيح عندما يكون رمز الاستجابة "000". مدى تستخدم رموزاً من 3 أرقام و 000 = مقبولة.
    ///     تأكد من قائمة الرموز الكاملة من المورّد قبل الاعتماد على أكثر من ذلك.
    /// </summary>
    public bool IsApproved => ResponseCode == "000";
}

/// <summary>
/// EN: End-of-day reconciliation result. TerminalTotal and HostTotal should match.
/// AR: نتيجة التسوية (إقفال اليوم). يجب أن تتطابق مجاميع الجهاز (TerminalTotal) مع مجاميع البنك (HostTotal).
/// </summary>
public sealed record ReconciliationResult(
    int StatusCode,
    Transport Transport,
    string ResponseCode,
    string TerminalTotal,
    string HostTotal,
    string Stan,
    string DateTime,
    string TerminalId,
    string MerchantId);
