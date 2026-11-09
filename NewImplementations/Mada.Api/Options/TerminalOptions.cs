using Mada.Contracts;

namespace Mada.Api.Options;

/// <summary>
/// EN: Bound from the "Terminal" section of appsettings.json. Replaces the hard-coded ports/IPs of the old sample.
///     Any value can be overridden by an environment variable, e.g. Terminal__Com__Port=9.
/// AR: تُقرأ من قسم "Terminal" في appsettings.json، وتستبدل المنافذ وعناوين IP المكتوبة مباشرة في المثال القديم.
///     يمكن تجاوز أي قيمة بمتغير بيئة، مثل Terminal__Com__Port=9.
/// </summary>
public sealed class TerminalOptions
{
    public const string SectionName = "Terminal";

    /// <summary>
    /// EN: true = fake terminal (no DLL, no hardware), for developing the client. On in appsettings.Development.json.
    /// AR: true = جهاز وهمي (بدون مكتبة وبدون جهاز فعلي) لتطوير الواجهة. مفعّل في appsettings.Development.json.
    /// </summary>
    public bool UseSimulator { get; set; }

    /// <summary>EN: Used when a request does not specify a transport.  AR: يُستخدم عندما لا يحدد الطلب نوع الاتصال.</summary>
    public Transport DefaultTransport { get; set; } = Transport.Tcp;

    /// <summary>
    /// EN: Return the terminal's full raw response. Off by default: it can contain unmasked card data.
    /// AR: إرجاع الاستجابة الخام الكاملة من الجهاز. معطّل افتراضياً لأنها قد تحتوي بيانات بطاقة غير مُقنّعة.
    /// </summary>
    public bool IncludeRawResponse { get; set; }

    public ComOptions Com { get; set; } = new();
    public TcpOptions Tcp { get; set; } = new();
}

/// <summary>
/// EN: Serial settings passed to every COM function as (port, Rate, x, y, z). Defaults = 38400 8N1 like the old sample.
/// AR: إعدادات المنفذ التسلسلي التي تُمرَّر لكل دالة COM بالترتيب (port, Rate, x, y, z). الافتراضي 38400 8N1 مثل المثال القديم.
/// </summary>
public sealed class ComOptions
{
    /// <summary>EN: COM port number (5 = COM5).  AR: رقم المنفذ (5 = COM5).</summary>
    public int Port { get; set; } = 5;
    public int BaudRate { get; set; } = 38400;
    /// <summary>EN: 0 = no parity (Win32 NOPARITY).  AR: 0 = بدون Parity.</summary>
    public int Parity { get; set; }
    public int DataBits { get; set; } = 8;
    /// <summary>EN: 0 = one stop bit (Win32 ONESTOPBIT).  AR: 0 = بت توقف واحد.</summary>
    public int StopBits { get; set; }
}

/// <summary>EN: Terminal network address.  AR: عنوان الجهاز على الشبكة.</summary>
public sealed class TcpOptions
{
    public string Ip { get; set; } = "127.0.0.1";
    public string Port { get; set; } = "6100";
}
