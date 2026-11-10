// =====================================================================================
// EN: What the NamiPay terminal needs remembered between calls and between app restarts, per cash register:
//       - TerminalId           returned by Register (part of every signature),
//       - NextEcrRef           6-digit counter (000001-999999) of the next ECR reference,
//       - LastTransactionEcrRef full ECR reference of the last purchase / refund (used by Repeat last).
//     Saved as JSON in <DataFolder>\namipay\state.json (atomic writes). Thread-safe.
// AR: ما يجب أن يتذكره جهاز NamiPay بين الاستدعاءات وبين مرات التشغيل لكل صندوق: رقم الجهاز من التسجيل،
//     عدّاد مرجع ECR التالي، ومرجع آخر شراء / استرجاع (لإعادة آخر عملية). يُحفظ كـ JSON بشكل ذري.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using CDLL.Payments.Storage;

namespace CDLL.Payments.Terminal.Skyband
{
    public sealed class SkybandState
    {
        public string TerminalId { get; set; }
        public int NextEcrRef { get; set; } = 1;
        public string LastTransactionEcrRef { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public sealed class SkybandStateStore
    {
        private readonly string _file;
        private readonly object _lock = new object();
        private Dictionary<string, SkybandState> _states;

        public SkybandStateStore(string folder)
        {
            _file = Path.Combine(folder, "namipay", "state.json");
        }

        /// <summary>EN: A copy of the state of one cash register.  AR: نسخة من حالة صندوق واحد.</summary>
        public SkybandState Get(string cashRegister)
        {
            lock (_lock)
            {
                SkybandState state;
                Load().TryGetValue(cashRegister ?? "", out state);
                state = state ?? new SkybandState();
                return new SkybandState
                {
                    TerminalId = state.TerminalId, NextEcrRef = state.NextEcrRef,
                    LastTransactionEcrRef = state.LastTransactionEcrRef, UpdatedAtUtc = state.UpdatedAtUtc
                };
            }
        }

        public void Save(string cashRegister, SkybandState state)
        {
            lock (_lock)
            {
                state.UpdatedAtUtc = DateTime.UtcNow;
                Load()[cashRegister ?? ""] = state;
                JsonFiles.WriteAtomic(_file, _states);
            }
        }

        /// <summary>EN: Takes the next 6-digit counter and moves it on (999999 -> 000001).  AR: يأخذ العدّاد التالي ويزيده.</summary>
        public string TakeEcrRef(string cashRegister)
        {
            lock (_lock)
            {
                SkybandState state = Get(cashRegister);
                int current = state.NextEcrRef < 1 || state.NextEcrRef > 999999 ? 1 : state.NextEcrRef;
                state.NextEcrRef = current >= 999999 ? 1 : current + 1;
                Save(cashRegister, state);
                return current.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private Dictionary<string, SkybandState> Load()
        {
            if (_states != null)
                return _states;
            try
            {
                _states = File.Exists(_file)
                    ? JsonFiles.Read<Dictionary<string, SkybandState>>(_file) ?? new Dictionary<string, SkybandState>()
                    : new Dictionary<string, SkybandState>();
            }
            catch (Exception ex) when (ex is IOException || ex is Newtonsoft.Json.JsonException)
            {
                _states = new Dictionary<string, SkybandState>();   // EN: damaged file: start again (terminal re-registers)   AR: ملف تالف: البدء من جديد
            }
            return _states;
        }
    }
}
