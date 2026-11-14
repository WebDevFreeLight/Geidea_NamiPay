// =====================================================================================
// EN: The pipeline shared by EVERY terminal provider (Geidea / madaapi and NamiPay / SkyBand), so both behave
//     exactly the same around the vendor call:
//       1. validate the connection (never reaches the terminal when wrong),
//       2. one call at a time (a shared gate: both providers may use the same COM port),
//       3. USB/COM: quick port check (exists, not in use) within 3 s,
//       4. the vendor call on a BACKGROUND thread with a watchdog (short for status checks),
//       5. the provider reads the fields and decides the outcome (Complete).
//     Never throws: every problem becomes an outcome + message in TerminalResult.
// AR: المسار المشترك لكل مزوّد أجهزة (Geidea / madaapi و NamiPay / SkyBand) حتى يتصرف الاثنان بنفس الطريقة:
//       1. التحقق من الاتصال، 2. استدعاء واحد في كل مرة (قفل مشترك لأن المزودين قد يستخدمان نفس المنفذ)،
//       3. فحص سريع لمنفذ COM، 4. استدعاء المورّد على خيط خلفي مع مراقب للمهلة، 5. المزوّد يقرأ الحقول ويحدد النتيجة.
//     لا يرمي استثناءً أبداً: كل مشكلة تصبح نتيجة ورسالة في TerminalResult.
// =====================================================================================

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CDLL.Payments.Terminal
{
    public abstract class TerminalServiceBase
    {
        /// <summary>EN: Default watchdog for Check status / Terminal ID / Merchant info (the terminal answers in ~1-2 s).  AR: المهلة الافتراضية لفحص الحالة ورقم الجهاز وبيانات التاجر.</summary>
        public static readonly TimeSpan DefaultStatusTimeout = TimeSpan.FromSeconds(20);

        /// <summary>EN: Time limit of the COM port check before each USB call.  AR: مهلة فحص منفذ COM قبل كل استدعاء USB.</summary>
        public static readonly TimeSpan PortCheckTimeout = TimeSpan.FromSeconds(3);

        private readonly TimeSpan _timeout;
        private readonly TimeSpan _statusTimeout;
        private readonly IComPortProbe _portProbe;
        private readonly SemaphoreSlim _gate;

        /// <param name="timeout">EN: watchdog for payments, refunds, repeat, reconciliation.  AR: المهلة للعمليات المالية والتسوية.</param>
        /// <param name="statusTimeout">EN: shorter watchdog for Check status / Terminal ID / Merchant info.  AR: مهلة أقصر لفحص الحالة ورقم الجهاز وبيانات التاجر.</param>
        /// <param name="portProbe">EN: quick COM port check before each USB call; null = no check (tests, simulator).  AR: فحص سريع للمنفذ، و null = بدون فحص.</param>
        /// <param name="gate">EN: one-call-at-a-time lock, shared between providers; null = own lock.  AR: قفل الاستدعاء الواحد المشترك بين المزودين.</param>
        protected TerminalServiceBase(TimeSpan timeout, TimeSpan? statusTimeout, IComPortProbe portProbe, SemaphoreSlim gate)
        {
            _timeout = timeout;
            _statusTimeout = statusTimeout ?? (timeout < DefaultStatusTimeout ? timeout : DefaultStatusTimeout);
            _portProbe = portProbe;
            _gate = gate ?? new SemaphoreSlim(1, 1);
        }

        /// <summary>EN: Saved in every TerminalResult / transaction row.  AR: يُحفظ في كل نتيجة وسجل عملية.</summary>
        protected abstract TerminalProvider Provider { get; }

        /// <summary>
        /// EN: Called after the vendor call returned: read the output fields and set result.Outcome (+ ErrorMessage).
        /// AR: تُستدعى بعد عودة استدعاء المورّد: قراءة الحقول وتحديد result.Outcome (والرسالة).
        /// </summary>
        protected abstract void Complete(TerminalResult result);

        /// <summary>
        /// EN: Runs one vendor call through the shared pipeline. vendorCall returns the return code (0 = the call worked)
        ///     and may fill result fields itself; readFields runs on the caller's side after it returned.
        /// AR: ينفّذ استدعاء مورّد واحد عبر المسار المشترك. vendorCall يُعيد رمز الإرجاع (0 = نجح الاستدعاء).
        /// </summary>
        protected async Task<TerminalResult> RunAsync(TerminalResult result, TerminalConnection connection, Func<int> vendorCall, Action readFields)
        {
            result.Provider = Provider;
            string invalid = connection == null ? "No terminal connection. / لا يوجد اتصال بالجهاز." : connection.Validate();
            if (invalid != null)
                return InputError(result, connection, invalid);

            result.Transport = connection.Transport;
            result.ConnectionTarget = connection.Describe();

            if (!await _gate.WaitAsync(0).ConfigureAwait(false))
            {
                result.Outcome = TransactionOutcome.Failed;
                result.ErrorMessage = "Terminal is busy with another operation. / الجهاز مشغول بعملية أخرى.";
                return result;
            }

            result.StartedAtUtc = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            // EN: USB/COM: make sure the port exists and is free BEFORE the vendor call, so a wrong / unplugged / busy
            //     port fails in seconds instead of leaving the call waiting.
            // AR: USB/COM: التأكد من وجود المنفذ وأنه غير مستخدم قبل الاستدعاء، حتى يفشل المنفذ الخاطئ خلال ثوانٍ.
            if (connection.Transport == TerminalTransport.Com && _portProbe != null)
            {
                string portProblem;
                try
                {
                    portProblem = await _portProbe.CheckAsync(connection.ComPort, PortCheckTimeout).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    portProblem = ex.GetType().Name + ": " + ex.Message;
                }

                if (portProblem != null)
                {
                    _gate.Release();
                    result.DurationMs = stopwatch.ElapsedMilliseconds;
                    result.Outcome = TransactionOutcome.NoResponse;
                    result.ErrorMessage = portProblem;
                    return result;
                }
            }

            Task<int> call = Task.Run(vendorCall);

            // EN: Release the gate only when the vendor call really returns: it can outlive the watchdog below.
            //     The watchdog waits for THIS continuation (not for "call"), so the gate is already free when the
            //     result comes back and the next call (e.g. NamiPay Purchase right after Start session) is never "busy".
            // AR: يُحرَّر القفل فقط عندما يعود الاستدعاء فعلاً، لأنه قد يستمر بعد انتهاء المراقب أدناه.
            //     المراقب ينتظر هذا الإكمال، فيكون القفل محرراً عند عودة النتيجة ولا يظهر "الجهاز مشغول" للاستدعاء التالي.
            Task released = call.ContinueWith(t => { var ignored = t.Exception; _gate.Release(); }, TaskScheduler.Default);

            TimeSpan timeout = IsQuickCheck(result.Operation) ? _statusTimeout : _timeout;
            Task finished = await Task.WhenAny(released, Task.Delay(timeout)).ConfigureAwait(false);
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            if (finished != released)
            {
                result.Outcome = TransactionOutcome.Timeout;
                result.ErrorMessage = string.Format(CultureInfo.InvariantCulture,
                    "No answer from the terminal within {0} s. Use Repeat last before charging again. / لا رد من الجهاز خلال {0} ثانية. استخدم إعادة آخر عملية قبل الخصم مرة أخرى.",
                    (int)timeout.TotalSeconds);
                return result;
            }

            try
            {
                result.ReturnCode = call.Result;
            }
            catch (AggregateException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                result.Outcome = TransactionOutcome.Error;
                result.ErrorMessage = CardDataMasker.MaskText(inner.GetType().Name + ": " + inner.Message);
                return result;
            }

            if (readFields != null)
                readFields();

            Complete(result);
            return result;
        }

        /// <summary>EN: Calls that need no card or bank: they get the short watchdog.  AR: استدعاءات لا تحتاج بطاقة أو بنكاً: تأخذ المهلة القصيرة.</summary>
        protected static bool IsQuickCheck(TerminalOperation operation)
        {
            return operation == TerminalOperation.CheckStatus || operation == TerminalOperation.TerminalId || operation == TerminalOperation.MerchantInfo;
        }

        protected TerminalResult NewResult(TerminalOperation operation, string request)
        {
            return new TerminalResult { Operation = operation, Request = request, Provider = Provider };
        }

        protected TerminalResult InputError(TerminalResult result, TerminalConnection connection, string message)
        {
            result.Provider = Provider;
            if (connection != null)
            {
                result.Transport = connection.Transport;
                result.ConnectionTarget = connection.Describe();
            }
            result.Outcome = TransactionOutcome.Error;
            result.ErrorMessage = message;
            return result;
        }
    }
}
