// =====================================================================================
// EN: COM ports for the "COM port" drop-down (Form1 and the mapping screen) and the quick port check
//     that runs before every USB/COM call, so a wrong / missing / busy port fails in seconds instead of
//     leaving the DLL waiting.
//       - SerialPortScanner.ListAsync(): every COM port with its Device Manager name, e.g.
//             "COM8 - USB Serial Device [terminal]", "COM3 - Standard Serial over Bluetooth link"
//         The PAX terminal (USB vendor ID 2FB8, e.g. A920Pro) is marked [terminal].
//       - SerialPortProbe.CheckAsync(): opens and closes the port within a time limit (nothing is sent).
//     Everything runs on background threads: Windows (WMI) and some drivers (Bluetooth) can take seconds.
// AR: منافذ COM لقائمة "منفذ COM" المنسدلة (Form1 وشاشة الربط) والفحص السريع للمنفذ قبل كل استدعاء USB/COM،
//     حتى يفشل المنفذ الخاطئ أو غير الموجود أو المشغول خلال ثوانٍ بدلاً من انتظار المكتبة.
//       - SerialPortScanner.ListAsync(): كل منافذ COM مع اسمها في "إدارة الأجهزة"، وجهاز PAX يُعلَّم [terminal].
//       - SerialPortProbe.CheckAsync(): يفتح المنفذ ويغلقه خلال مهلة محددة (بدون إرسال أي شيء).
//     كل ذلك على خيوط خلفية لأن Windows (WMI) وبعض التعريفات (Bluetooth) قد تستغرق ثوانٍ.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CDLL.Payments.Terminal
{
    /// <summary>EN: One COM port as shown in the drop-down.  AR: منفذ COM واحد كما يظهر في القائمة.</summary>
    public sealed class SerialPortInfo
    {
        public int Number { get; set; }
        public string Description { get; set; }
        public bool IsTerminal { get; set; }
        public bool IsBluetooth { get; set; }
        public bool IsPresent { get; set; } = true;

        public string Name { get { return "COM" + Number.ToString(CultureInfo.InvariantCulture); } }

        public override string ToString()
        {
            if (!IsPresent)
                return Name + " - not connected / غير متصل";
            string text = string.IsNullOrEmpty(Description) ? Name : Name + " - " + Description;
            return IsTerminal ? text + "  [terminal / الجهاز]" : text;
        }
    }

    public static class SerialPortScanner
    {
        /// <summary>EN: USB vendor ID of PAX (A920Pro and other PAX terminals).  AR: معرّف المورّد لأجهزة PAX.</summary>
        public const string PaxUsbVendorId = "VID_2FB8";

        private static readonly Regex ComInName = new Regex(@"\(COM(\d+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // EN: Windows wraps device names in invisible left-to-right marks (U+202A ... U+202C) on Arabic systems.
        // AR: Windows يحيط أسماء الأجهزة بعلامات اتجاه غير مرئية على الأنظمة العربية.
        private static readonly Regex BidiMarks = new Regex("[‎‏‪-‮⁦-⁩]", RegexOptions.Compiled);

        /// <summary>EN: All COM ports, terminal ports first. Never throws.  AR: كل منافذ COM، منافذ الجهاز أولاً. لا يرمي استثناءً.</summary>
        public static Task<IList<SerialPortInfo>> ListAsync()
        {
            return Task.Run(() => List());
        }

        private static IList<SerialPortInfo> List()
        {
            var ports = new Dictionary<int, SerialPortInfo>();

            // EN: 1) Names Windows can open.   AR: 1) الأسماء التي يمكن لـ Windows فتحها.
            foreach (string name in SafePortNames())
            {
                int number;
                if (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(name.Substring(3), NumberStyles.None, CultureInfo.InvariantCulture, out number))
                    ports[number] = new SerialPortInfo { Number = number };
            }

            // EN: 2) Device Manager names (Ports (COM & LPT)) through WMI.   AR: 2) أسماء "إدارة الأجهزة" عبر WMI.
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
                using (ManagementObjectCollection devices = searcher.Get())
                {
                    foreach (ManagementBaseObject device in devices)
                    {
                        string name = device["Name"] as string ?? "";
                        string deviceId = device["PNPDeviceID"] as string ?? "";
                        Match match = ComInName.Match(name);
                        int number;
                        if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out number))
                            continue;

                        SerialPortInfo info;
                        if (!ports.TryGetValue(number, out info))
                            ports[number] = info = new SerialPortInfo { Number = number };
                        info.Description = BidiMarks.Replace(ComInName.Replace(name, ""), "").Trim();
                        info.IsTerminal = deviceId.IndexOf(PaxUsbVendorId, StringComparison.OrdinalIgnoreCase) >= 0;
                        info.IsBluetooth = deviceId.StartsWith("BTHENUM", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex) when (ex is ManagementException || ex is UnauthorizedAccessException || ex is System.Runtime.InteropServices.COMException)
            {
                // EN: WMI unavailable: the plain COM names are still listed.   AR: WMI غير متاح: تبقى أسماء COM فقط.
            }

            return ports.Values.OrderByDescending(p => p.IsTerminal).ThenBy(p => p.Number).ToList();
        }

        private static string[] SafePortNames()
        {
            try { return SerialPort.GetPortNames(); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is IOException || ex is UnauthorizedAccessException) { return new string[0]; }
        }
    }

    /// <summary>
    /// EN: Quick check before a USB/COM call. Returns null when the port can be opened, otherwise a message.
    /// AR: فحص سريع قبل استدعاء USB/COM. يُعيد null إذا أمكن فتح المنفذ، وإلا رسالة توضح المشكلة.
    /// </summary>
    public interface IComPortProbe
    {
        Task<string> CheckAsync(int comPort, TimeSpan timeout);
    }

    public sealed class SerialPortProbe : IComPortProbe
    {
        public async Task<string> CheckAsync(int comPort, TimeSpan timeout)
        {
            string name = "COM" + comPort.ToString(CultureInfo.InvariantCulture);
            Task<string> open = Task.Run(() => TryOpen(name));
            if (await Task.WhenAny(open, Task.Delay(timeout)).ConfigureAwait(false) != open)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} did not open within {1} s (Bluetooth port or stuck driver?). Choose the terminal's USB port. / " +
                    "لم يُفتح {0} خلال {1} ثانية (منفذ Bluetooth أو تعريف متوقف؟). اختر منفذ USB الخاص بالجهاز.",
                    name, (int)timeout.TotalSeconds);
            }
            return await open.ConfigureAwait(false);
        }

        private static string TryOpen(string name)
        {
            string[] existing;
            try { existing = SerialPort.GetPortNames(); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is IOException || ex is UnauthorizedAccessException) { existing = new string[0]; }

            if (!existing.Contains(name, StringComparer.OrdinalIgnoreCase))
                return name + " does not exist on this PC (terminal unplugged or wrong port). / " + name + " غير موجود على هذا الحاسوب (الجهاز غير متصل أو المنفذ خاطئ).";

            try
            {
                using (var port = new SerialPort(name))
                {
                    port.Open();
                    port.Close();
                }
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return name + " is in use by another program (another CDLL window?). / " + name + " مستخدم من برنامج آخر (نافذة CDLL أخرى؟).";
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is ArgumentException)
            {
                return name + " could not be opened / تعذر فتح " + name + ": " + ex.Message;
            }
        }
    }
}
