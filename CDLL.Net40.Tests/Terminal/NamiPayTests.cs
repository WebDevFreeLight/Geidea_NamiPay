// =====================================================================================
// EN: NamiPay (SkyBand) provider: protocol rules, automatic Register + Start session, ECR reference counter,
//     every operation, every returned field, every outcome, card-data security, and the Geidea / NamiPay
//     router with "Detect". Uses FakeSkyband: no device, no ECR.dll, nothing charged.
// AR: مزوّد NamiPay (SkyBand): قواعد البروتوكول، التسجيل وبدء الجلسة تلقائياً، عدّاد مرجع ECR، كل العمليات والحقول
//     والنتائج، أمان بيانات البطاقة، والموجّه بين Geidea و NamiPay مع "الكشف". بدون جهاز ولا ECR.dll.
// =====================================================================================

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CDLL.Payments;
using CDLL.Payments.Terminal;
using CDLL.Payments.Terminal.Skyband;
using CDLL.Tests.Fakes;
using CDLL.Tests.Storage;
using Xunit;

namespace CDLL.Tests.Terminal
{
    public sealed class NamiPayTests : IDisposable
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 10, 12, 30, 45);
        private readonly string _folder = TestRecords.NewTempFolder();
        private readonly FakeSkyband _fake = new FakeSkyband();

        private static TerminalConnection Com8()
        {
            return new TerminalConnection { Transport = TerminalTransport.Com, ComPort = 8, BaudRate = 38400, DataBits = 8, Provider = TerminalProvider.NamiPay, CashRegisterNumber = "12345678" };
        }

        private SkybandTerminalService Service(TimeSpan? timeout = null)
        {
            return new SkybandTerminalService(_fake, _fake, new SkybandStateStore(_folder), new OutcomeClassifier(new[] { "000" }, null, null),
                timeout ?? TimeSpan.FromSeconds(10), clock: () => Now);
        }

        // =============================================================================
        // EN: PROTOCOL RULES / قواعد البروتوكول
        // =============================================================================

        [Fact]
        public void Signature_is_sha256_of_counter_plus_terminal_id_and_of_empty_before_register()
        {
            Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", SkybandProtocol.Signature("000001", null));
            Assert.Equal(64, SkybandProtocol.Signature("000001", "SKY12345").Length);
            Assert.NotEqual(SkybandProtocol.Signature("000001", "SKY12345"), SkybandProtocol.Signature("000002", "SKY12345"));
        }

        [Fact]
        public void Dates_are_Gregorian_whatever_the_Windows_calendar()
        {
            Assert.Equal("101126123045", SkybandProtocol.Timestamp(Now));
            Assert.Equal("111224", SkybandProtocol.OriginalDate(new DateTime(2024, 12, 11)));
        }

        [Fact]
        public void Decode_maps_command_letters_and_unwraps_a_full_repeat()
        {
            string[] status = SkybandProtocol.Decode(Bytes(";C3;00;READY"));
            Assert.Equal(new[] { "", "24", "00", "READY" }, status);

            var inner = Enumerable.Repeat("x", 40).ToArray();
            inner[1] = "C2"; inner[5] = "A1"; inner[6] = "000"; inner[7] = "APPROVED";
            string[] repeat = SkybandProtocol.Decode(Bytes(string.Join(";", inner)));
            Assert.Equal("0", repeat[1]);          // EN: the repeated purchase   AR: الشراء المعاد
            Assert.Equal("000", repeat[2]);
            Assert.Equal("APPROVED", repeat[3]);

            Assert.Equal("50", SkybandProtocol.CommandToType("ZZ"));
        }

        [Fact]
        public void Audit_text_has_no_expiry_and_no_full_card_number()
        {
            var f = Enumerable.Repeat("", 37).ToArray();
            f[1] = "0"; f[4] = "4464041234561234"; f[9] = "2812";
            string audit = SkybandProtocol.ForAudit(f);
            Assert.DoesNotContain("2812", audit);
            Assert.DoesNotContain("4464041234561234", audit);
            Assert.Contains("446404******1234", audit);
        }

        [Fact]
        public void Arabic_merchant_name_is_decoded_from_ISO_8859_6_hex()
        {
            Assert.Equal("مستشفى", SkybandProtocol.ArabicFromHex("E5D3CAD4E1E9"));
            Assert.Null(SkybandProtocol.ArabicFromHex("XYZ"));
        }

        // =============================================================================
        // EN: OPERATIONS / العمليات
        // =============================================================================

        [Fact]
        public async Task First_purchase_registers_starts_session_then_pays_with_all_fields()
        {
            TerminalResult r = await Service().PurchaseAsync(Com8(), 0.01m);

            Assert.Equal(new[] { 17, 18, 0 }, _fake.Requests.Select(q => q.Type).ToArray());
            Assert.Equal("101126123045;12345678!", _fake.Last(17).Request);
            Assert.Equal(SkybandProtocol.Signature("000001", null), _fake.Last(17).Signature);
            Assert.Equal("101126123045;1;0;12345678000001!", r.Request);
            Assert.Equal(SkybandProtocol.Signature("000001", "SKY12345"), _fake.Last(0).Signature);
            Assert.Equal(50000, _fake.Last(0).ReadTimeoutMs);

            Assert.Equal(TerminalProvider.NamiPay, r.Provider);
            Assert.Equal(TransactionOutcome.Approved, r.Outcome);
            Assert.Equal(0, r.ReturnCode);
            Assert.Equal("000", r.ResponseCode);
            Assert.Equal("APPROVED", r.ResponseMessage);
            Assert.Equal("446404******1234", r.MaskedPan);
            Assert.Equal("000000000001", r.TerminalAmount);
            Assert.Equal("000777", r.Stan);
            Assert.Equal("101126120000", r.TerminalDateTime);
            Assert.Equal("631412345678", r.Rrn);
            Assert.Equal("A1B2C3", r.AuthCode);
            Assert.Equal("SKY12345", r.TerminalId);
            Assert.Equal("123456789012345", r.MerchantId);
            Assert.Equal("mada", r.CardScheme);
            Assert.Equal("2.0.1", r.Version);
            Assert.Equal("KING FAHAD HOSPITAL", r.MerchantNameEn);
            Assert.Equal("مستشفى", r.MerchantNameAr);
            Assert.Equal("12345678000001", r.EcrRefNo);
            Assert.DoesNotContain("2812", r.RawResponse);
            Assert.DoesNotContain("4464041234561234", r.RawResponse);
            Assert.Equal(1L, r.AmountHalalas);
        }

        [Fact]
        public async Task Counter_goes_up_and_survives_a_restart_terminal_id_is_kept_session_is_restarted()
        {
            await Service().PurchaseAsync(Com8(), 0.01m);
            SkybandTerminalService same = Service();
            _fake.Requests.Clear();

            TerminalResult second = await same.PurchaseAsync(Com8(), 0.02m);
            TerminalResult third = await same.PurchaseAsync(Com8(), 0.03m);

            Assert.Equal(new[] { 18, 0, 0 }, _fake.Requests.Select(q => q.Type).ToArray());   // EN: no new Register   AR: بدون تسجيل جديد
            Assert.EndsWith("12345678000002!", second.Request);
            Assert.EndsWith("12345678000003!", third.Request);
            Assert.True(File.Exists(Path.Combine(_folder, "namipay", "state.json")));
        }

        [Fact]
        public async Task Refund_sends_RRN_and_original_date()
        {
            TerminalResult r = await Service().RefundAsync(Com8(), 0.5m, "434615455456", new DateTime(2024, 12, 11));

            Assert.Equal("101126123045;50;434615455456;0;111224;12345678000001!", r.Request);
            Assert.Equal(TerminalOperation.Refund, r.Operation);
            Assert.Equal(TransactionOutcome.Approved, r.Outcome);
            Assert.Equal("434615455456", r.OriginalRrn);
            Assert.Equal(TransactionOutcome.Error, (await Service().RefundAsync(Com8(), 0.5m, "12AB", DateTime.Today)).Outcome);
        }

        [Fact]
        public async Task Repeat_asks_for_the_last_purchase_reference()
        {
            SkybandTerminalService service = Service();
            Assert.Equal(TransactionOutcome.Error, (await service.RepeatLastAsync(Com8())).Outcome);   // EN: nothing to repeat yet   AR: لا شيء لإعادته

            await service.PurchaseAsync(Com8(), 0.01m);
            TerminalResult r = await service.RepeatLastAsync(Com8());

            Assert.Equal("101126123045;12345678000001;12345678000002!", r.Request);
            Assert.Equal(TransactionOutcome.Approved, r.Outcome);   // EN: previous answer "APPROVED"   AR: الرد السابق "مقبول"
            Assert.Equal("APPROVED", r.ResponseMessage);
        }

        [Fact]
        public async Task Status_reconciliation_and_terminal_id()
        {
            SkybandTerminalService service = Service();

            TerminalResult status = await service.CheckStatusAsync(Com8());
            Assert.Equal(TransactionOutcome.Ok, status.Outcome);
            Assert.Equal(5000, _fake.Last(24).ReadTimeoutMs);

            TerminalResult recon = await service.ReconcileAsync(Com8());
            Assert.Equal(TransactionOutcome.Ok, recon.Outcome);
            Assert.Equal("500", recon.ResponseCode);
            Assert.Equal("1;mada;0;1", recon.TerminalTotal);

            _fake.Requests.Clear();
            TerminalResult id = await service.GetTerminalIdAsync(Com8());
            Assert.Equal(TransactionOutcome.Ok, id.Outcome);
            Assert.Equal("SKY12345", id.TerminalId);
            Assert.Equal(new[] { 17 }, _fake.Requests.Select(q => q.Type).ToArray());

            Assert.Equal(TransactionOutcome.Error, (await service.GetMerchantInfoAsync(Com8())).Outcome);
        }

        [Theory]
        [InlineData("DECLINED", "100", TransactionOutcome.Declined)]
        [InlineData("CANCELLED BY USER", "", TransactionOutcome.Cancelled)]
        [InlineData("CARD READ TIMEOUT", "", TransactionOutcome.Timeout)]
        [InlineData("", "000", TransactionOutcome.Approved)]
        [InlineData("", "", TransactionOutcome.Failed)]
        public async Task Purchase_outcome_from_terminal_message_and_code(string message, string code, TransactionOutcome expected)
        {
            _fake.PurchaseMessage = message;
            _fake.PurchaseCode = code;

            TerminalResult r = await Service().PurchaseAsync(Com8(), 0.01m);

            Assert.Equal(expected, r.Outcome);
            if (!r.IsSuccess)
                Assert.NotNull(r.ErrorMessage);
        }

        [Fact]
        public async Task Link_problems_become_NoResponse_Timeout_or_Failed()
        {
            SkybandTerminalService service = Service();
            await service.CheckStatusAsync(Com8());   // EN: register + session done   AR: تم التسجيل والجلسة

            _fake.Fail = new SkybandLinkException(SkybandLinkException.OpenFailed, "COM8 could not be opened");
            Assert.Equal(TransactionOutcome.NoResponse, (await service.PurchaseAsync(Com8(), 0.01m)).Outcome);

            _fake.Fail = new SkybandLinkException(SkybandLinkException.NoAnswer, "No answer");
            Assert.Equal(TransactionOutcome.Timeout, (await service.PurchaseAsync(Com8(), 0.01m)).Outcome);

            _fake.RawAnswer = "garbage";
            TerminalResult unreadable = await service.PurchaseAsync(Com8(), 0.01m);
            Assert.Equal(TransactionOutcome.Failed, unreadable.Outcome);
            Assert.Equal(SkybandTerminalService.UnreadableAnswer, unreadable.ReturnCode);
        }

        [Fact]
        public async Task Session_refused_or_missing_cash_register_never_charges()
        {
            _fake.StartSessionCode = "05";
            TerminalResult r = await Service().PurchaseAsync(Com8(), 0.01m);
            Assert.Equal(TransactionOutcome.Failed, r.Outcome);
            Assert.Contains("Start session failed", r.ErrorMessage);
            Assert.DoesNotContain(_fake.Requests, q => q.Type == SkybandProtocol.Purchase);

            _fake.Requests.Clear();
            TerminalConnection noRegister = Com8();
            noRegister.CashRegisterNumber = "123";
            Assert.Equal(TransactionOutcome.Error, (await Service().PurchaseAsync(noRegister, 0.01m)).Outcome);
            Assert.Empty(_fake.Requests);
        }

        [Fact]
        public async Task Simulator_approves_through_the_real_service()
        {
            var sim = new SkybandSimulator { CardDelayMs = 0 };
            var service = new SkybandTerminalService(sim, sim, new SkybandStateStore(_folder), null, TimeSpan.FromSeconds(10));

            TerminalResult r = await service.PurchaseAsync(Com8(), 1.25m);

            Assert.Equal(TransactionOutcome.Approved, r.Outcome);
            Assert.Equal("125", r.TerminalAmount);
            Assert.Equal(SkybandSimulator.TerminalId, r.TerminalId);
            Assert.Equal("446404******0000", r.MaskedPan);
        }

        // =============================================================================
        // EN: ROUTER + DETECT / الموجّه والكشف
        // =============================================================================

        [Fact]
        public async Task Router_sends_each_call_to_the_terminal_provider()
        {
            var geideaFake = new FakeMadaApi();
            var geidea = new MadaTerminalService(geideaFake, new OutcomeClassifier(new[] { "000" }, null, null), TimeSpan.FromSeconds(10));
            var router = new ProviderTerminalService(geidea, Service());

            TerminalConnection asGeidea = Com8();
            asGeidea.Provider = TerminalProvider.Geidea;

            Assert.Equal(TerminalProvider.Geidea, (await router.PurchaseAsync(asGeidea, 0.01m)).Provider);
            Assert.Equal(1, geideaFake.Calls);
            Assert.Equal(TerminalProvider.NamiPay, (await router.PurchaseAsync(Com8(), 0.01m)).Provider);
            Assert.Equal(1, geideaFake.Calls);
        }

        [Theory]
        [InlineData(0, TerminalProvider.Geidea)]       // EN: Geidea answers   AR: Geidea يرد
        [InlineData(-4, TerminalProvider.NamiPay)]     // EN: Geidea times out, NamiPay answers   AR: Geidea لا يرد و NamiPay يرد
        public async Task Detect_finds_which_company_is_on_the_port(int geideaReturnCode, TerminalProvider expected)
        {
            var geideaFake = new FakeMadaApi { ReturnCode = geideaReturnCode };
            var router = new ProviderTerminalService(
                new MadaTerminalService(geideaFake, new OutcomeClassifier(new[] { "000" }, null, null), TimeSpan.FromSeconds(10)), Service());

            ProviderDetection detection = await router.DetectAsync(Com8());

            Assert.Equal(expected, detection.Provider);
            Assert.All(detection.Attempts, a => Assert.Equal(TerminalOperation.CheckStatus, a.Operation));
            Assert.Contains(expected.ToString(), detection.Describe());
        }

        [Fact]
        public async Task Detect_reports_nothing_when_neither_answers()
        {
            _fake.Fail = new SkybandLinkException(SkybandLinkException.NoAnswer, "No answer");
            var router = new ProviderTerminalService(
                new MadaTerminalService(new FakeMadaApi { ReturnCode = -4 }, new OutcomeClassifier(new[] { "000" }, null, null), TimeSpan.FromSeconds(10)), Service());

            ProviderDetection detection = await router.DetectAsync(Com8());

            Assert.Null(detection.Provider);
            Assert.Equal(2, detection.Attempts.Count);
            Assert.Contains("No Geidea or NamiPay terminal answered", detection.Describe());
        }

        private static byte[] Bytes(string text)
        {
            return System.Text.Encoding.ASCII.GetBytes(text);
        }

        public void Dispose()
        {
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }
    }
}
