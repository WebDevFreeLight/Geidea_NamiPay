# CDLL: mada Payment Terminal ECR Sample / مثال ربط نقاط البيع مع جهاز مدى

**EN:** A Windows Forms test app (C#, .NET Framework 4.8) showing how a cash register (ECR) talks to a **mada payment terminal** through the vendor's native DLLs `madaapi_v1_9.dll` and `madaapi_v1_8.dll`, over **USB/COM** or **TCP/IP**.

**AR:** تطبيق اختبار (Windows Forms بلغة C# على ‎.NET Framework 4.8) يوضّح كيف يتواصل نظام نقاط البيع (ECR) مع **جهاز الدفع مدى** عبر مكتبات المورّد `madaapi_v1_9.dll` و `madaapi_v1_8.dll`، من خلال **USB/COM** أو **TCP/IP**.

---

## Requirements / المتطلبات

| EN | AR |
|---|---|
| Windows + Visual Studio (or MSBuild) | نظام Windows مع Visual Studio (أو MSBuild) |
| .NET Framework 4.8 | ‎.NET Framework 4.8 |
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
| Cash payment | Records cash taken at reception (no terminal call) | يسجّل دفعاً نقدياً عند الاستقبال (بدون الجهاز) | - |
| Terminal mapping | Assign / move a terminal to a workstation (authorized users only) | ربط أو نقل جهاز إلى حاسوب (للمصرّح لهم فقط) | - |
| Sync pending | Insert the records saved as JSON while the database was down | إدراج السجلات المحفوظة كـ JSON أثناء تعطل القاعدة | - |

**EN:** Each button shows the outcome (Approved, Declined, Cancelled, Timeout, NoResponse, Failed, Error, Ok) and the return code in a message box, prints every field the terminal returned on the right, and **saves the result**. Fill **Patient MRN** / **Invoice no** to link the payment to the patient's bill.
**AR:** كل زر يعرض النتيجة ورمز الإرجاع في رسالة، ويطبع كل الحقول التي أعادها الجهاز على اليمين، **ويحفظ النتيجة**. املأ **رقم الملف** و**رقم الفاتورة** لربط الدفع بفاتورة المريض.

**EN:** Amounts are typed in SAR on the form and sent in **halalas**: 43.71 SAR -> `4371`.
**AR:** المبالغ تُكتب بالريال في النافذة وتُرسل **بالهللة**: 43.71 ريال -> `4371`.

## Payment services, storage and mapping / خدمات الدفع والتخزين والربط

**EN:** All logic lives in reusable services under `CDLL/Payments/`; Form1 only collects input and shows results.
**AR:** كل المنطق في خدمات قابلة لإعادة الاستخدام داخل `CDLL/Payments/`، و Form1 تجمع المدخلات وتعرض النتائج فقط.

| Part | EN | AR |
|---|---|---|
| `Terminal/IPaymentTerminalService` | One interface for every terminal operation. Runs the DLL on a background thread, one call at a time, with a timeout watchdog (`TerminalTimeoutSeconds`) | واجهة واحدة لكل عمليات الجهاز، على خيط خلفي، استدعاء واحد في كل مرة، مع مراقب للمهلة |
| `Storage/PaymentStorage` | Saves **every** result, error and mapping on one background thread | يحفظ **كل** نتيجة وخطأ وربط على خيط خلفي واحد |
| `Storage.Provider` | `Sqlite` (default, `%ProgramData%\CDLL\Payments\payments.db`), `SqlServer` (connection string `PaymentsSqlServer`) or `Json` | `Sqlite` (الافتراضي) أو `SqlServer` أو `Json` |
| Database down | The record is written to `<DataFolder>\pending\` as JSON; it is inserted automatically after the next successful save, or with **Sync pending** | يُحفظ السجل كـ JSON في `pending\`، ويُدرج تلقائياً بعد أول حفظ ناجح أو بزر **مزامنة المعلّق** |
| `<DataFolder>\logs\` | Daily activity log, one line per record and per database failure | سجل نشاط يومي، سطر لكل سجل ولكل فشل في القاعدة |
| `TerminalMappings` | Which terminal belongs to which PC; loaded at startup. `Mapping.AllowedUsersOrGroups` decides who may change it (empty = Windows administrators) | أي جهاز مرتبط بأي حاسوب، يُحمّل عند التشغيل. `Mapping.AllowedUsersOrGroups` يحدد من يمكنه تغييره |
| `Outcome.*ResponseCodes` | Host response codes for Approved / Cancelled / Timeout. **Fill the cancel and timeout codes from the vendor's ECR document** | رموز البنك للقبول والإلغاء وانتهاء المهلة. **املأ رموز الإلغاء والمهلة من وثيقة المورّد** |
| `TerminalMode` | `Real` or `Simulator` (Form1 with a fake terminal; everything is saved, no real payments) | `Real` أو `Simulator` (جهاز وهمي، يُحفظ كل شيء بدون مدفوعات حقيقية) |
| `Integration/IHisNotifier` | Hook to send saved payments to the HIS; `HisBillingRules.ShouldNotifyHis` decides which | نقطة ربط لإرسال المدفوعات إلى HIS، و `HisBillingRules.ShouldNotifyHis` يحدد أيها |

**EN:** SECURITY: card numbers are masked (first 6 + last 4) before they are shown, logged or saved; the expiry date is never stored; all SQL uses parameters; JSON is read without type information.
**AR:** الأمان: أرقام البطاقات تُقنّع (أول 6 + آخر 4) قبل العرض أو التسجيل أو الحفظ، وتاريخ الانتهاء لا يُحفظ أبداً، وكل استعلامات SQL تستخدم معاملات، وملفات JSON تُقرأ بدون معلومات أنواع.

## Tests / الاختبارات

**EN:** `CDLL.Tests` (xUnit) covers every outcome, every returned field, COM and TCP, SQLite, SQL Server (LocalDB) and JSON, the pending-JSON fallback and the background threads, using a fake terminal (nothing is charged). Build with Visual Studio's MSBuild, then run:
**AR:** مشروع `CDLL.Tests` يغطي كل النتائج وكل الحقول المعادة و COM و TCP و SQLite و SQL Server (LocalDB) و JSON والحفظ الاحتياطي والخيوط الخلفية، باستخدام جهاز وهمي (لا يُخصم أي مبلغ). ابنِ بـ MSBuild من Visual Studio ثم شغّل:

```powershell
msbuild CDLL.sln -restore -p:Configuration=Debug
sqllocaldb start MSSQLLocalDB
dotnet test CDLL.Tests --no-build
```

**EN:** Or use Test Explorer in Visual Studio. (`dotnet build` cannot build the old-style `CDLL.csproj` with its NuGet packages; use MSBuild / Visual Studio.)
**AR:** أو استخدم Test Explorer في Visual Studio. (`dotnet build` لا يستطيع بناء مشروع `CDLL.csproj` القديم مع حزم NuGet، استخدم MSBuild أو Visual Studio.)

## Logs / السجلات

**EN:** The DLL writes a daily log to `bin/Debug/ECRLOG_DDMMYYYY.txt` (e.g. `API_TERM_BUSY`, `communication timeout`). These logs can contain card data, so they are **git-ignored**. Never commit or share them.
**AR:** المكتبة تكتب سجلاً يومياً في `bin/Debug/ECRLOG_DDMMYYYY.txt` (مثل `API_TERM_BUSY` و `communication timeout`). هذه السجلات قد تحتوي بيانات بطاقات، لذلك هي **مستثناة من git**. لا ترفعها ولا تشاركها أبداً.

## More details / تفاصيل أكثر

**EN:** The top of `CDLL/Form1.cs` shows how the window, services and storage fit together; `CDLL/Payments/Terminal/MadaNativeMethods.cs` documents every DLL parameter, request string and output buffer, in English and Arabic.
**AR:** أعلى ملف `CDLL/Form1.cs` يوضح كيف تترابط النافذة والخدمات والتخزين، و `CDLL/Payments/Terminal/MadaNativeMethods.cs` يشرح كل معاملات المكتبة ونصوص الطلبات ومخازن المخرجات، بالعربية والإنجليزية.
