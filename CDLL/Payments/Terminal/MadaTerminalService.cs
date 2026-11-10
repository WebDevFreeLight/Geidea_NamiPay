// =====================================================================================
// EN: IPaymentTerminalService for mada / SPAN terminals, on top of IMadaNativeApi (real DLL, simulator or test fake).
//     For each operation it: validates the input, builds the request string, allocates the output buffers,
//     runs the DLL call on a background thread (Task.Run) behind a one-call-at-a-time gate, waits at most
//     "TerminalTimeoutSeconds", reads every returned field (card number masked, expiry never read) and
//     classifies the outcome with OutcomeClassifier.
// AR: تطبيق IPaymentTerminalService لأجهزة مدى / SPAN فوق IMadaNativeApi (المكتبة الفعلية أو المحاكي أو نسخة اختبار).
//     لكل عملية: يتحقق من المدخلات، يبني نص الطلب، يجهّز مخازن المخرجات، يشغّل استدعاء المكتبة على خيط خلفي
//     (Task.Run) مع السماح باستدعاء واحد فقط في كل مرة، ينتظر بحد أقصى "TerminalTimeoutSeconds"، يقرأ كل الحقول
//     المعادة (رقم البطاقة مُقنّع وتاريخ الانتهاء لا يُقرأ)، ثم يحدد النتيجة بواسطة OutcomeClassifier.
// =====================================================================================

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CDLL.Payments.Terminal
{
    public sealed class MadaTerminalService : IPaymentTerminalService
    {
        private readonly IMadaNativeApi _api;
        private readonly OutcomeClassifier _classifier;
        private readonly TimeSpan _timeout;

        // EN: Only one DLL call at a time. Two calls on the same COM port fail with "Port open Failed".
        // AR: استدعاء واحد فقط في كل مرة. استدعاءان على نفس منفذ COM يفشلان بخطأ "Port open Failed".
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public MadaTerminalService(IMadaNativeApi api, OutcomeClassifier classifier, TimeSpan timeout)
        {
            if (api == null) throw new ArgumentNullException("api");
            if (classifier == null) throw new ArgumentNullException("classifier");
            _api = api;
            _classifier = classifier;
            _timeout = timeout;
        }

        // =============================================================================
        // EN: OPERATIONS / العمليات
        // =============================================================================

        public Task<TerminalResult> CheckStatusAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.CheckStatus, TerminalRequests.CheckStatus);
            byte[] request = TerminalRequests.Ascii(result.Request);
            byte[] length = { (byte)request.Length };

            return RunAsync(result, connection, () => connection.Transport == TerminalTransport.Com
                ? _api.api_CheckStatus(connection.ComPort, connection.BaudRate, connection.Parity, connection.DataBits, connection.StopBits, request, length)
                : _api.api_TCPIPCheckStatus(TerminalRequests.CString(connection.TcpIp), TerminalRequests.CString(connection.TcpPort), request, length),
                null);
        }

        public Task<TerminalResult> PurchaseAsync(TerminalConnection connection, decimal amountSar)
        {
            var result = NewResult(TerminalOperation.Purchase, null);
            try
            {
                result.AmountHalalas = TerminalRequests.ToHalalas(amountSar);
                result.Request = TerminalRequests.Purchase(amountSar);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(InputError(result, connection, ex.Message));
            }
            return RunTransactionAsync(result, connection, TerminalRequests.PurchaseType);
        }

        public Task<TerminalResult> RefundAsync(TerminalConnection connection, decimal amountSar, string originalRrn, DateTime originalDate)
        {
            var result = NewResult(TerminalOperation.Refund, null);
            result.OriginalRrn = originalRrn;
            result.OriginalDate = originalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            try
            {
                result.AmountHalalas = TerminalRequests.ToHalalas(amountSar);
                result.Request = TerminalRequests.Refund(amountSar, originalRrn, originalDate);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(InputError(result, connection, ex.Message));
            }
            return RunTransactionAsync(result, connection, TerminalRequests.RefundType);
        }

        public Task<TerminalResult> RepeatLastAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.RepeatLast, TerminalRequests.RepeatLast);
            byte[] request = TerminalRequests.Ascii(result.Request);
            byte[] length = { (byte)request.Length };
            var b = new TransactionBuffers();
            byte[] cardScheme = new byte[10];

            return RunAsync(result, connection, () => connection.Transport == TerminalTransport.Com
                ? _api.api_RequestCOMRepeat(connection.ComPort, connection.BaudRate, connection.Parity, connection.DataBits, connection.StopBits, request, length,
                    b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.EcrRefNo, b.Version, b.OutResp, b.OutRespLen)
                : _api.api_RequestTCPIPRepeat(TerminalRequests.CString(connection.TcpIp), TerminalRequests.CString(connection.TcpPort), request, length,
                    b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.EcrRefNo, b.Version, cardScheme, b.OutResp, b.OutRespLen),
                () =>
                {
                    b.CopyTo(result);
                    result.CardScheme = Read(cardScheme);
                });
        }

        public Task<TerminalResult> ReconcileAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.Reconciliation, TerminalRequests.Reconciliation);
            byte[] request = TerminalRequests.Ascii(result.Request);
            byte[] length = { (byte)request.Length };

            // EN: Sized generously, as in the original sample.   AR: بأحجام كبيرة احتياطاً كما في المثال الأصلي.
            byte[] rspCode = new byte[30], terminalTotal = new byte[500], hostTotal = new byte[500];
            byte[] stanNo = new byte[30], dataTime = new byte[30], terminalId = new byte[30], merchantId = new byte[30];

            return RunAsync(result, connection, () =>
                {
                    if (connection.Transport == TerminalTransport.Com)
                        return _api.api_RequestReconCOMWORcpt(connection.ComPort, connection.BaudRate, connection.Parity, connection.DataBits, connection.StopBits,
                            request, length, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);

                    int requestLength = request.Length;   // EN: passed by ref   AR: يُمرَّر بالمرجع
                    return _api.api_RequestReconTCPIPWORcpt(TerminalRequests.CString(connection.TcpIp), TerminalRequests.CString(connection.TcpPort),
                        request, ref requestLength, rspCode, terminalTotal, hostTotal, stanNo, dataTime, terminalId, merchantId);
                },
                () =>
                {
                    result.ResponseCode = Read(rspCode);
                    result.TerminalTotal = Read(terminalTotal);
                    result.HostTotal = Read(hostTotal);
                    result.Stan = Read(stanNo);
                    result.TerminalDateTime = Read(dataTime);
                    result.TerminalId = Read(terminalId);
                    result.MerchantId = Read(merchantId);
                });
        }

        public Task<TerminalResult> GetMerchantInfoAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.MerchantInfo, TerminalRequests.MerchantInfo);
            if (connection.Transport != TerminalTransport.Com)
                return Task.FromResult(InputError(result, connection, "Merchant info is only available over USB/COM. / بيانات التاجر متاحة عبر USB/COM فقط."));

            byte[] request = TerminalRequests.Ascii(result.Request);
            byte[] length = { (byte)request.Length };
            byte[] nameEn = new byte[500], addressEn = new byte[500], addressOpt1 = new byte[500];
            var nameAr = new StringBuilder(500);
            var addressAr = new StringBuilder(500);
            var addressOpt2 = new StringBuilder(500);

            return RunAsync(result, connection,
                () => _api.api_GetMerchantDetails(connection.ComPort, connection.BaudRate, connection.Parity, connection.DataBits, connection.StopBits,
                    request, length, nameEn, nameAr, addressEn, addressAr, addressOpt1, addressOpt2),
                () =>
                {
                    result.MerchantNameEn = Read(nameEn);
                    result.MerchantNameAr = nameAr.ToString().Trim();
                    result.MerchantAddressEn = Read(addressEn);
                    result.MerchantAddressAr = addressAr.ToString().Trim();
                });
        }

        public Task<TerminalResult> GetTerminalIdAsync(TerminalConnection connection)
        {
            var result = NewResult(TerminalOperation.TerminalId, TerminalRequests.TerminalId);
            if (connection.Transport != TerminalTransport.Tcp)
                return Task.FromResult(InputError(result, connection, "Terminal ID is only available over TCP/IP. / رقم الجهاز متاح عبر TCP/IP فقط."));

            byte[] request = TerminalRequests.Ascii(result.Request);
            byte[] length = { (byte)request.Length };
            byte[] terminalId = new byte[17];

            return RunAsync(result, connection,
                () => _api.api_TCPIPGetTerminalID(TerminalRequests.CString(connection.TcpIp), TerminalRequests.CString(connection.TcpPort), request, length, terminalId),
                () => result.TerminalId = Read(terminalId));
        }

        // =============================================================================
        // EN: SHARED PIPELINE / المسار المشترك
        // =============================================================================

        /// <summary>EN: Purchase and refund share the same DLL functions and buffers.  AR: الشراء والاسترجاع يستخدمان نفس الدوال والمخازن.</summary>
        private Task<TerminalResult> RunTransactionAsync(TerminalResult result, TerminalConnection connection, int transactionType)
        {
            byte[] request = TerminalRequests.Ascii(result.Request);
            byte[] length = { (byte)request.Length };
            var b = new TransactionBuffers();

            return RunAsync(result, connection, () => connection.Transport == TerminalTransport.Com
                ? _api.api_RequestCOMTrxn(connection.ComPort, connection.BaudRate, connection.Parity, connection.DataBits, connection.StopBits, request, length, transactionType,
                    b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.EcrRefNo, b.Version, b.OutResp, b.OutRespLen)
                : _api.api_RequestTCPIPTrxn(TerminalRequests.CString(connection.TcpIp), TerminalRequests.CString(connection.TcpPort), request, length, transactionType,
                    b.PanNo, b.PurAmount, b.StanNo, b.DataTime, b.ExpDate, b.TrxRrn, b.AuthCode, b.RspCode, b.TerminalId, b.SchemeId, b.MerchantId, b.AddtlAmount, b.EcrRefNo, b.Version, b.OutResp, b.OutRespLen),
                () => b.CopyTo(result));
        }

        /// <summary>
        /// EN: Validates the connection, takes the one-call gate, runs the DLL call on a background thread with a
        ///     watchdog, reads the output fields and classifies the outcome. Never throws.
        /// AR: يتحقق من الاتصال، يأخذ قفل الاستدعاء الواحد، يشغّل استدعاء المكتبة على خيط خلفي مع مراقب للمهلة،
        ///     يقرأ حقول المخرجات ويحدد النتيجة. لا يرمي استثناءً أبداً.
        /// </summary>
        private async Task<TerminalResult> RunAsync(TerminalResult result, TerminalConnection connection, Func<IntPtr> nativeCall, Action readFields)
        {
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
            Task<IntPtr> call = Task.Run(nativeCall);

            // EN: Release the gate only when the DLL really returns: it can outlive the watchdog below.
            // AR: يُحرَّر القفل فقط عندما تعود المكتبة فعلاً، لأنها قد تستمر بعد انتهاء المراقب أدناه.
            2  call.ContinueWith(t => { var ignored = t.Exception; _gate.Release(); }, TaskScheduler.Default);

            Task finished = await Task.WhenAny(call, Task.Delay(_timeout)).ConfigureAwait(false);
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            if (finished != call)
            {
                result.Outcome = TransactionOutcome.Timeout;
                result.ErrorMessage = string.Format(CultureInfo.InvariantCulture,
                    "No answer from the terminal within {0} s. Use Repeat last before charging again. / لا رد من الجهاز خلال {0} ثانية. استخدم إعادة آخر عملية قبل الخصم مرة أخرى.",
                    (int)_timeout.TotalSeconds);
                return result;
            }

            try
            {
                result.ReturnCode = (int)call.Result.ToInt64();
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

            result.Outcome = _classifier.Classify(result.Operation, result.ReturnCode.Value, result.ResponseCode, result.DurationMs);
            if (result.ReturnCode == OutcomeClassifier.CancelledOrTimeoutReturnCode && result.ErrorMessage == null)
                result.ErrorMessage = CardStoppedMessage(result);
            return result;
        }

        /// <summary>
        /// EN: Message for return code 8, by outcome (the duration decided Timeout vs Cancelled).
        /// AR: رسالة رمز الإرجاع 8 حسب النتيجة (المدة حددت انتهاء المهلة أو الإلغاء).
        /// </summary>
        private string CardStoppedMessage(TerminalResult r)
        {
            string seconds = (r.DurationMs / 1000.0).ToString("0.0", CultureInfo.InvariantCulture);
            if (r.Outcome == TransactionOutcome.Timeout)
                return "No card was presented in time: the terminal stopped after " + seconds + " s (return code 8). / " +
                       "لم تُقدَّم البطاقة في الوقت المحدد: أوقف الجهاز العملية بعد " + seconds + " ثانية (رمز 8).";
            if (r.Outcome == TransactionOutcome.Cancelled)
                return "Cancelled on the terminal after " + seconds + " s (return code 8). / أُلغيت العملية على الجهاز بعد " + seconds + " ثانية (رمز 8).";
            return "Cancelled on the terminal, or no card was presented in time (return code 8). Check the terminal screen; use Repeat last if unsure. / " +
                   "أُلغيت العملية على الجهاز أو لم تُقدَّم البطاقة في الوقت المحدد (رمز 8). راجع شاشة الجهاز، واستخدم إعادة آخر عملية عند الشك.";
        }

        private static TerminalResult NewResult(TerminalOperation operation, string request)
        {
            return new TerminalResult { Operation = operation, Request = request };
        }

        private static TerminalResult InputError(TerminalResult result, TerminalConnection connection, string message)
        {
            if (connection != null)
            {
                result.Transport = connection.Transport;
                result.ConnectionTarget = connection.Describe();
            }
            result.Outcome = TransactionOutcome.Error;
            result.ErrorMessage = message;
            return result;
        }

        /// <summary>
        /// EN: Text the DLL wrote into a buffer, up to the first '\0', trimmed.
        /// AR: النص الذي كتبته المكتبة في المخزن حتى أول '\0' بدون مسافات زائدة.
        /// </summary>
        internal static string Read(byte[] buffer)
        {
            int end = Array.IndexOf(buffer, (byte)0);
            string text = Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end).Trim();
            return text.Length == 0 ? null : text;
        }

        /// <summary>
        /// EN: Output buffers of the purchase / refund / repeat functions. Size = max length + 1 for '\0'.
        /// AR: مخازن مخرجات دوال الشراء والاسترجاع والإعادة. الحجم = أقصى طول + 1 لحرف النهاية.
        /// </summary>
        private sealed class TransactionBuffers
        {
            public readonly byte[] PanNo = new byte[23];
            public readonly byte[] PurAmount = new byte[13];
            public readonly byte[] StanNo = new byte[7];
            public readonly byte[] DataTime = new byte[13];
            public readonly byte[] ExpDate = new byte[5];       // EN: required by the DLL, NEVER read or saved   AR: مطلوب للمكتبة ولا يُقرأ أو يُحفظ أبداً
            public readonly byte[] TrxRrn = new byte[13];
            public readonly byte[] AuthCode = new byte[7];
            public readonly byte[] RspCode = new byte[4];
            public readonly byte[] TerminalId = new byte[17];
            public readonly byte[] SchemeId = new byte[3];
            public readonly byte[] MerchantId = new byte[16];
            public readonly byte[] AddtlAmount = new byte[13];
            public readonly byte[] EcrRefNo = new byte[17];
            public readonly byte[] Version = new byte[10];
            public readonly byte[] OutRespLen = new byte[1];
            public readonly StringBuilder OutResp = new StringBuilder(15000);

            public void CopyTo(TerminalResult r)
            {
                r.MaskedPan = CardDataMasker.MaskPan(Read(PanNo));
                r.TerminalAmount = Read(PurAmount);
                r.Stan = Read(StanNo);
                r.TerminalDateTime = Read(DataTime);
                r.Rrn = Read(TrxRrn);
                r.AuthCode = Read(AuthCode);
                r.ResponseCode = Read(RspCode);
                r.TerminalId = Read(TerminalId);
                r.SchemeId = Read(SchemeId);
                r.MerchantId = Read(MerchantId);
                r.AdditionalAmount = Read(AddtlAmount);
                r.EcrRefNo = Read(EcrRefNo);
                r.Version = Read(Version);
                string raw = OutResp.ToString().Trim();
                r.RawResponse = raw.Length == 0 ? null : CardDataMasker.MaskText(raw);
                Array.Clear(ExpDate, 0, ExpDate.Length);   // EN: wipe the expiry   AR: مسح تاريخ الانتهاء
            }
        }
    }
}
