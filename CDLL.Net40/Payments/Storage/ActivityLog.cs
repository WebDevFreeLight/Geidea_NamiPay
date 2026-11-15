// =====================================================================================
// EN: Plain-text activity log, one file per day: <DataFolder>\logs\activity-yyyy-MM-dd.log.
//     One line per saved transaction / error / mapping, plus every database failure and pending import,
//     so support staff can follow what happened even when the database itself was down.
//     Written ONLY from PaymentStorage's background thread. Card numbers are masked; logging never throws.
// AR: سجل نشاط نصي، ملف لكل يوم: <DataFolder>\logs\activity-yyyy-MM-dd.log.
//     سطر لكل عملية أو خطأ أو ربط محفوظ، مع كل فشل في قاعدة البيانات وكل استيراد للمعلّق،
//     حتى يتمكن الدعم الفني من متابعة ما حدث حتى عندما كانت القاعدة نفسها متعطلة.
//     يُكتب فقط من الخيط الخلفي لـ PaymentStorage. أرقام البطاقات مُقنّعة، والتسجيل لا يرمي استثناءً أبداً.
// =====================================================================================

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CDLL.Payments.Storage
{
    public sealed class ActivityLog
    {
        private readonly string _folder;

        public ActivityLog(string folder)
        {
            _folder = folder;
        }

        public string Folder { get { return _folder; } }

        public void Write(string message)
        {
            try
            {
                Directory.CreateDirectory(_folder);
                DateTime now = DateTime.Now;
                string path = Path.Combine(_folder, "activity-" + now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
                string line = now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " | " +
                              CardDataMasker.MaskText(message) + Environment.NewLine;
                File.AppendAllText(path, line, new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // EN: the log must never break a payment   AR: السجل يجب ألا يعطّل عملية الدفع أبداً
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
