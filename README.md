# CDLL: mada Payment Terminal ECR Sample / مثال ربط نقاط البيع مع جهاز مدى

**EN:** A Windows Forms test app (C#, .NET Framework 4.6.1) showing how a cash register (ECR) talks to a **mada payment terminal** through the vendor's native DLLs `madaapi_v1_9.dll` and `madaapi_v1_8.dll`, over **USB/COM** or **TCP/IP**.

**AR:** تطبيق اختبار (Windows Forms بلغة C# على ‎.NET Framework 4.6.1) يوضّح كيف يتواصل نظام نقاط البيع (ECR) مع **جهاز الدفع مدى** عبر مكتبات المورّد `madaapi_v1_9.dll` و `madaapi_v1_8.dll`، من خلال **USB/COM** أو **TCP/IP**.

---

## Requirements / المتطلبات

| EN | AR |
|---|---|
| Windows + Visual Studio (or MSBuild) | نظام Windows مع Visual Studio (أو MSBuild) |
| .NET Framework 4.6.1+ | ‎.NET Framework 4.6.1 أو أحدث |
| `madaapi_v1_9.dll` and `madaapi_v1_8.dll` next to `CDLL.exe` (already in `CDLL/bin/Debug/`) | وجود `madaapi_v1_9.dll` و `madaapi_v1_8.dll` بجانب `CDLL.exe` (موجودتان في `CDLL/bin/Debug/`) |
| A mada terminal connected by USB (shows as a COM port) or on the same network | جهاز مدى متصل عبر USB (يظهر كمنفذ COM) أو على نفس الشبكة |

> **EN:** Build the **Debug** configuration. The DLLs are only in `bin/Debug`, so for Release copy them into `bin/Release` too.
> **AR:** ابنِ إعداد **Debug**. المكتبات موجودة فقط في `bin/Debug`، لذلك عند استخدام Release انسخها إلى `bin/Release` أيضاً.

## Setup / الإعداد

1. **EN:** Open `CDLL.sln` in Visual Studio.
   **AR:** افتح `CDLL.sln` في Visual Studio.
2. **EN:** USB: find the terminal's COM port in *Device Manager > Ports (COM & LPT)*. Put it in `CDLL/app.config` → `PaymentMachinePort`, **and** update the hard-coded port numbers in `Form1.cs` (5, 9, 15, 17).
   **AR:** USB: اعرف رقم منفذ COM للجهاز من *إدارة الأجهزة > المنافذ (COM و LPT)*. ضعه في `CDLL/app.config` ← `PaymentMachinePort`، **و** عدّل أرقام المنافذ المكتوبة مباشرة في `Form1.cs` (5، 9، 15، 17).
3. **EN:** TCP: replace the hard-coded IPs in `Form1.cs` with your terminal's IP (port is `6100`).
   **AR:** TCP: استبدل عناوين IP المكتوبة في `Form1.cs` بعنوان جهازك (المنفذ `6100`).
4. **EN:** Press **F5** to build and run.
   **AR:** اضغط **F5** للبناء والتشغيل.

## Buttons / الأزرار

| Button | EN | AR | Link / الاتصال |
|---|---|---|---|
| Check status | Is the terminal ready? | هل الجهاز جاهز؟ | COM (port from `app.config`) |
| TCP_Checkstatus | Is the terminal ready? | هل الجهاز جاهز؟ | TCP |
| Purchase_USB / Purchase_TCP | Purchase (test amount) | عملية شراء (مبلغ تجريبي) | COM / TCP |
| Refund_USB / Refund_TCP | Refund an earlier purchase (needs original RRN + date) | استرجاع عملية سابقة (يحتاج الرقم المرجعي والتاريخ الأصلي) | COM / TCP |
| Repeat_USB / Repeat_TCP | Get the last transaction's result again | جلب نتيجة آخر عملية مرة أخرى | COM / TCP |
| Recon_USB / Recon_TCP | End-of-day reconciliation | التسوية / إقفال نهاية اليوم | COM / TCP |
| Get Merchant | Merchant name/address (EN + AR) | اسم التاجر وعنوانه (عربي + إنجليزي) | COM |
| Terminal ID | Terminal ID | رقم الجهاز | TCP |

**EN:** Each button shows the DLL's return code in a message box, then prints the result fields in the text box on the right.
**AR:** كل زر يعرض رمز الإرجاع من المكتبة في رسالة، ثم يطبع حقول النتيجة في مربع النص على اليمين.

**EN:** Amounts are in **halalas**: `4371` = 43.71 SAR.
**AR:** المبالغ **بالهللة**: `4371` = 43.71 ريال.

## Logs / السجلات

**EN:** The DLL writes a daily log to `bin/Debug/ECRLOG_DDMMYYYY.txt` (e.g. `API_TERM_BUSY`, `communication timeout`). These logs can contain card data, so they are **git-ignored**. Never commit or share them.
**AR:** المكتبة تكتب سجلاً يومياً في `bin/Debug/ECRLOG_DDMMYYYY.txt` (مثل `API_TERM_BUSY` و `communication timeout`). هذه السجلات قد تحتوي بيانات بطاقات، لذلك هي **مستثناة من git**. لا ترفعها ولا تشاركها أبداً.

## More details / تفاصيل أكثر

**EN:** The top of `CDLL/Form1.cs` documents every DLL parameter, request string, output buffer and known issue, in English and Arabic.
**AR:** أعلى ملف `CDLL/Form1.cs` يشرح كل معاملات المكتبة ونصوص الطلبات ومخازن المخرجات والمشاكل المعروفة، بالعربية والإنجليزية.
