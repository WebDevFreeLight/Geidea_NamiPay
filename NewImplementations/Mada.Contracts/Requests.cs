namespace Mada.Contracts;

/// <summary>
/// EN: How the API reaches the terminal. Sent in JSON as the string "Tcp" or "Com".
/// AR: طريقة وصول الخدمة إلى الجهاز. تُرسل في JSON كنص "Tcp" أو "Com".
/// </summary>
public enum Transport
{
    /// <summary>EN: Network (TCP/IP).  AR: عبر الشبكة (TCP/IP).</summary>
    Tcp,

    /// <summary>EN: USB / serial COM port.  AR: عبر USB / منفذ COM التسلسلي.</summary>
    Com,
}

/// <summary>
/// EN: Purchase request. Amount is in SAR with up to 2 decimals (43.71). The API converts it to halalas.
///     Transport is optional: null = use "Terminal:DefaultTransport" from the API's appsettings.json.
/// AR: طلب شراء. المبلغ بالريال وبحد أقصى خانتين عشريتين (43.71)، والخدمة تحوّله إلى هللات.
///     الاتصال اختياري: null = استخدام "Terminal:DefaultTransport" من appsettings.json في الخدمة.
/// </summary>
public sealed record PurchaseRequest(decimal Amount, Transport? Transport = null);

/// <summary>
/// EN: Refund request. Needs the original purchase's 12-digit RRN and its date.
/// AR: طلب استرجاع. يحتاج الرقم المرجعي (12 رقماً) لعملية الشراء الأصلية وتاريخها.
/// </summary>
public sealed record RefundRequest(decimal Amount, string OriginalRrn, DateOnly OriginalDate, Transport? Transport = null);

/// <summary>
/// EN: Body for operations that only need to know the transport (repeat, reconciliation).
/// AR: جسم الطلب للعمليات التي تحتاج فقط نوع الاتصال (إعادة آخر عملية، التسوية).
/// </summary>
public sealed record TransportRequest(Transport? Transport = null);
