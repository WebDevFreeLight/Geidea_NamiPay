// =====================================================================================
// EN: MadaTerminalService against the fake terminal: every outcome (approved, declined, cancelled, timeout,
//     no response, failed, error, ok), every operation over COM and TCP, every returned field, the
//     one-call-at-a-time gate, the watchdog timeout, background thread use and card-data masking.
// AR: اختبار MadaTerminalService مع الجهاز الوهمي: كل النتائج، كل العمليات عبر COM و TCP، كل الحقول المعادة،
//     السماح باستدعاء واحد، مراقب المهلة، استخدام الخيط الخلفي وتقنيع بيانات البطاقة.
// =====================================================================================

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CDLL.Payments;
using CDLL.Payments.Terminal;
using CDLL.Tests.Fakes;
using Xunit;

namespace CDLL.Tests.Terminal
{
    public class MadaTerminalServiceTests
    {
        private static readonly TerminalConnection Com8 = new TerminalConnection { Transport = TerminalTransport.Com, ComPort = 8, BaudRate = 38400, DataBits = 8 };
        private static readonly TerminalConnection Tcp = new TerminalConnection { Transport = TerminalTransport.Tcp, TcpIp = "192.168.1.25", TcpPort = "6100" };

        private readonly FakeMadaApi _fake = new FakeMadaApi();

        private MadaTerminalService Service(TimeSpan? timeout = null)
        {
            var classifier = new OutcomeClassifier(new[] { "000" }, new[] { "400" }, new[] { "911" });
            return new MadaTerminalService(_fake, classifier, timeout ?? TimeSpan.FromSeconds(10));
        }

        // =============================================================================
        // EN: EVERY OUTCOME / كل النتائج
        // =============================================================================

        [Theory]
        [InlineData(0, "000", TransactionOutcome.Approved)]    // EN: paid                       AR: تم الدفع
        [InlineData(0, "100", TransactionOutcome.Declined)]    // EN: refused by the bank         AR: رفض البنك
        [InlineData(0, "400", TransactionOutcome.Cancelled)]   // EN: cancel response code        AR: رمز إلغاء من البنك
        [InlineData(8, null, TransactionOutcome.Cancelled)]    // EN: code 8 at once = cancel pressed   AR: رمز 8 فوراً = ضُغط الإلغاء
        [InlineData(0, "911", TransactionOutcome.Timeout)]     // EN: host timeout code           AR: رمز انتهاء مهلة البنك
        [InlineData(-4, null, TransactionOutcome.Timeout)]     // EN: communication timeout       AR: انتهاء مهلة الاتصال
        [InlineData(-3, null, TransactionOutcome.NoResponse)]  // EN: no terminal                 AR: لا يوجد جهاز
        [InlineData(-1, null, TransactionOutcome.Failed)]      // EN: other DLL error             AR: خطأ آخر من المكتبة
        [InlineData(0, "", TransactionOutcome.Failed)]         // EN: answered without host code  AR: رد بدون رمز البنك
        public async Task Purchase_outcome_follows_return_code_and_response_code(int returnCode, string responseCode, TransactionOutcome expected)
        {
            _fake.ReturnCode = returnCode;
            _fake.ResponseCode = responseCode;

            TerminalResult result = await Service().PurchaseAsync(Com8, 0.01m);

            Assert.Equal(expected, result.Outcome);
            Assert.Equal(returnCode, result.ReturnCode);
        }

        [Fact]
        public async Task Return_code_8_before_the_card_timeout_is_Cancelled()
        {
            _fake.ReturnCode = OutcomeClassifier.CancelledOrTimeoutReturnCode;

            TerminalResult result = await Service().PurchaseAsync(Com8, 0.01m);

            Assert.Equal(TransactionOutcome.Cancelled, result.Outcome);
            Assert.False(result.IsSuccess);
            Assert.Contains("Cancelled on the terminal after", result.ErrorMessage);
            Assert.Contains("return code 8", result.ErrorMessage);
        }

        [Fact]
        public async Task Return_code_8_after_the_card_timeout_is_Timeout()
        {
            // EN: Card timeout shortened to 200 ms so the test does not wait 15 s.   AR: مهلة البطاقة 200 ملي ثانية حتى لا ينتظر الاختبار 15 ثانية.
            var classifier = new OutcomeClassifier(new[] { "000" }, null, null, TimeSpan.FromMilliseconds(200));
            var service = new MadaTerminalService(_fake, classifier, TimeSpan.FromSeconds(10));
            _fake.ReturnCode = OutcomeClassifier.CancelledOrTimeoutReturnCode;
            _fake.DelayMs = 300;

            TerminalResult result = await service.PurchaseAsync(Com8, 0.01m);

            Assert.Equal(TransactionOutcome.Timeout, result.Outcome);
            Assert.True(result.DurationMs >= 200);
            Assert.Contains("No card was presented in time", result.ErrorMessage);
        }

        [Fact]
        public async Task Dll_exception_is_recorded_as_Error_and_never_thrown()
        {
            _fake.Throw = new DllNotFoundException("Unable to load DLL 'madaapi_v1_9.dll'");

            TerminalResult result = await Service().PurchaseAsync(Com8, 0.01m);

            Assert.Equal(TransactionOutcome.Error, result.Outcome);
            Assert.Null(result.ReturnCode);
            Assert.Contains("madaapi_v1_9.dll", result.ErrorMessage);
        }

        [Fact]
        public async Task Terminal_that_never_answers_is_a_Timeout_and_blocks_new_calls_until_it_returns()
        {
            _fake.Gate = new ManualResetEventSlim(false);
            MadaTerminalService service = Service(TimeSpan.FromMilliseconds(300));

            TerminalResult first = await service.PurchaseAsync(Com8, 0.01m);
            Assert.Equal(TransactionOutcome.Timeout, first.Outcome);
            Assert.Null(first.ReturnCode);
            Assert.Contains("Repeat last", first.ErrorMessage);

            // EN: The DLL call is still running: a second call must not hit the same port.
            // AR: استدعاء المكتبة ما زال يعمل: الاستدعاء الثاني يجب ألا يستخدم نفس المنفذ.
            TerminalResult busy = await service.CheckStatusAsync(Com8);
            Assert.Equal(TransactionOutcome.Failed, busy.Outcome);
            Assert.Contains("busy", busy.ErrorMessage);
            Assert.Equal(1, _fake.Calls);

            _fake.Gate.Set();
            await WaitUntil(() => service.CheckStatusAsync(Com8).Result.Outcome == TransactionOutcome.Ok);
        }

        [Fact]
        public async Task Back_to_back_calls_are_never_busy()
        {
            // EN: The gate is free as soon as a result comes back (NamiPay runs Start session then Purchase at once).
            // AR: القفل محرر فور عودة النتيجة (NamiPay ينفّذ بدء الجلسة ثم الشراء مباشرة).
            MadaTerminalService service = Service();
            for (int i = 0; i < 200; i++)
            {
                TerminalResult result = await service.CheckStatusAsync(Com8);
                Assert.True(result.Outcome == TransactionOutcome.Ok, "call " + i + ": " + result.ErrorMessage);
            }
        }

        [Theory]
        [InlineData(0, TransactionOutcome.Ok)]
        [InlineData(-3, TransactionOutcome.NoResponse)]
        [InlineData(-4, TransactionOutcome.Timeout)]
        [InlineData(-2, TransactionOutcome.Failed)]
        public async Task Check_status_outcomes(int returnCode, TransactionOutcome expected)
        {
            _fake.ReturnCode = returnCode;
            Assert.Equal(expected, (await Service().CheckStatusAsync(Com8)).Outcome);
            Assert.Equal(expected, (await Service().CheckStatusAsync(Tcp)).Outcome);
        }

        // =============================================================================
        // EN: EVERY RETURNED FIELD / كل الحقول المعادة
        // =============================================================================

        [Fact]
        public async Task Purchase_returns_every_field_with_the_card_number_masked()
        {
            TerminalResult r = await Service().PurchaseAsync(Com8, 0.01m);

            Assert.Equal(TerminalOperation.Purchase, r.Operation);
            Assert.Equal(TerminalTransport.Com, r.Transport);
            Assert.Equal("1;0;1!", r.Request);
            Assert.Equal(1L, r.AmountHalalas);
            Assert.Equal("000", r.ResponseCode);
            Assert.Equal("446404******1234", r.MaskedPan);
            Assert.Equal("000000000001", r.TerminalAmount);
            Assert.Equal("000123", r.Stan);
            Assert.Equal("261110120000", r.TerminalDateTime);
            Assert.Equal("631412345678", r.Rrn);
            Assert.Equal("A1B2C3", r.AuthCode);
            Assert.Equal("1234567812345678", r.TerminalId);
            Assert.Equal("P1", r.SchemeId);
            Assert.Equal("123456789012345", r.MerchantId);
            Assert.Equal("000000000000", r.AdditionalAmount);
            Assert.Equal("ECR0000000000001", r.EcrRefNo);
            Assert.Equal("1.9", r.Version);
            Assert.Equal("RAW|446404******1234|000|APPROVED", r.RawResponse);
            Assert.Equal("COM8 38400 baud, parity 0, 8 data bits, stop bits 0", r.ConnectionTarget);
            Assert.True(r.IsSuccess);
        }

        [Fact]
        public async Task Expiry_date_is_never_returned_and_is_wiped_from_the_buffer()
        {
            TerminalResult r = await Service().PurchaseAsync(Com8, 0.01m);

            Assert.All(_fake.LastExpiryBuffer, b => Assert.Equal(0, b));
            foreach (var property in typeof(TerminalResult).GetProperties().Where(p => p.PropertyType == typeof(string)))
                Assert.NotEqual(_fake.Expiry, (string)property.GetValue(r));
        }

        [Fact]
        public async Task Purchase_and_refund_send_the_right_function_type_and_request()
        {
            MadaTerminalService service = Service();

            await service.PurchaseAsync(Tcp, 43.71m);
            Assert.Equal("api_RequestTCPIPTrxn", _fake.LastFunction);
            Assert.Equal(TerminalRequests.PurchaseType, _fake.LastTransactionType);
            Assert.Equal("4371;0;1!", _fake.LastRequest);
            Assert.Equal("192.168.1.25", _fake.LastIp);
            Assert.Equal("6100", _fake.LastTcpPort);
            Assert.Equal(0, _fake.LastIpBytes.Last());   // EN: C string   AR: نص C ينتهي بـ '\0'

            TerminalResult refund = await service.RefundAsync(Com8, 0.5m, "434615455456", new DateTime(2024, 12, 11));
            Assert.Equal("api_RequestCOMTrxn", _fake.LastFunction);
            Assert.Equal(TerminalRequests.RefundType, _fake.LastTransactionType);
            Assert.Equal("50;434615455456;11122024;1;1!", _fake.LastRequest);
            Assert.Equal(8, _fake.LastComPort);
            Assert.Equal("434615455456", refund.OriginalRrn);
            Assert.Equal("2024-12-11", refund.OriginalDate);
            Assert.Equal(TerminalOperation.Refund, refund.Operation);
        }

        [Fact]
        public async Task Repeat_last_returns_all_fields_and_card_scheme_over_TCP()
        {
            TerminalResult r = await Service().RepeatLastAsync(Tcp);

            Assert.Equal("api_RequestTCPIPRepeat", _fake.LastFunction);
            Assert.Equal("0B!", r.Request);
            Assert.Equal(TransactionOutcome.Approved, r.Outcome);
            Assert.Equal("MADA", r.CardScheme);
            Assert.Equal("631412345678", r.Rrn);
            Assert.Equal("446404******1234", r.MaskedPan);

            r = await Service().RepeatLastAsync(Com8);
            Assert.Equal("api_RequestCOMRepeat", _fake.LastFunction);
            Assert.Equal("A1B2C3", r.AuthCode);
        }

        [Fact]
        public async Task Reconciliation_returns_totals_over_COM_and_TCP()
        {
            foreach (TerminalConnection connection in new[] { Com8, Tcp })
            {
                TerminalResult r = await Service().ReconcileAsync(connection);
                Assert.Equal("09;0!", _fake.LastRequest);
                Assert.Equal(TransactionOutcome.Ok, r.Outcome);
                Assert.Equal("PUR 001 000000000001", r.TerminalTotal);
                Assert.Equal("PUR 001 000000000001", r.HostTotal);
                Assert.Equal("000123", r.Stan);
                Assert.Equal("1234567812345678", r.TerminalId);
                Assert.Equal("123456789012345", r.MerchantId);
                Assert.Equal("000", r.ResponseCode);
            }
        }

        [Fact]
        public async Task Merchant_info_returns_English_and_Arabic_over_COM_only()
        {
            TerminalResult r = await Service().GetMerchantInfoAsync(Com8);
            Assert.Equal(TransactionOutcome.Ok, r.Outcome);
            Assert.Equal("0A!", _fake.LastRequest);
            Assert.Equal("KING FAHAD HOSPITAL", r.MerchantNameEn);
            Assert.Equal("مستشفى الملك فهد", r.MerchantNameAr);
            Assert.Equal("RIYADH", r.MerchantAddressEn);
            Assert.Equal("الرياض", r.MerchantAddressAr);

            TerminalResult overTcp = await Service().GetMerchantInfoAsync(Tcp);
            Assert.Equal(TransactionOutcome.Error, overTcp.Outcome);
            Assert.Equal(1, _fake.Calls);
        }

        [Fact]
        public async Task Terminal_id_over_TCP_only()
        {
            TerminalResult r = await Service().GetTerminalIdAsync(Tcp);
            Assert.Equal(TransactionOutcome.Ok, r.Outcome);
            Assert.Equal("07!", _fake.LastRequest);
            Assert.Equal("1234567812345678", r.TerminalId);

            Assert.Equal(TransactionOutcome.Error, (await Service().GetTerminalIdAsync(Com8)).Outcome);
            Assert.Equal(1, _fake.Calls);
        }

        // =============================================================================
        // EN: INPUT CHECKS, THREADS / التحقق من المدخلات والخيوط
        // =============================================================================

        [Fact]
        public async Task Bad_input_never_reaches_the_terminal()
        {
            MadaTerminalService service = Service();
            var badIp = new TerminalConnection { Transport = TerminalTransport.Tcp, TcpIp = "999.1.1", TcpPort = "6100" };
            var badPort = new TerminalConnection { Transport = TerminalTransport.Tcp, TcpIp = "10.0.0.1", TcpPort = "70000" };
            var badCom = new TerminalConnection { Transport = TerminalTransport.Com, ComPort = 0 };

            Assert.Equal(TransactionOutcome.Error, (await service.CheckStatusAsync(badIp)).Outcome);
            Assert.Equal(TransactionOutcome.Error, (await service.CheckStatusAsync(badPort)).Outcome);
            Assert.Equal(TransactionOutcome.Error, (await service.CheckStatusAsync(badCom)).Outcome);
            Assert.Equal(TransactionOutcome.Error, (await service.PurchaseAsync(Com8, 0m)).Outcome);
            Assert.Equal(TransactionOutcome.Error, (await service.PurchaseAsync(Com8, -5m)).Outcome);
            Assert.Equal(TransactionOutcome.Error, (await service.RefundAsync(Com8, 1m, "12AB", DateTime.Today)).Outcome);
            Assert.Equal(TransactionOutcome.Error, (await service.CheckStatusAsync(null)).Outcome);
            Assert.Equal(0, _fake.Calls);
        }

        [Fact]
        public void Dll_call_never_runs_on_the_calling_UI_thread()
        {
            // EN: A dedicated thread stands in for the UI thread and stays blocked while it waits,
            //     so the DLL call can only have run somewhere else.
            // AR: خيط مخصص يمثّل خيط الواجهة ويبقى منتظراً، لذلك استدعاء المكتبة لا يمكن أن يتم إلا على خيط آخر.
            int uiThread = 0;
            var ui = new Thread(() =>
            {
                uiThread = Thread.CurrentThread.ManagedThreadId;
                Service().CheckStatusAsync(Com8).Wait();
            });
            ui.Start();
            ui.Join();

            Assert.Equal(1, _fake.Calls);
            Assert.NotEqual(uiThread, _fake.LastCallThreadId);
        }

        [Fact]
        public async Task Simulator_terminal_works_through_the_same_service()
        {
            Simulator.MadaApiSimulator.CardDelayMs = 0;
            Simulator.MadaApiSimulator.Scenario = Simulator.SimulationScenario.Approved;
            var service = new MadaTerminalService(new SimulatorMadaApi(), new OutcomeClassifier(new[] { "000" }, null, null), TimeSpan.FromSeconds(30));

            TerminalResult r = await service.PurchaseAsync(Com8, 0.01m);

            Assert.Equal(TransactionOutcome.Approved, r.Outcome);
            Assert.NotNull(r.Rrn);
        }

        private static async Task WaitUntil(Func<bool> condition)
        {
            for (int i = 0; i < 100; i++)
            {
                if (condition())
                    return;
                await Task.Delay(50);
            }
            Assert.True(condition(), "condition not reached in 5 s");
        }
    }
}
