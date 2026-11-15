// =====================================================================================
// EN: Fills a "COM port" ComboBox with SerialPortScanner's list (built on a background thread) and reads the
//     chosen port back. Shared by Form1 and the terminal mapping screen.
//     Selection order: the wanted port (mapping / app.config) -> the first [terminal] port -> the first port.
//     A wanted port that is not plugged in is still listed as "COMx - not connected" so it is never lost silently.
// AR: يملأ قائمة "منفذ COM" بقائمة SerialPortScanner (تُبنى على خيط خلفي) ويقرأ المنفذ المختار.
//     مشتركة بين Form1 وشاشة ربط الأجهزة. ترتيب الاختيار: المنفذ المطلوب -> أول منفذ [terminal] -> أول منفذ.
//     المنفذ المطلوب غير الموصول يظهر "غير متصل" حتى لا يضيع بصمت.
// =====================================================================================

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CDLL.Payments.Terminal;

namespace CDLL.Payments
{
    public static class ComPortPicker
    {
        /// <summary>EN: Reloads the list without freezing the window.  AR: يعيد تحميل القائمة بدون تجميد النافذة.</summary>
        public static async Task RefreshAsync(ComboBox combo, int wantedPort)
        {
            combo.Enabled = false;
            try
            {
                IList<SerialPortInfo> ports = await SerialPortScanner.ListAsync();
                Fill(combo, ports, wantedPort);
            }
            finally
            {
                combo.Enabled = true;
            }
        }

        public static void Fill(ComboBox combo, IList<SerialPortInfo> ports, int wantedPort)
        {
            combo.BeginUpdate();
            combo.Items.Clear();
            foreach (SerialPortInfo port in ports)
                combo.Items.Add(port);

            if (wantedPort > 0 && !ports.Any(p => p.Number == wantedPort))
                combo.Items.Add(new SerialPortInfo { Number = wantedPort, IsPresent = false });

            SerialPortInfo selected =
                combo.Items.Cast<SerialPortInfo>().FirstOrDefault(p => p.Number == wantedPort) ??
                combo.Items.Cast<SerialPortInfo>().FirstOrDefault(p => p.IsTerminal) ??
                combo.Items.Cast<SerialPortInfo>().FirstOrDefault();
            combo.SelectedItem = selected;
            combo.EndUpdate();
        }

        /// <summary>EN: Chooses a port that may not be in the list yet (e.g. from a mapping).  AR: يختار منفذاً قد لا يكون في القائمة بعد.</summary>
        public static void Select(ComboBox combo, int port)
        {
            Fill(combo, combo.Items.Cast<SerialPortInfo>().Where(p => p.IsPresent).ToList(), port);
        }

        /// <summary>EN: Chosen COM number, or 0 when nothing is chosen.  AR: رقم المنفذ المختار، أو 0 إذا لم يُختر شيء.</summary>
        public static int SelectedPort(ComboBox combo)
        {
            var port = combo.SelectedItem as SerialPortInfo;
            return port == null ? 0 : port.Number;
        }
    }
}
