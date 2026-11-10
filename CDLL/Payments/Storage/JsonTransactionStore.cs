// =====================================================================================
// EN: JSON back-end: one readable .json file per record, no database needed.
//       <folder>\transactions\yyyy-MM-dd\<Id>.json
//       <folder>\errors\yyyy-MM-dd\<Id>.json
//       <folder>\mappings\<Workstation>.json
//     Files are written atomically (temp file + rename), so a crash never leaves a half-written file.
//     SECURITY: TypeNameHandling is off, so a tampered file cannot make the app create arbitrary types.
// AR: تخزين JSON: ملف .json مقروء لكل سجل، بدون قاعدة بيانات.
//     الملفات تُكتب بشكل ذري (ملف مؤقت ثم إعادة تسمية)، فلا يبقى ملف مكتوب جزئياً عند التعطل.
//     الأمان: TypeNameHandling معطّل، فلا يستطيع ملف معدّل أن يجعل البرنامج ينشئ أنواعاً عشوائية.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CDLL.Payments.Storage
{
    public sealed class JsonTransactionStore : ITransactionStore
    {
        private readonly string _folder;

        public JsonTransactionStore(string folder)
        {
            _folder = folder;
        }

        public string Name { get { return "Json"; } }

        public void EnsureCreated()
        {
            Directory.CreateDirectory(Path.Combine(_folder, "transactions"));
            Directory.CreateDirectory(Path.Combine(_folder, "errors"));
            Directory.CreateDirectory(Path.Combine(_folder, "mappings"));
        }

        public void SaveTransaction(PaymentTransactionRecord record)
        {
            string path = Path.Combine(_folder, "transactions", Day(record.CreatedAtUtc), JsonFiles.SafeName(record.Id) + ".json");
            if (!File.Exists(path))   // EN: first save wins, like the SQL stores   AR: الحفظ الأول هو المعتمد مثل قواعد SQL
                JsonFiles.WriteAtomic(path, record);
        }

        public void SaveError(ErrorLogRecord record)
        {
            string path = Path.Combine(_folder, "errors", Day(record.CreatedAtUtc), JsonFiles.SafeName(record.Id) + ".json");
            if (!File.Exists(path))
                JsonFiles.WriteAtomic(path, record);
        }

        public void SaveMapping(TerminalMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(mapping.Workstation))
                throw new ArgumentException("Workstation is required. / اسم الحاسوب مطلوب.");
            JsonFiles.WriteAtomic(MappingPath(mapping.Workstation), mapping);
        }

        public void DeleteMapping(string workstation)
        {
            string path = MappingPath(workstation);
            if (File.Exists(path))
                File.Delete(path);
        }

        public TerminalMapping GetMapping(string workstation)
        {
            string path = MappingPath(workstation);
            return File.Exists(path) ? JsonFiles.Read<TerminalMapping>(path) : null;
        }

        public IList<TerminalMapping> GetMappings()
        {
            string folder = Path.Combine(_folder, "mappings");
            if (!Directory.Exists(folder))
                return new List<TerminalMapping>();
            return Directory.GetFiles(folder, "*.json")
                .Select(JsonFiles.Read<TerminalMapping>)
                .OrderBy(m => m.Workstation, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IList<PaymentTransactionRecord> GetTransactions(DateTime fromUtc, DateTime toUtc)
        {
            var rows = new List<PaymentTransactionRecord>();
            for (DateTime day = fromUtc.ToUniversalTime().Date; day <= toUtc.ToUniversalTime().Date; day = day.AddDays(1))
            {
                string folder = Path.Combine(_folder, "transactions", Day(day));
                if (!Directory.Exists(folder))
                    continue;
                rows.AddRange(Directory.GetFiles(folder, "*.json")
                    .Select(JsonFiles.Read<PaymentTransactionRecord>)
                    .Where(r => r.CreatedAtUtc >= fromUtc.ToUniversalTime() && r.CreatedAtUtc < toUtc.ToUniversalTime()));
            }
            return rows.OrderBy(r => r.CreatedAtUtc).ToList();
        }

        private string MappingPath(string workstation)
        {
            return Path.Combine(_folder, "mappings", JsonFiles.SafeName(workstation.ToUpperInvariant()) + ".json");
        }

        private static string Day(DateTime utc)
        {
            return utc.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// EN: JSON read / write shared by JsonTransactionStore and PendingJsonQueue.
    /// AR: قراءة وكتابة JSON المشتركة بين JsonTransactionStore و PendingJsonQueue.
    /// </summary>
    internal static class JsonFiles
    {
        private static readonly Regex Unsafe = new Regex(@"[^A-Za-z0-9_\-]", RegexOptions.Compiled);

        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.None,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = { new StringEnumConverter() }
        };

        public static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

        public static void WriteAtomic(string path, object value)
        {
            string folder = Path.GetDirectoryName(path);
            Directory.CreateDirectory(folder);
            // EN: Short temp name in the same folder: .NET Framework paths must stay under 260 characters.
            // AR: اسم مؤقت قصير في نفس المجلد: المسارات في ‎.NET Framework يجب أن تبقى أقل من 260 حرفاً.
            string temp = Path.Combine(folder, "~" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp");
            try
            {
                File.WriteAllText(temp, JsonConvert.SerializeObject(value, Settings), new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);   // EN: never leave a half-done temp file   AR: لا يُترك ملف مؤقت ناقص
            }
        }

        public static T Read<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path, Encoding.UTF8), Settings);
        }

        /// <summary>EN: Keeps only letters, digits, '_' and '-' (no "..\" path tricks).  AR: يُبقي الحروف والأرقام و '_' و '-' فقط.</summary>
        public static string SafeName(string value)
        {
            string safe = Unsafe.Replace(value ?? "", "_");
            return safe.Length == 0 ? "_" : safe;
        }
    }
}
