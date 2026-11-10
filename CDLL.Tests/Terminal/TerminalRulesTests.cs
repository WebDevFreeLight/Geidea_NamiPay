// =====================================================================================
// EN: Request strings, halalas, card masking, outcome rules, connection validation, mapping permissions.
// AR: نصوص الطلبات، الهللات، تقنيع البطاقة، قواعد النتائج، التحقق من الاتصال، صلاحيات الربط.
// =====================================================================================

using System;
using System.Security.Principal;
using CDLL.Payments;
using CDLL.Payments.Mapping;
using CDLL.Payments.Terminal;
using Xunit;

namespace CDLL.Tests.Terminal
{
    public class TerminalRulesTests
    {
        [Theory]
        [InlineData("0.01", "1;0;1!")]
        [InlineData("43.71", "4371;0;1!")]
        [InlineData("100", "10000;0;1!")]
        [InlineData("0.005", "1;0;1!")]   // EN: rounds like the original sample   AR: يقرّب مثل المثال الأصلي
        public void Purchase_request_is_in_halalas(string amount, string expected)
        {
            Assert.Equal(expected, TerminalRequests.Purchase(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
        }

        [Fact]
        public void Refund_request_uses_Gregorian_ddMMyyyy_and_checks_the_RRN()
        {
            Assert.Equal("50;434615455456;11122024;1;1!", TerminalRequests.Refund(0.5m, "434615455456", new DateTime(2024, 12, 11)));
            Assert.Throws<ArgumentException>(() => TerminalRequests.Refund(0.5m, "43461545545", DateTime.Today));
            Assert.Throws<ArgumentException>(() => TerminalRequests.Refund(0.5m, "43461545545X", DateTime.Today));
            Assert.Throws<ArgumentException>(() => TerminalRequests.Refund(0.5m, null, DateTime.Today));
        }

        [Fact]
        public void Zero_or_negative_amount_is_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TerminalRequests.ToHalalas(0m));
            Assert.Throws<ArgumentOutOfRangeException>(() => TerminalRequests.ToHalalas(-1m));
            Assert.Throws<ArgumentOutOfRangeException>(() => TerminalRequests.ToHalalas(0.004m));
        }

        [Theory]
        [InlineData("4464041234561234", "446404******1234")]
        [InlineData("446404******1234", "446404******1234")]
        [InlineData("4464041234561234567", "446404*********4567")]
        [InlineData("123456", "******")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void Pan_keeps_only_first6_last4(string pan, string expected)
        {
            Assert.Equal(expected, CardDataMasker.MaskPan(pan));
        }

        [Fact]
        public void Card_numbers_inside_any_text_are_masked()
        {
            Assert.Equal("card 446404******1234 declined, rrn 631412345678",
                CardDataMasker.MaskText("card 4464041234561234 declined, rrn 631412345678"));
        }

        [Theory]
        [InlineData(TerminalOperation.Purchase, 0, "000", TransactionOutcome.Approved)]
        [InlineData(TerminalOperation.Refund, 0, "000", TransactionOutcome.Approved)]
        [InlineData(TerminalOperation.RepeatLast, 0, "000", TransactionOutcome.Approved)]
        [InlineData(TerminalOperation.Purchase, 0, "C1", TransactionOutcome.Cancelled)]
        [InlineData(TerminalOperation.Purchase, 0, "c1", TransactionOutcome.Cancelled)]
        [InlineData(TerminalOperation.Purchase, 0, "T9", TransactionOutcome.Timeout)]
        [InlineData(TerminalOperation.Purchase, 0, "980", TransactionOutcome.Declined)]
        [InlineData(TerminalOperation.Purchase, 0, null, TransactionOutcome.Failed)]
        [InlineData(TerminalOperation.Purchase, -3, null, TransactionOutcome.NoResponse)]
        [InlineData(TerminalOperation.Purchase, -4, null, TransactionOutcome.Timeout)]
        [InlineData(TerminalOperation.Purchase, 8, null, TransactionOutcome.CancelledOrTimeout)]
        [InlineData(TerminalOperation.Refund, 8, null, TransactionOutcome.CancelledOrTimeout)]
        [InlineData(TerminalOperation.Purchase, 7, "000", TransactionOutcome.Failed)]
        [InlineData(TerminalOperation.CheckStatus, 0, null, TransactionOutcome.Ok)]
        [InlineData(TerminalOperation.Reconciliation, 0, "500", TransactionOutcome.Ok)]
        [InlineData(TerminalOperation.MerchantInfo, 0, null, TransactionOutcome.Ok)]
        [InlineData(TerminalOperation.TerminalId, 0, null, TransactionOutcome.Ok)]
        public void Outcome_rules(TerminalOperation operation, int returnCode, string responseCode, TransactionOutcome expected)
        {
            var classifier = new OutcomeClassifier(new[] { "000" }, new[] { "C1" }, new[] { "T9" });
            Assert.Equal(expected, classifier.Classify(operation, returnCode, responseCode));
        }

        [Theory]
        [InlineData(TerminalTransport.Com, 8, null, null, true)]
        [InlineData(TerminalTransport.Com, 0, null, null, false)]
        [InlineData(TerminalTransport.Com, 256, null, null, false)]
        [InlineData(TerminalTransport.Tcp, 0, "192.168.1.25", "6100", true)]
        [InlineData(TerminalTransport.Tcp, 0, "192.168.1", "6100", false)]
        [InlineData(TerminalTransport.Tcp, 0, "::1", "6100", false)]
        [InlineData(TerminalTransport.Tcp, 0, "192.168.1.25", "0", false)]
        [InlineData(TerminalTransport.Tcp, 0, "192.168.1.25", "abc", false)]
        public void Connection_validation(TerminalTransport transport, int comPort, string ip, string port, bool valid)
        {
            var connection = new TerminalConnection { Transport = transport, ComPort = comPort, TcpIp = ip, TcpPort = port };
            Assert.Equal(valid, connection.Validate() == null);
        }

        [Fact]
        public void Mapping_permission_by_user_or_group()
        {
            var principal = new GenericPrincipal(new GenericIdentity(@"HOSPITAL\nurse1"), new[] { @"HOSPITAL\BillingSupervisors" });
            string reason;

            Assert.True(MappingAuthorization.IsAllowed(@"HOSPITAL\nurse1", principal, @"HOSPITAL\it.admin, HOSPITAL\NURSE1", out reason));
            Assert.True(MappingAuthorization.IsAllowed(@"HOSPITAL\nurse1", principal, @"HOSPITAL\BillingSupervisors", out reason));
            Assert.False(MappingAuthorization.IsAllowed(@"HOSPITAL\nurse1", principal, @"HOSPITAL\it.admin", out reason));
            Assert.NotNull(reason);

            // EN: empty list = Windows administrators only; a non-Windows principal is never an administrator.
            // AR: القائمة الفارغة = مسؤولو Windows فقط.
            Assert.False(MappingAuthorization.IsAllowed(@"HOSPITAL\nurse1", principal, "", out reason));
        }
    }
}
