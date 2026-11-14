// =====================================================================================
// EN: Wraps a real store and can simulate "database down" (every call throws). Records the thread of
//     every call and what was saved, in order.
// AR: يغلّف تخزيناً حقيقياً ويمكنه محاكاة "تعطل قاعدة البيانات" (كل استدعاء يرمي استثناءً)، ويسجل خيط كل استدعاء
//     وما تم حفظه بالترتيب.
// =====================================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CDLL.Payments;
using CDLL.Payments.Storage;

namespace CDLL.Tests
{
    internal sealed class SwitchableStore : ITransactionStore
    {
        private readonly ITransactionStore _inner;

        public SwitchableStore(ITransactionStore inner)
        {
            _inner = inner;
        }

        public volatile bool Down;
        public readonly ConcurrentQueue<int> CallThreads = new ConcurrentQueue<int>();
        public readonly List<PaymentTransactionRecord> SavedTransactions = new List<PaymentTransactionRecord>();
        public readonly List<ErrorLogRecord> SavedErrors = new List<ErrorLogRecord>();

        public string Name { get { return "Switchable"; } }

        public void EnsureCreated() { Enter(); _inner.EnsureCreated(); }
        public void SaveTransaction(PaymentTransactionRecord record) { Enter(); _inner.SaveTransaction(record); SavedTransactions.Add(record); }
        public void SaveError(ErrorLogRecord record) { Enter(); _inner.SaveError(record); SavedErrors.Add(record); }
        public void SaveMapping(TerminalMapping mapping) { Enter(); _inner.SaveMapping(mapping); }
        public void DeleteMapping(string workstation, TerminalProvider provider) { Enter(); _inner.DeleteMapping(workstation, provider); }
        public void SetActiveMapping(string workstation, TerminalProvider provider) { Enter(); _inner.SetActiveMapping(workstation, provider); }
        public TerminalMapping GetMapping(string workstation) { Enter(); return _inner.GetMapping(workstation); }
        public IList<TerminalMapping> GetMappings() { Enter(); return _inner.GetMappings(); }
        public IList<PaymentTransactionRecord> GetTransactions(DateTime fromUtc, DateTime toUtc) { Enter(); return _inner.GetTransactions(fromUtc, toUtc); }

        private void Enter()
        {
            CallThreads.Enqueue(Thread.CurrentThread.ManagedThreadId);
            if (Down)
                throw new InvalidOperationException("database is down (test)");
        }
    }
}
