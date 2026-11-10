// =====================================================================================
// EN: Shows "… N s" while the terminal works, so the window never looks frozen. Uses a WinForms timer
//     (UI thread, every 0.5 s); the terminal call itself runs on a background thread. Dispose() stops it.
//     Used by Form1 (title bar) and the mapping screen (status line).
// AR: يعرض "… N ث" أثناء عمل الجهاز حتى لا تبدو النافذة متجمّدة. يستخدم مؤقت WinForms (خيط الواجهة، كل 0.5 ثانية)،
//     واستدعاء الجهاز نفسه يعمل على خيط خلفي. Dispose() يوقفه. تستخدمه Form1 (شريط العنوان) وشاشة الربط (سطر الحالة).
// =====================================================================================

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace CDLL.Payments
{
    public sealed class WaitingCounter : IDisposable
    {
        private readonly Timer _timer = new Timer { Interval = 500 };
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Action _done;

        /// <param name="show">EN: called with the elapsed whole seconds.  AR: تُستدعى بعدد الثواني المنقضية.</param>
        /// <param name="done">EN: called once when the wait is over.  AR: تُستدعى مرة واحدة عند انتهاء الانتظار.</param>
        public WaitingCounter(Action<int> show, Action done)
        {
            _done = done;
            show(0);
            _timer.Tick += (sender, e) => show((int)_stopwatch.Elapsed.TotalSeconds);
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
            _done();
        }
    }
}
