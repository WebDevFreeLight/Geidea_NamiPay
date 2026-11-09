using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;

namespace CDLL
{
    /// <summary>
    /// EN: Application startup class.
    /// AR: كلاس بدء تشغيل التطبيق.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// AR: نقطة الدخول الرئيسية للتطبيق (أول دالة تُنفَّذ عند تشغيل CDLL.exe).
        /// </summary>
        // EN: [STAThread] is required by Windows Forms (single-threaded apartment for UI/COM).
        // AR: الخاصية [STAThread] مطلوبة في Windows Forms (نموذج الخيط الواحد للواجهة و COM).
        [STAThread]
        static void Main()
        {
            // EN: Use the modern Windows look for buttons and controls.
            // AR: استخدام المظهر الحديث لنظام Windows للأزرار وعناصر الواجهة.
            Application.EnableVisualStyles();
            // EN: Use GDI+ default text rendering (must be called before any window is created).
            // AR: استخدام طريقة رسم النصوص الافتراضية (يجب استدعاؤها قبل إنشاء أي نافذة).
            Application.SetCompatibleTextRenderingDefault(false);
            // EN: Open the main window and run the message loop until it is closed.
            //     app.config UseSimulator=true -> SimulatorForm (fake terminal, no device, no DLL).
            //     Anything else (default)      -> Form1 (real terminal, unchanged).
            // AR: فتح النافذة الرئيسية وتشغيل حلقة الرسائل حتى يتم إغلاقها.
            //     إذا كان UseSimulator=true في app.config -> SimulatorForm (جهاز وهمي بدون جهاز ولا مكتبة).
            //     غير ذلك (الافتراضي)                    -> Form1 (الجهاز الفعلي، بدون تغيير).
            bool useSimulator = string.Equals(ConfigurationManager.AppSettings["UseSimulator"], "true", StringComparison.OrdinalIgnoreCase);
            Application.Run(useSimulator ? (Form)new SimulatorForm() : new Form1());
        }
    }
}
