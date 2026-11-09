using System.Text;

namespace Mada.Api.Native;

/// <summary>
/// EN: A request string ready for the DLL: ASCII bytes + a trailing '\0', and its length WITHOUT the '\0'.
/// AR: نص طلب جاهز للمكتبة: بايتات ASCII مع '\0' في النهاية، وطوله بدون '\0'.
/// </summary>
internal sealed class NativeRequest
{
    public byte[] Buffer { get; }

    /// <summary>EN: Length as the 1-byte "intval" array most functions expect.  AR: الطول كمصفوفة "intval" ببايت واحد كما تتوقع أغلب الدوال.</summary>
    public byte[] LengthByte { get; }

    public int Length { get; }

    public NativeRequest(string request)
    {
        Length = Encoding.ASCII.GetByteCount(request);
        // EN: intval is a single byte, so a request longer than 255 bytes cannot be described to the DLL.
        // AR: intval بايت واحد فقط، لذلك لا يمكن وصف طلب أطول من 255 بايت للمكتبة.
        if (Length > byte.MaxValue)
            throw new ArgumentException($"Request is {Length} bytes; the mada DLL accepts at most 255.");

        Buffer = NativeText.ToCString(request);
        LengthByte = [(byte)Length];
    }
}

internal static class NativeText
{
    /// <summary>
    /// EN: ASCII bytes plus a '\0' terminator. The old sample passed IP/port WITHOUT a terminator, which only
    ///     worked if the memory after the array happened to be zero. The terminator is outside the counted
    ///     length, so functions that take an explicit length see exactly the same bytes as before.
    /// AR: بايتات ASCII مع حرف النهاية '\0'. المثال القديم كان يمرر IP والمنفذ بدون حرف نهاية، وكان يعمل فقط
    ///     إذا صادف أن الذاكرة بعد المصفوفة أصفار. حرف النهاية خارج الطول المحسوب، لذلك الدوال التي تأخذ
    ///     طولاً صريحاً ترى نفس البايتات تماماً كما في السابق.
    /// </summary>
    public static byte[] ToCString(string value) => Encoding.ASCII.GetBytes(value + '\0');

    /// <summary>
    /// EN: Reads a text field the DLL wrote into a byte[] buffer: stops at the first '\0' and trims spaces.
    ///     (The old sample printed the whole buffer, trailing '\0' characters included.)
    /// AR: يقرأ حقلاً نصياً كتبته المكتبة في مخزن byte[]: يتوقف عند أول '\0' ويحذف المسافات.
    ///     (المثال القديم كان يطبع المخزن كاملاً بما فيه أحرف '\0' في النهاية.)
    /// </summary>
    public static string Read(byte[] buffer)
    {
        int end = Array.IndexOf(buffer, (byte)0);
        return Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end).Trim();
    }
}
