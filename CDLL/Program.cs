using System;
using System.Configuration;
using System.Threading;
using System.Windows.Forms;
using CDLL.Payments;

namespace CDLL
{
    /// <summary>
    /// EN: Application startup class.
    /// AR: كلاس بدء تشغيل التطبيق.
    /// </summary>
    static class Program
    {
        private static PaymentServices _services;

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

            // EN: app.config UseSimulator=true -> SimulatorForm (fake terminal, no device, no DLL, nothing saved).
            // AR: إذا كان UseSimulator=true في app.config -> SimulatorForm (جهاز وهمي بدون جهاز ولا مكتبة ولا حفظ).
            bool useSimulator = string.Equals(ConfigurationManager.AppSettings["UseSimulator"], "true", StringComparison.OrdinalIgnoreCase);
            if (useSimulator)
            {
                Application.Run(new SimulatorForm());
                return;
            }

            // EN: Form1: build the payment services (terminal + storage) once, from app.config.
            //     A wrong setting (e.g. Storage.Provider=SqlServer without a connection string) stops here with a clear message.
            // AR: Form1: إنشاء خدمات الدفع (الجهاز + التخزين) مرة واحدة من app.config.
            //     أي إعداد خاطئ يتوقف هنا برسالة واضحة.
            try
            {
                _services = PaymentServices.FromConfig();
            }
            catch (ConfigurationErrorsException ex)
            {
                MessageBox.Show("Configuration error / خطأ في الإعدادات:\r\n" + ex.Message, "CDLL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // EN: Any exception nobody caught is saved to ErrorLog (or pending JSON) before it is shown.
            // AR: أي استثناء لم يُعالَج يُحفظ في ErrorLog (أو JSON معلّق) قبل عرضه.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            using (_services)
            {
                Application.Run(new Form1(_services));
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            SaveError("Unhandled UI exception", e.Exception);
            MessageBox.Show("Unexpected error, it was saved to the error log / خطأ غير متوقع، تم حفظه في سجل الأخطاء:\r\n" +
                            CardDataMasker.MaskText(e.Exception.Message), "CDLL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
                SaveError("Unhandled exception", ex);
        }

        /// <summary>EN: Waits up to 5 s for the storage thread to save the error.  AR: ينتظر حتى 5 ثوانٍ لحفظ الخطأ.</summary>
        private static void SaveError(string source, Exception ex)
        {
            try
            {
                if (_services != null)
                    _services.Storage.SaveErrorAsync(ErrorLogRecord.FromException(source, ex)).Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception)
            {
                // EN: never throw from the last-chance handler   AR: لا يرمي استثناءً من معالج الملاذ الأخير
            }
        }
    }
}
