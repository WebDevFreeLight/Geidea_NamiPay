// =====================================================================================
// EN: NamiPay provider: IPaymentTerminalService for SkyBand ECR terminals, on the SAME pipeline as Geidea
//     (TerminalServiceBase: validation, shared one-call gate, COM port check, background thread, watchdog),
//     so saving, audit, outcomes and the screens work the same for both companies.
//       Check status -> type 24 | Purchase -> 0 | Refund -> 2 | Repeat last -> 23 | Reconciliation -> 10
//       Terminal ID  -> 17 (Register)      | Merchant info -> not offered by the SkyBand protocol
//     Before the first call the service Registers the cash register (gets the terminal ID) and Starts the
//     session, automatically (the NamiPay app required the cashier to press both buttons first).
//     Needs the 8-digit cash register number (Terminal mapping / app.config "NamiPay.CashRegisterNumber").
// AR: مزوّد NamiPay: تطبيق IPaymentTerminalService لأجهزة SkyBand على نفس المسار المشترك مع Geidea، لذلك الحفظ
//     والتدقيق والنتائج والشاشات تعمل بنفس الطريقة للشركتين. قبل أول استدعاء تقوم الخدمة تلقائياً بالتسجيل
//     (للحصول على رقم الجهاز) وبدء الجلسة. تحتاج رقم الصندوق المكوّن من 8 أرقام.
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CDLL.Payments.Terminal.Skyband
{
    public sealed class SkybandTerminalService : TerminalServiceBase, IPaymentTerminalService
    {
        // EN: Our own return codes for SkyBand (the protocol has no DLL status code).   AR: رموز إرجاع خاصة بـ SkyBand.
        public const int Answered = 0;
        public const int UnreadableAnswer = -5;
        public const int RegisterOrSessionFailed = -6;

        private readonly ISkybandCodec _codec;
        private readonly ISkybandLink _link;
        private readonly SkybandStateStore _state;
        private readonly OutcomeClassifier _classifier;
        private readonly bool _terminalPrinter;
        private readonly Func<DateTime> _clock;
        private readonly HashSet<string> _sessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public SkybandTerminalService(ISkybandCodec codec, ISkybandLink link, SkybandStateStore state, OutcomeClassifier classifier,
                                      TimeSpan timeout, TimeSpan? statusTimeout = null, IComPortProbe portProbe = null,
                                      SemaphoreSlim gate = null, bool terminalPrinter = false, Func<DateTime> clock = null)
            : base(timeout, statusTimeout, portProbe, gate)
        {
            if (codec == null) throw new ArgumentNullException("codec");
            if (link == null) throw new ArgumentNullException("link");
            if (state == null) throw new ArgumentNullException("state");
            _codec = codec;
            _link = link;
            _state = state;
            _classifier = classifier ?? new OutcomeClassifier(new[] { "000" }, null, null);
            _terminalPrinter = terminalPrinter;
            _clock = clock ?? (() => DateTime.Now);
        }

        protected override TerminalProvider Provider { get { return TerminalProvider.NamiPay; } }

        private string Etp { get { return _terminalPrinter ? "1" : "0"; } }

        // =============================================================================
        // EN: OPERATIONS / العمليات
        // =============================================================================

        public Task<TerminalResult> CheckStatusAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.CheckStatus, null);
            return Start(result, connection, SkybandProtocol.CheckStatus, (time, ecr) => time + ";" + ecr);
        }

        public Task<TerminalResult> PurchaseAsync(TerminalConnection connection, decimal amountSar)
        {
            var result = NewResult(TerminalOperation.Purchase, null);
            long halalas;
            try { halalas = TerminalRequests.ToHalalas(amountSar); }
            catch (ArgumentException ex) { return Task.FromResult(InputError(result, connection, ex.Message)); }

            result.AmountHalalas = halalas;
            string amount = halalas.ToString(CultureInfo.InvariantCulture);
            return Start(result, connection, SkybandProtocol.Purchase, (time, ecr) => time + ";" + amount + ";" + Etp + ";" + ecr);
        }

        public Task<TerminalResult> RefundAsync(TerminalConnection connection, decimal amountSar, string originalRrn, DateTime originalDate)
        {
            var result = NewResult(TerminalOperation.Refund, null);
            result.OriginalRrn = originalRrn;
            result.OriginalDate = originalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!TerminalRequests.IsValidRrn(originalRrn))
                return Task.FromResult(InputError(result, connection, "Original RRN must be exactly 12 digits. / الرقم المرجعي يجب أن يكون 12 رقماً بالضبط."));

            long halalas;
            try { halalas = TerminalRequests.ToHalalas(amountSar); }
            catch (ArgumentException ex) { return Task.FromResult(InputError(result, connection, ex.Message)); }

            result.AmountHalalas = halalas;
            string amount = halalas.ToString(CultureInfo.InvariantCulture);
            string date = SkybandProtocol.OriginalDate(originalDate);
            return Start(result, connection, SkybandProtocol.Refund,
                (time, ecr) => time + ";" + amount + ";" + originalRrn + ";" + Etp + ";" + date + ";" + ecr);
        }

        public Task<TerminalResult> RepeatLastAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.RepeatLast, null);
            string previous = connection == null ? null : _state.Get(connection.CashRegisterNumber).LastTransactionEcrRef;
            if (connection != null && previous == null)
                return Task.FromResult(InputError(result, connection,
                    "No previous NamiPay purchase / refund from this PC to repeat. / لا توجد عملية سابقة من هذا الحاسوب لإعادتها."));
            return Start(result, connection, SkybandProtocol.Repeat, (time, ecr) => time + ";" + previous + ";" + ecr);
        }

        public Task<TerminalResult> ReconcileAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.Reconciliation, null);
            return Start(result, connection, SkybandProtocol.Reconciliation, (time, ecr) => time + ";" + Etp + ";" + ecr);
        }

        public Task<TerminalResult> GetMerchantInfoAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.MerchantInfo, null);
            return Task.FromResult(InputError(result, connection,
                "Merchant info is not offered by NamiPay terminals (the merchant name is printed in each purchase result). / " +
                "بيانات التاجر غير متاحة في أجهزة NamiPay (اسم التاجر يظهر في نتيجة كل عملية شراء)."));
        }

        /// <summary>EN: Register (type 17): returns the terminal ID and stores it.  AR: التسجيل: يُعيد رقم الجهاز ويحفظه.</summary>
        public Task<TerminalResult> GetTerminalIdAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.TerminalId, null);
            string problem = CheckCashRegister(connection);
            if (problem != null)
                return Task.FromResult(InputError(result, connection, problem));

            return RunAsync(result, connection, () =>
            {
                string[] fields;
                int rc = Register(connection, result, true, out fields);
                if (rc == Answered)
                    Fill(result, fields);
                return rc;
            }, null);
        }

        // =============================================================================
        // EN: PIPELINE / المسار
        // =============================================================================

        private Task<TerminalResult> Start(TerminalResult result, TerminalConnection connection, int type, Func<string, string, string> body)
        {
            string problem = CheckCashRegister(connection);
            if (problem != null)
                return Task.FromResult(InputError(result, connection, problem));

            return RunAsync(result, connection, () =>
            {
                string[] fields;
                int rc = EnsureReady(connection, result);
                if (rc != Answered)
                    return rc;

                rc = Exchange(connection, result, type, body, true, out fields);
                if (rc == Answered)
                {
                    Fill(result, fields);
                    if ((result.ResponseMessage ?? "").IndexOf("SESSION", StringComparison.OrdinalIgnoreCase) >= 0)
                        ForgetSession(connection);   // EN: terminal lost the session: start again next time   AR: بدء جلسة جديدة في المرة القادمة
                }
                return rc;
            }, null);
        }

        /// <summary>EN: Register once (terminal ID) and start the session once per run.  AR: التسجيل مرة واحدة وبدء الجلسة مرة لكل تشغيل.</summary>
        private int EnsureReady(TerminalConnection connection, TerminalResult result)
        {
            string[] fields;
            if (string.IsNullOrEmpty(_state.Get(connection.CashRegisterNumber).TerminalId))
            {
                int rc = Register(connection, result, false, out fields);
                if (rc != Answered)
                    return rc;
            }

            lock (_sessions)
            {
                if (_sessions.Contains(SessionKey(connection)))
                    return Answered;
            }

            int started = Exchange(connection, result, SkybandProtocol.StartSession,
                (time, ecr) => time + ";" + connection.CashRegisterNumber, false, out fields);
            if (started != Answered)
                return started;
            if (SkybandProtocol.Field(fields, 2) != "00")
            {
                result.ErrorMessage = "Start session failed / فشل بدء الجلسة: " + SkybandProtocol.Field(fields, 2) + " " + SkybandProtocol.Field(fields, 3);
                return RegisterOrSessionFailed;
            }
            lock (_sessions)
                _sessions.Add(SessionKey(connection));
            return Answered;
        }

        private int Register(TerminalConnection connection, TerminalResult result, bool forResult, out string[] fields)
        {
            int rc = Exchange(connection, result, SkybandProtocol.Register,
                (time, ecr) => time + ";" + connection.CashRegisterNumber, forResult, out fields);
            if (rc != Answered)
                return rc;

            string terminalId = SkybandProtocol.Field(fields, 3);
            if (terminalId == null || fields[1] != SkybandProtocol.Register.ToString(CultureInfo.InvariantCulture))
            {
                result.ErrorMessage = "Register failed / فشل التسجيل: " + SkybandProtocol.Field(fields, 2) + " " + terminalId;
                return RegisterOrSessionFailed;
            }

            SkybandState state = _state.Get(connection.CashRegisterNumber);
            state.TerminalId = terminalId;
            _state.Save(connection.CashRegisterNumber, state);
            ForgetSession(connection);   // EN: a new registration needs a new session   AR: تسجيل جديد يحتاج جلسة جديدة
            return Answered;
        }

        /// <summary>
        /// EN: One request / answer: builds the ECR reference + signature, packs, sends, reads, parses.
        /// AR: طلب ورد واحد: يبني مرجع ECR والتوقيع، يحزم، يرسل، يقرأ، ويحلل.
        /// </summary>
        private int Exchange(TerminalConnection connection, TerminalResult result, int type, Func<string, string, string> body, bool isMain, out string[] fields)
        {
            fields = new string[0];
            string cashRegister = connection.CashRegisterNumber;
            SkybandState state = _state.Get(cashRegister);

            // EN: Admin calls (register / session) do not move the counter.   AR: التسجيل والجلسة لا تزيد العدّاد.
            string counter = SkybandProtocol.IsAdmin(type)
                ? state.NextEcrRef.ToString("D6", CultureInfo.InvariantCulture)
                : _state.TakeEcrRef(cashRegister);
            string ecrReference = cashRegister + counter;
            string request = body(SkybandProtocol.Timestamp(_clock()), ecrReference) + "!";

            if (isMain)
                result.Request = request;
            if (type == SkybandProtocol.Purchase || type == SkybandProtocol.Refund)
            {
                SkybandState after = _state.Get(cashRegister);
                after.LastTransactionEcrRef = ecrReference;
                _state.Save(cashRegister, after);
            }

            byte[] frame = _codec.Pack(request, type, SkybandProtocol.Signature(counter, state.TerminalId));
            if (frame.Length == 0)
            {
                result.ErrorMessage = "ECR.dll returned an empty request / أعادت ECR.dll طلباً فارغاً";
                return UnreadableAnswer;
            }

            byte[] answer;
            try
            {
                answer = _link.Exchange(connection, frame, SkybandProtocol.ReadTimeoutMs(type));
            }
            catch (SkybandLinkException ex)
            {
                result.ErrorMessage = ex.Message;
                return ex.ReturnCode;
            }

            fields = SkybandProtocol.Decode(_codec.Parse(answer));
            if (fields.Length < 3 || fields[1] == "50")
            {
                result.RawResponse = SkybandProtocol.ForAudit(fields);
                result.ErrorMessage = "The NamiPay terminal answer could not be read / تعذرت قراءة رد جهاز NamiPay";
                return UnreadableAnswer;
            }
            return Answered;
        }

        /// <summary>EN: Copies every field the terminal returned (PAN masked, expiry skipped).  AR: ينسخ كل الحقول (البطاقة مُقنّعة وتاريخ الانتهاء متجاهَل).</summary>
        private static void Fill(TerminalResult r, string[] f)
        {
            r.ResponseCode = SkybandProtocol.Field(f, 2);
            r.ResponseMessage = SkybandProtocol.Field(f, 3);
            r.RawResponse = SkybandProtocol.ForAudit(f);

            if (SkybandProtocol.IsFinancial(f))
            {
                r.MaskedPan = CardDataMasker.MaskPan(SkybandProtocol.Field(f, SkybandProtocol.PanField));
                r.TerminalAmount = SkybandProtocol.Field(f, SkybandProtocol.AmountField);
                r.Stan = SkybandProtocol.Field(f, SkybandProtocol.StanField);
                r.TerminalDateTime = SkybandProtocol.Field(f, SkybandProtocol.DateTimeField);
                r.Rrn = SkybandProtocol.Field(f, SkybandProtocol.RrnField);
                r.AuthCode = SkybandProtocol.Field(f, SkybandProtocol.AuthCodeField);
                r.TerminalId = SkybandProtocol.Field(f, SkybandProtocol.TerminalIdField);
                r.MerchantId = SkybandProtocol.Field(f, SkybandProtocol.MerchantIdField);
                r.CardScheme = SkybandProtocol.Field(f, SkybandProtocol.SchemeField);
                r.Version = SkybandProtocol.Field(f, SkybandProtocol.VersionField);
                r.MerchantNameEn = SkybandProtocol.Field(f, SkybandProtocol.MerchantNameField);
                r.MerchantAddressEn = SkybandProtocol.Field(f, SkybandProtocol.MerchantAddressField);
                r.MerchantNameAr = SkybandProtocol.ArabicFromHex(SkybandProtocol.Field(f, SkybandProtocol.MerchantNameArField));
                r.MerchantAddressAr = SkybandProtocol.ArabicFromHex(SkybandProtocol.Field(f, SkybandProtocol.MerchantAddressArField));
                r.EcrRefNo = SkybandProtocol.Field(f, SkybandProtocol.EcrRefField);
                return;
            }

            switch (f[1])
            {
                case "17":   // EN: Register: terminal ID   AR: التسجيل: رقم الجهاز
                    r.TerminalId = SkybandProtocol.Field(f, 3);
                    break;
                case "23":   // EN: Repeat (short): previous transaction's answer   AR: إعادة مختصرة: نتيجة العملية السابقة
                    r.TerminalDateTime = SkybandProtocol.Field(f, 4);
                    r.ResponseMessage = SkybandProtocol.Field(f, 5) ?? r.ResponseMessage;
                    r.EcrRefNo = SkybandProtocol.Field(f, 6);
                    break;
                case "24":   // EN: Check status   AR: فحص الحالة
                    r.TerminalDateTime = SkybandProtocol.Field(f, 4);
                    r.EcrRefNo = SkybandProtocol.Field(f, 5);
                    break;
                case "10":   // EN: Reconciliation: totals follow the date-time   AR: التسوية: المجاميع بعد التاريخ
                    r.TerminalDateTime = SkybandProtocol.Field(f, 4);
                    r.TerminalTotal = f.Length > 5 ? string.Join(";", f, 5, f.Length - 5).Trim(';') : null;
                    break;
            }
        }

        /// <summary>
        /// EN: SkyBand outcome: transport codes first, then the terminal's message (APPROVED / DECLINED / CANCEL / TIMEOUT),
        ///     then the host response code lists of OutcomeClassifier.
        /// AR: نتيجة SkyBand: رموز الاتصال أولاً، ثم رسالة الجهاز، ثم قوائم رموز البنك في OutcomeClassifier.
        /// </summary>
        protected override void Complete(TerminalResult r)
        {
            switch (r.ReturnCode)
            {
                case Answered: break;
                case SkybandLinkException.OpenFailed: r.Outcome = TransactionOutcome.NoResponse; return;
                case SkybandLinkException.NoAnswer: r.Outcome = TransactionOutcome.Timeout; return;
                default: r.Outcome = TransactionOutcome.Failed; return;
            }

            string message = (r.ResponseMessage ?? "").ToUpperInvariant();
            switch (r.Operation)
            {
                case TerminalOperation.CheckStatus:
                    r.Outcome = TransactionOutcome.Ok;   // EN: the terminal answered the status request   AR: الجهاز رد على طلب الحالة
                    return;
                case TerminalOperation.TerminalId:
                    r.Outcome = string.IsNullOrEmpty(r.TerminalId) ? TransactionOutcome.Failed : TransactionOutcome.Ok;
                    return;
                case TerminalOperation.Reconciliation:
                    r.Outcome = r.ResponseCode == "500" || r.ResponseCode == "501" ? TransactionOutcome.Ok : TransactionOutcome.Failed;
                    break;
                default:
                    if (message.Contains("APPROVED"))
                        r.Outcome = TransactionOutcome.Approved;
                    else if (message.Contains("DECLINED"))
                        r.Outcome = TransactionOutcome.Declined;
                    else if (message.Contains("CANCEL"))
                        r.Outcome = TransactionOutcome.Cancelled;
                    else if (message.Contains("TIMEOUT") || message.Contains("TIME OUT"))
                        r.Outcome = TransactionOutcome.Timeout;
                    else
                        r.Outcome = _classifier.Classify(r.Operation, 0, r.ResponseCode, r.DurationMs);
                    break;
            }

            if (!r.IsSuccess && r.ErrorMessage == null)
                r.ErrorMessage = "Terminal / الجهاز: " + r.ResponseCode + " " + r.ResponseMessage;
        }

        private static string CheckCashRegister(TerminalConnection connection)
        {
            if (connection == null || TerminalConnection.IsValidCashRegister(connection.CashRegisterNumber))
                return null;
            return "NamiPay needs the 8-digit cash register number (Terminal mapping or NamiPay.CashRegisterNumber in app.config). / " +
                   "NamiPay يحتاج رقم الصندوق المكوّن من 8 أرقام (ربط الجهاز أو NamiPay.CashRegisterNumber).";
        }

        private static string SessionKey(TerminalConnection c)
        {
            return c.Describe() + "|" + c.CashRegisterNumber;
        }

        private void ForgetSession(TerminalConnection connection)
        {
            lock (_sessions)
                _sessions.Remove(SessionKey(connection));
        }
    }
}
