// =====================================================================================
// EN: Test data: a transaction with EVERY field filled, one per outcome, mappings and errors.
// AR: بيانات الاختبار: عملية بكل الحقول، عملية لكل نتيجة، ربطات وأخطاء.
// =====================================================================================

using System;
using System.IO;
using CDLL.Payments;

namespace CDLL.Tests.Storage
{
    internal static class TestRecords
    {
        /// <summary>EN: Millisecond precision, the same as SQL Server DATETIME2(3).  AR: دقة الملي ثانية مثل SQL Server.</summary>
        public static DateTime UtcMs(int year, int month, int day, int hour, int minute, int second, int ms)
        {
            return new DateTime(year, month, day, hour, minute, second, ms, DateTimeKind.Utc);
        }

        public static PaymentTransactionRecord Full(TransactionOutcome outcome, DateTime createdAtUtc)
        {
            return new PaymentTransactionRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = createdAtUtc,
                DurationMs = 1234,
                Workstation = "CASHIER-01",
                WindowsUser = @"HOSPITAL\cashier",
                TerminalName = "SPAN-ER-01",
                Department = "Emergency",
                PatientMrn = "MRN000123",
                InvoiceNo = "INV-2026-0001",
                PaymentMethod = PaymentMethod.Card,
                Operation = TerminalOperation.Purchase,
                Transport = TerminalTransport.Com,
                ConnectionTarget = "COM8 38400 baud, parity 0, 8 data bits, stop bits 0",
                Request = "1;0;1!",
                AmountHalalas = 1,
                OriginalRrn = "434615455456",
                OriginalDate = "2024-12-11",
                ReturnCode = 0,
                Outcome = outcome,
                ErrorMessage = outcome == TransactionOutcome.Error ? "DllNotFoundException: madaapi_v1_9.dll" : null,
                ResponseCode = "000",
                MaskedPan = "446404******1234",
                SchemeId = "P1",
                CardScheme = "MADA",
                TerminalAmount = "000000000001",
                Stan = "000123",
                TerminalDateTime = "261110120000",
                Rrn = "631412345678",
                AuthCode = "A1B2C3",
                TerminalId = "1234567812345678",
                MerchantId = "123456789012345",
                AdditionalAmount = "000000000000",
                EcrRefNo = "ECR0000000000001",
                Version = "1.9",
                TerminalTotal = "PUR 001 000000000001",
                HostTotal = "PUR 001 000000000001",
                MerchantNameEn = "KING FAHAD HOSPITAL",
                MerchantNameAr = "مستشفى الملك فهد",
                MerchantAddressEn = "RIYADH",
                MerchantAddressAr = "الرياض",
                RawResponse = "RAW|446404******1234|000|APPROVED"
            };
        }

        public static TerminalMapping Mapping(string workstation, string terminal)
        {
            return new TerminalMapping
            {
                Workstation = workstation,
                TerminalName = terminal,
                Department = "Emergency",
                Transport = TerminalTransport.Com,
                ComPort = 8,
                BaudRate = 38400,
                DataBits = 8,
                TcpIp = "192.168.1.25",
                TcpPort = "6100",
                UpdatedAtUtc = UtcMs(2026, 11, 10, 9, 0, 0, 0),
                UpdatedBy = @"HOSPITAL\it.admin"
            };
        }

        public static string NewTempFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "CDLL.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
