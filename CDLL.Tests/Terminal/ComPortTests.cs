// =====================================================================================
// EN: The COM port drop-down and the "never hang" rules: the port check before a USB call, the short
//     watchdog for status checks, and how the drop-down picks a port.
//     No real port is opened except COM250, which does not exist on any normal PC.
// AR: قائمة منافذ COM وقواعد "عدم التوقف": فحص المنفذ قبل استدعاء USB، والمهلة القصيرة لفحص الحالة، وطريقة اختيار المنفذ.
//     لا يُفتح أي منفذ فعلي باستثناء COM250 غير الموجود على أي حاسوب عادي.
// =====================================================================================

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CDLL.Payments;
using CDLL.Payments.Terminal;
using CDLL.Tests.Fakes;
using Xunit;

namespace CDLL.Tests.Terminal
{
    public class ComPortTests
    {
        private static readonly TerminalConnection Com8 = new TerminalConnection { Transport = TerminalTransport.Com, ComPort = 8, BaudRate = 38400, DataBits = 8 };
        private static readonly TerminalConnection Tcp = new TerminalConnection { Transport = TerminalTransport.Tcp, TcpIp = "192.168.1.25", TcpPort = "6100" };
        private static readonly OutcomeClassifier Classifier = new OutcomeClassifier(new[] { "000" }, null, null);

        private readonly FakeMadaApi _fake = new FakeMadaApi();

        [Fact]
        public async Task Bad_COM_port_fails_fast_without_calling_the_DLL_and_frees_the_terminal()
        {
            var probe = new FakeProbe { Problem = "COM8 is in use by another program" };
            var service = new MadaTerminalService(_fake, Classifier, TimeSpan.FromSeconds(10), null, probe);

            TerminalResult result = await service.CheckStatusAsync(Com8);

            Assert.Equal(TransactionOutcome.NoResponse, result.Outcome);
            Assert.Equal("COM8 is in use by another program", result.ErrorMessage);
            Assert.Null(result.ReturnCode);
            Assert.Equal(0, _fake.Calls);
            Assert.Equal(8, probe.LastPort);

            probe.Problem = null;   // EN: port fixed: the next call must not be "busy"   AR: بعد إصلاح المنفذ يجب ألا يكون الاستدعاء التالي "مشغولاً"
            Assert.Equal(TransactionOutcome.Ok, (await service.CheckStatusAsync(Com8)).Outcome);
            Assert.Equal(1, _fake.Calls);
        }

        [Fact]
        public async Task Port_check_is_only_for_USB()
        {
            var probe = new FakeProbe { Problem = "should not be asked" };
            var service = new MadaTerminalService(_fake, Classifier, TimeSpan.FromSeconds(10), null, probe);

            Assert.Equal(TransactionOutcome.Ok, (await service.CheckStatusAsync(Tcp)).Outcome);
            Assert.Equal(0, probe.Calls);
        }

        [Fact]
        public async Task Status_check_uses_the_short_watchdog_payments_keep_the_long_one()
        {
            _fake.Gate = new ManualResetEventSlim(false);
            var service = new MadaTerminalService(_fake, Classifier, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(300));

            Stopwatch watch = Stopwatch.StartNew();
            TerminalResult status = await service.CheckStatusAsync(Com8);
            watch.Stop();

            Assert.Equal(TransactionOutcome.Timeout, status.Outcome);
            Assert.InRange(watch.ElapsedMilliseconds, 250, 5000);
            _fake.Gate.Set();
        }

        [Fact]
        public async Task Missing_port_is_reported_in_seconds()
        {
            Stopwatch watch = Stopwatch.StartNew();
            string problem = await new SerialPortProbe().CheckAsync(250, TimeSpan.FromSeconds(3));

            Assert.Contains("COM250 does not exist", problem);
            Assert.True(watch.ElapsedMilliseconds < 3500);
        }

        [Fact]
        public async Task Scanner_never_throws_and_lists_terminal_ports_first()
        {
            var ports = await SerialPortScanner.ListAsync();
            Assert.NotNull(ports);
            Assert.Equal(ports.OrderByDescending(p => p.IsTerminal).ThenBy(p => p.Number).Select(p => p.Number), ports.Select(p => p.Number));
        }

        [Fact]
        public void Drop_down_picks_wanted_port_then_terminal_port_then_first()
        {
            var ports = new[]
            {
                new SerialPortInfo { Number = 8, Description = "USB Serial Device", IsTerminal = true },
                new SerialPortInfo { Number = 3, Description = "Standard Serial over Bluetooth link", IsBluetooth = true },
                new SerialPortInfo { Number = 9, Description = "USB Serial Device", IsTerminal = true }
            };

            using (var combo = new ComboBox())
            {
                ComPortPicker.Fill(combo, ports, 9);
                Assert.Equal(9, ComPortPicker.SelectedPort(combo));

                ComPortPicker.Fill(combo, ports, 0);
                Assert.Equal(8, ComPortPicker.SelectedPort(combo));

                // EN: a mapped port that is unplugged stays visible as "not connected"   AR: المنفذ المفصول يبقى ظاهراً
                ComPortPicker.Fill(combo, ports, 5);
                Assert.Equal(5, ComPortPicker.SelectedPort(combo));
                Assert.Contains("not connected", combo.SelectedItem.ToString());

                ComPortPicker.Select(combo, 3);
                Assert.Equal(3, ComPortPicker.SelectedPort(combo));
                Assert.DoesNotContain(combo.Items.Cast<SerialPortInfo>(), p => p.Number == 5);
            }

            Assert.Equal("COM8 - USB Serial Device  [terminal / الجهاز]", ports[0].ToString());
            Assert.Equal("COM3 - Standard Serial over Bluetooth link", ports[1].ToString());
        }

        private sealed class FakeProbe : IComPortProbe
        {
            public string Problem;
            public int LastPort;
            public int Calls;

            public Task<string> CheckAsync(int comPort, TimeSpan timeout)
            {
                Calls++;
                LastPort = comPort;
                return Task.FromResult(Problem);
            }
        }
    }
}
