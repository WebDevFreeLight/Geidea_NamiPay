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
2. **EN:** Press **F5** to build and run.
   **AR:** اضغط **F5** للبناء والتشغيل.
3. **EN:** In the window, set **Connection settings**: the COM port (from *Device Manager > Ports (COM & LPT)*), serial settings, the terminal IP and TCP port. Set **Transaction values**: amounts, original RRN and date. Everything is editable; nothing is hard-coded.
   **AR:** في النافذة اضبط **إعدادات الاتصال**: منفذ COM (من *إدارة الأجهزة > المنافذ (COM و LPT)*)، إعدادات المنفذ التسلسلي، عنوان IP للجهاز ومنفذ TCP. واضبط **قيم العملية**: المبالغ والرقم المرجعي والتاريخ الأصليين. كل شيء قابل للتعديل، ولا توجد قيم مكتوبة مباشرة.
4. **EN:** The starting values come from `CDLL/app.config`. **Save defaults** writes the current values to `CDLL.exe.config`. Rebuilding copies `app.config` over it again, so put permanent defaults in `app.config`.
   **AR:** القيم الأولية تأتي من `CDLL/app.config`. زر **حفظ الافتراضي** يكتب القيم الحالية في `CDLL.exe.config`، وإعادة البناء تنسخ `app.config` فوقه مرة أخرى، لذلك ضع القيم الدائمة في `app.config`.

## Buttons / الأزرار

| Button | EN | AR | Link / الاتصال |
|---|---|---|---|
| Check status (USB) / (TCP) | Is the terminal ready? | هل الجهاز جاهز؟ | COM / TCP |
| Purchase (USB) / (TCP) | Purchase of the "Purchase amount" | شراء بقيمة "مبلغ الشراء" | COM / TCP |
| Refund (USB) / (TCP) | Refund an earlier purchase (needs original RRN + date) | استرجاع عملية سابقة (يحتاج الرقم المرجعي والتاريخ الأصلي) | COM / TCP |
| Repeat last (USB) / (TCP) | Get the last transaction's result again | جلب نتيجة آخر عملية مرة أخرى | COM / TCP |
| Reconciliation (USB) / (TCP) | End-of-day reconciliation | التسوية / إقفال نهاية اليوم | COM / TCP |
| Merchant info (USB) | Merchant name/address (EN + AR) | اسم التاجر وعنوانه (عربي + إنجليزي) | COM |
| Terminal ID (TCP) | Terminal ID | رقم الجهاز | TCP |

**EN:** Each button shows the DLL's return code in a message box, then prints the result fields in the text box on the right.
**AR:** كل زر يعرض رمز الإرجاع من المكتبة في رسالة، ثم يطبع حقول النتيجة في مربع النص على اليمين.

**EN:** Amounts are typed in SAR on the form and sent in **halalas**: 43.71 SAR -> `4371`.
**AR:** المبالغ تُكتب بالريال في النافذة وتُرسل **بالهللة**: 43.71 ريال -> `4371`.

## Logs / السجلات

**EN:** The DLL writes a daily log to `bin/Debug/ECRLOG_DDMMYYYY.txt` (e.g. `API_TERM_BUSY`, `communication timeout`). These logs can contain card data, so they are **git-ignored**. Never commit or share them.
**AR:** المكتبة تكتب سجلاً يومياً في `bin/Debug/ECRLOG_DDMMYYYY.txt` (مثل `API_TERM_BUSY` و `communication timeout`). هذه السجلات قد تحتوي بيانات بطاقات، لذلك هي **مستثناة من git**. لا ترفعها ولا تشاركها أبداً.

## More details / تفاصيل أكثر

**EN:** The top of `CDLL/Form1.cs` documents every DLL parameter, request string, output buffer and known issue, in English and Arabic.
**AR:** أعلى ملف `CDLL/Form1.cs` يشرح كل معاملات المكتبة ونصوص الطلبات ومخازن المخرجات والمشاكل المعروفة، بالعربية والإنجليزية.
