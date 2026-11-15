# CDLL: mada Payment Terminal ECR Sample / مثال ربط نقاط البيع مع جهاز مدى

**EN:** A Windows Forms test app (C#, .NET Framework 4.8) showing how a cash register (ECR) talks to a **mada payment terminal** through the vendor's native DLLs `madaapi_v1_9.dll` and `madaapi_v1_8.dll`, over **USB/COM** or **TCP/IP**.

**AR:** تطبيق اختبار (Windows Forms بلغة C# على ‎.NET Framework 4.8) يوضّح كيف يتواصل نظام نقاط البيع (ECR) مع **جهاز الدفع مدى** عبر مكتبات المورّد `madaapi_v1_9.dll` و `madaapi_v1_8.dll`، من خلال **USB/COM** أو **TCP/IP**.

---

## Requirements / المتطلبات

| EN | AR |
|---|---|
| Windows + Visual Studio (or MSBuild) | نظام Windows مع Visual Studio (أو MSBuild) |
| .NET Framework 4.8 | ‎.NET Framework 4.8 |
| `madaapi_v1_9.dll` and `madaapi_v1_8.dll` (kept in `Vendor/Geidea/`, copied to `bin\<Config>\libs\` by the build) | `madaapi_v1_9.dll` و `madaapi_v1_8.dll` (في `Vendor/Geidea/` وتُنسخ إلى `bin\<Config>\libs\` عند البناء) |
| A mada terminal connected by USB (shows as a COM port) or on the same network | جهاز مدى متصل عبر USB (يظهر كمنفذ COM) أو على نفس الشبكة |

> **EN:** Debug and Release both work: every build copies all DLLs into its own `libs` folder (see below).
> **AR:** إعداد Debug و Release يعملان: كل بناء ينسخ كل المكتبات إلى مجلد `libs` الخاص به (انظر أدناه).

## Libraries folder / مجلد المكتبات

**EN:** All libraries live in ONE sub-folder, not next to the .exe, so DLLs of other programs or old builds can never conflict:
**AR:** كل المكتبات في مجلد فرعي واحد وليس بجانب البرنامج، حتى لا تتعارض مكتبات برامج أخرى أو نسخ قديمة:

```
bin\Debug\CDLL.exe                 (CDLL40.exe for the .NET 4.0 build)
bin\Debug\CDLL.exe.config
bin\Debug\libs\*.dll              NuGet DLLs, madaapi_v1_8/9.dll (Geidea), ECR.dll (NamiPay)
bin\Debug\libs\runtimes\ x86\ x64\  native SQLite
```

| Part | EN | AR |
|---|---|---|
| Build | `build\Libraries.targets` (imported by both apps) moves every DLL into `libs` after each build, including old DLLs left next to the .exe; Clean / Rebuild empties it. Other folder name: `msbuild /p:LibrariesFolder=...` | ينقل كل المكتبات إلى `libs` بعد كل بناء، ومنها القديمة بجانب البرنامج |
| .NET DLLs | `<probing privatePath="libs"/>` in `app.config`, plus `LibraryLoader` (`AssemblyResolve`) for anything else | عبر `probing` في `app.config` ثم `LibraryLoader` |
| Native DLLs | `LibraryLoader` calls `SetDllDirectory(libs)` first thing in `Main`, so `[DllImport]` finds madaapi, ECR.dll and SQLite there | `SetDllDirectory` في بداية `Main` حتى تجد `[DllImport]` المكتبات |
| `Libraries.Folder` | `app.config` setting: `libs` (default), another sub-folder, a full path or `%ProgramData%\CDLL\libs` (shared by several apps). If it is outside the .exe folder, `LibraryLoader` loads from it | إعداد في `app.config`: `libs` أو مسار كامل أو مجلد مشترك |
| Deploy | Copy the .exe, its .config and the whole `libs` folder | انسخ البرنامج وملف الإعدادات ومجلد `libs` كاملاً |

## Setup / الإعداد

1. **EN:** Open `CDLL.sln` in Visual Studio.
   **AR:** افتح `CDLL.sln` في Visual Studio.
2. **EN:** Press **F5** to build and run.
   **AR:** اضغط **F5** للبناء والتشغيل.
3. **EN:** In the window, set **Connection settings**: the COM port (drop-down with the *Device Manager* names; the PAX terminal is marked `[terminal]`, ↻ reloads the list after plugging it in), serial settings, the terminal IP and TCP port. Set **Transaction values**: amounts, original RRN and date. Everything is editable; nothing is hard-coded.
   **AR:** في النافذة اضبط **إعدادات الاتصال**: منفذ COM (قائمة منسدلة بأسماء *إدارة الأجهزة*، وجهاز PAX مُعلَّم `[terminal]`، وزر ↻ يحدّث القائمة بعد توصيله)، إعدادات المنفذ التسلسلي، عنوان IP للجهاز ومنفذ TCP. واضبط **قيم العملية**: المبالغ والرقم المرجعي والتاريخ الأصليين. كل شيء قابل للتعديل، ولا توجد قيم مكتوبة مباشرة.
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
| **Provider** (Geidea / NamiPay) | `ProviderTerminalService` sends each call to the terminal's company: **Geidea** = `MadaTerminalService` (madaapi DLLs), **NamiPay** = `SkybandTerminalService` (SkyBand `ECR.dll` from `NamiPay\CoreSkybandECR`, COM or TCP). Same pipeline, storage and audit for both; every row has a `Provider` column | `ProviderTerminalService` يرسل كل استدعاء إلى شركة الجهاز: **Geidea** أو **NamiPay**، بنفس المسار والتخزين والتدقيق، وكل سجل فيه عمود `Provider` |
| NamiPay specifics | Needs the 8-digit **cash register** (mapping or `NamiPay.CashRegisterNumber`). Register (terminal ID) and Start session run automatically; the ECR reference counter and terminal ID are kept in `<DataFolder>\namipay\state.json`. Merchant info is not offered by NamiPay; Terminal ID = Register | يحتاج **رقم الصندوق** (8 أرقام). التسجيل وبدء الجلسة تلقائيان، وعدّاد المرجع ورقم الجهاز في `namipay\state.json` |
| Mapping **Detect** | Tries Geidea Check status, then NamiPay Check status, and fills Provider with the one that answered | يجرّب فحص حالة Geidea ثم NamiPay ويملأ المزوّد بالذي رد |
| `Storage/PaymentStorage` | Saves **every** result, error and mapping on one background thread | يحفظ **كل** نتيجة وخطأ وربط على خيط خلفي واحد |
| `Storage.Provider` | `Sqlite` (default, `%ProgramData%\CDLL\Payments\payments.db`), `SqlServer` (connection string `PaymentsSqlServer`) or `Json` | `Sqlite` (الافتراضي) أو `SqlServer` أو `Json` |
| Database down | The record is written to `<DataFolder>\pending\` as JSON; it is inserted automatically after the next successful save, or with **Sync pending** | يُحفظ السجل كـ JSON في `pending\`، ويُدرج تلقائياً بعد أول حفظ ناجح أو بزر **مزامنة المعلّق** |
| `<DataFolder>\logs\` | Daily activity log, one line per record and per database failure | سجل نشاط يومي، سطر لكل سجل ولكل فشل في القاعدة |
| `WorkstationTerminals` | Which terminal belongs to which PC. A PC can keep **one Geidea and one NamiPay** mapping (each with its own port and cash register); exactly **one is active** (the default Form1 loads at startup, provider box locked). The PC's first mapping becomes active; **Set active / default** on the mapping screen switches it; removing the active one activates the other. The old `TerminalMappings` table (one row per PC) is copied once as each PC's active mapping. `Mapping.AllowedUsersOrGroups` decides who may change it (empty = Windows administrators) | أي جهاز مرتبط بأي حاسوب. الحاسوب يحتفظ **بربط Geidea وربط NamiPay**، و**واحد فقط فعّال** (الافتراضي الذي تحمّله Form1). أول ربط يصبح فعّالاً، وزر **تعيين كافتراضي** يغيّره، وحذف الفعّال يفعّل الآخر. جدول `TerminalMappings` القديم يُنسخ مرة واحدة كربط فعّال. `Mapping.AllowedUsersOrGroups` يحدد من يمكنه تغييره |
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

## .NET Framework 4.0 build / نسخة ‎.NET Framework 4.0

**EN:** `CDLL.Net40` is the same application (same screens, Geidea + NamiPay, storage, mapping, audit) built for **.NET Framework 4.0**, for PCs that cannot install 4.5+. It builds `CDLL40.exe`; namespaces, `app.config` keys and the database format are unchanged, so both versions can share the same `payments.db` / SQL Server database. `CDLL.Net40.Tests` runs the same 151 tests against it.
**AR:** `CDLL.Net40` هو نفس التطبيق (نفس الشاشات و Geidea و NamiPay والتخزين والربط والتدقيق) مبنياً على **‎.NET Framework 4.0** للأجهزة التي لا يمكن تثبيت 4.5+ عليها. الناتج `CDLL40.exe`، وأسماء النطاقات ومفاتيح `app.config` وصيغة القاعدة كما هي، فيمكن للنسختين مشاركة نفس القاعدة. ومشروع `CDLL.Net40.Tests` يشغّل نفس الاختبارات الـ151 عليه.

| Difference | EN | AR |
|---|---|---|
| async / await | `Microsoft.Bcl.Async` (`TaskEx.Run`, `TaskEx.Delay`, `TaskEx.WhenAny`, `TaskEx.FromResult`); binding redirects for `System.Runtime` / `System.Threading.Tasks` 2.6.8 are in `app.config` | من `Microsoft.Bcl.Async`، وتوجيهات الإصدارات موجودة في `app.config` |
| SQLite | `System.Data.SQLite` (Microsoft.Data.Sqlite needs 4.6.1+); native `x86\SQLite.Interop.dll` is copied to the output | `System.Data.SQLite` مع المكتبة الأصلية `x86\SQLite.Interop.dll` |
| Build | 4.0 reference assemblies come from NuGet (`Microsoft.NETFramework.ReferenceAssemblies`), so no targeting pack is needed; any API newer than 4.0 fails to compile | مكتبات المرجع لـ 4.0 من NuGet، وأي واجهة أحدث من 4.0 تفشل في البناء |
| Deploy | Copy `CDLL40.exe`, `CDLL40.exe.config` and the whole `libs` folder (it holds `x86\SQLite.Interop.dll`, the `Microsoft.Threading.Tasks*`, `System.Runtime`, `System.Threading.Tasks`, `System.IO` DLLs, `ECR.dll` and `madaapi_v1_*.dll`) | انسخ البرنامج وملف الإعدادات ومجلد `libs` كاملاً |

> **EN:** The two projects are separate copies: a fix in `CDLL` must also be made in `CDLL.Net40` (and the tests in both test projects).
> **AR:** المشروعان نسختان منفصلتان: أي إصلاح في `CDLL` يجب تطبيقه أيضاً في `CDLL.Net40` (والاختبارات في المشروعين).

## Logs / السجلات

**EN:** The DLL writes a daily log to `bin/Debug/ECRLOG_DDMMYYYY.txt` (e.g. `API_TERM_BUSY`, `communication timeout`). These logs can contain card data, so they are **git-ignored**. Never commit or share them.
**AR:** المكتبة تكتب سجلاً يومياً في `bin/Debug/ECRLOG_DDMMYYYY.txt` (مثل `API_TERM_BUSY` و `communication timeout`). هذه السجلات قد تحتوي بيانات بطاقات، لذلك هي **مستثناة من git**. لا ترفعها ولا تشاركها أبداً.

## More details / تفاصيل أكثر

**EN:** The top of `CDLL/Form1.cs` shows how the window, services and storage fit together; `CDLL/Payments/Terminal/MadaNativeMethods.cs` documents every DLL parameter, request string and output buffer, in English and Arabic.
**AR:** أعلى ملف `CDLL/Form1.cs` يوضح كيف تترابط النافذة والخدمات والتخزين، و `CDLL/Payments/Terminal/MadaNativeMethods.cs` يشرح كل معاملات المكتبة ونصوص الطلبات ومخازن المخرجات، بالعربية والإنجليزية.
