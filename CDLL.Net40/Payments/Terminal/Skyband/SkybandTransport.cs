// =====================================================================================
// EN: The two moving parts of the NamiPay / SkyBand protocol, behind interfaces so tests can replace them:
//       ISkybandCodec  - ECR.dll pack() / parse() (32-bit, cdecl), copied next to CDLL.exe from NamiPay\CoreSkybandECR.
//       ISkybandLink   - sends the packed frame and reads the answer over USB/COM (SerialPort) or TCP/IP.
//     The link reads at least 5 bytes (each read waits up to the type's timeout), then waits 1 s and takes the
//     rest, exactly like the NamiPay app. Failures become SkybandLinkException with a return code:
//       -3 = port / socket could not be opened (NoResponse), -4 = no answer in time (Timeout).
// AR: الجزءان المتحركان في بروتوكول NamiPay / SkyBand خلف واجهات حتى تستبدلهما الاختبارات:
//       ISkybandCodec = دالتا pack و parse من ECR.dll، و ISkybandLink = الإرسال والاستقبال عبر USB/COM أو TCP/IP.
//     الفشل يصبح SkybandLinkException برمز: ‎-3 تعذر فتح المنفذ، ‎-4 لا رد في الوقت المحدد.
// =====================================================================================

using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace CDLL.Payments.Terminal.Skyband
{
    public interface ISkybandCodec
    {
        byte[] Pack(string request, int transactionType, string signature);
        byte[] Parse(byte[] response);
    }

    public interface ISkybandLink
    {
        /// <summary>EN: Sends the frame and returns the raw answer. Throws SkybandLinkException.  AR: يرسل الإطار ويُعيد الرد الخام.</summary>
        byte[] Exchange(TerminalConnection connection, byte[] frame, int readTimeoutMs);
    }

    public sealed class SkybandLinkException : Exception
    {
        public const int OpenFailed = -3;
        public const int NoAnswer = -4;

        public SkybandLinkException(int returnCode, string message) : base(message)
        {
            ReturnCode = returnCode;
        }

        public int ReturnCode { get; private set; }
    }

    /// <summary>EN: The vendor's ECR.dll (from the NamiPay project).  AR: مكتبة ECR.dll من مشروع NamiPay.</summary>
    public sealed class NativeSkybandCodec : ISkybandCodec
    {
        [DllImport("ECR.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pack([MarshalAs(UnmanagedType.LPStr)] string inputReqData, int transactionType,
                                        [MarshalAs(UnmanagedType.LPStr)] string szSignature, byte[] szEcrBuffer);

        [DllImport("ECR.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void parse(byte[] respData, byte[] szRespFields);

        public byte[] Pack(string request, int transactionType, string signature)
        {
            byte[] buffer = new byte[1024];   // EN: the NamiPay app used 150; larger is safer   AR: أكبر من 150 للأمان
            pack(request, transactionType, signature, buffer);
            return TrimTrailingZeros(buffer);
        }

        public byte[] Parse(byte[] response)
        {
            byte[] input = new byte[response.Length + 1];   // EN: '\0' at the end for the C code   AR: '\0' في النهاية لكود C
            Array.Copy(response, input, response.Length);
            byte[] output = new byte[50000];
            parse(input, output);
            return TrimTrailingZeros(output);
        }

        internal static byte[] TrimTrailingZeros(byte[] buffer)
        {
            int length = buffer.Length;
            while (length > 0 && buffer[length - 1] == 0)
                length--;
            byte[] trimmed = new byte[length];
            Array.Copy(buffer, trimmed, length);
            return trimmed;
        }
    }

    /// <summary>EN: USB/COM through SerialPort, TCP/IP through a socket. One connection per call.  AR: اتصال واحد لكل استدعاء.</summary>
    public sealed class SkybandLink : ISkybandLink
    {
        private const int MinimumAnswerBytes = 5;
        private const int ConnectTimeoutMs = 5000;

        public byte[] Exchange(TerminalConnection connection, byte[] frame, int readTimeoutMs)
        {
            return connection.Transport == TerminalTransport.Com
                ? OverSerial(connection, frame, readTimeoutMs)
                : OverTcp(connection, frame, readTimeoutMs);
        }

        private static byte[] OverSerial(TerminalConnection c, byte[] frame, int readTimeoutMs)
        {
            string name = "COM" + c.ComPort.ToString(CultureInfo.InvariantCulture);
            using (var port = new SerialPort(name, c.BaudRate, (Parity)c.Parity, c.DataBits, ToStopBits(c.StopBits)))
            {
                port.ReadTimeout = readTimeoutMs;
                port.WriteTimeout = 5000;
                try
                {
                    port.Open();
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException)
                {
                    throw new SkybandLinkException(SkybandLinkException.OpenFailed, name + " could not be opened / تعذر فتح " + name + ": " + ex.Message);
                }

                port.DiscardOutBuffer();
                port.DiscardInBuffer();
                port.Write(frame, 0, frame.Length);

                var buffer = new byte[50000];
                int count = 0;
                try
                {
                    while (count < MinimumAnswerBytes)
                        count += port.Read(buffer, count, buffer.Length - count);
                    Thread.Sleep(1000);   // EN: let the rest of the answer arrive (as the NamiPay app)   AR: انتظار بقية الرد
                    while (port.BytesToRead > 0 && count < buffer.Length)
                        count += port.Read(buffer, count, buffer.Length - count);
                }
                catch (TimeoutException)
                {
                    throw NoAnswer(readTimeoutMs);
                }

                byte[] answer = new byte[count];
                Array.Copy(buffer, answer, count);
                return answer;
            }
        }

        private static byte[] OverTcp(TerminalConnection c, byte[] frame, int readTimeoutMs)
        {
            int tcpPort = int.Parse(c.TcpPort.Trim(), CultureInfo.InvariantCulture);
            using (var client = new TcpClient())
            {
                try
                {
                    IAsyncResult connect = client.BeginConnect(c.TcpIp.Trim(), tcpPort, null, null);
                    if (!connect.AsyncWaitHandle.WaitOne(ConnectTimeoutMs) || !client.Connected)
                        throw new SkybandLinkException(SkybandLinkException.OpenFailed,
                            "No connection to " + c.TcpIp + ":" + c.TcpPort + " within 5 s / لا اتصال خلال 5 ثوانٍ");
                    client.EndConnect(connect);
                }
                catch (SocketException ex)
                {
                    throw new SkybandLinkException(SkybandLinkException.OpenFailed, c.TcpIp + ":" + c.TcpPort + ": " + ex.Message);
                }

                NetworkStream stream = client.GetStream();
                stream.ReadTimeout = readTimeoutMs;
                stream.Write(frame, 0, frame.Length);

                var buffer = new byte[50000];
                int count = 0;
                try
                {
                    while (count < MinimumAnswerBytes)
                    {
                        int read = stream.Read(buffer, count, buffer.Length - count);
                        if (read == 0)
                            throw new SkybandLinkException(SkybandLinkException.OpenFailed, "The terminal closed the connection / أغلق الجهاز الاتصال");
                        count += read;
                    }
                    Thread.Sleep(300);
                    while (stream.DataAvailable && count < buffer.Length)
                        count += stream.Read(buffer, count, buffer.Length - count);
                }
                catch (IOException)
                {
                    throw NoAnswer(readTimeoutMs);
                }

                byte[] answer = new byte[count];
                Array.Copy(buffer, answer, count);
                return answer;
            }
        }

        private static SkybandLinkException NoAnswer(int readTimeoutMs)
        {
            string seconds = (readTimeoutMs / 1000).ToString(CultureInfo.InvariantCulture);
            return new SkybandLinkException(SkybandLinkException.NoAnswer,
                "No answer from the NamiPay terminal within " + seconds + " s / لا رد من جهاز NamiPay خلال " + seconds + " ثانية");
        }

        /// <summary>EN: Our stop bits are Win32 numbers (0 = one, 1 = 1.5, 2 = two).  AR: بتات التوقف بأرقام Win32.</summary>
        private static StopBits ToStopBits(int win32)
        {
            switch (win32)
            {
                case 1: return StopBits.OnePointFive;
                case 2: return StopBits.Two;
                default: return StopBits.One;
            }
        }
    }
}
