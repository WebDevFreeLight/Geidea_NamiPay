# NewImplementations: mada Terminal API (.NET 8) + Windows Forms (.NET 8)

**EN:** A modern rewrite of the old `CDLL` sample, split into two parts. A **.NET 8 Web API** owns the mada DLLs and the terminal. A **.NET 8 Windows Forms** app calls that API over HTTP.

**AR:** إعادة كتابة حديثة لمثال `CDLL` القديم، مقسّمة إلى جزأين. **خدمة Web API على ‎.NET 8** تتولى مكتبات مدى والجهاز، و**تطبيق Windows Forms على ‎.NET 8** يستدعي هذه الخدمة عبر HTTP.

```
┌──────────────────────┐   HTTP/JSON    ┌──────────────────────────┐  P/Invoke   ┌──────────────────┐  USB/COM or TCP  ┌──────────┐
│ Mada.WinForms        │ ─────────────▶ │ Mada.Api  (32-bit x86)   │ ──────────▶ │ madaapi_v1_9.dll │ ───────────────▶ │ mada POS │
│ .NET 8, any CPU      │ ◀───────────── │ .NET 8 minimal API       │             │ madaapi_v1_8.dll │                  │ terminal │
└──────────────────────┘                └──────────────────────────┘             └──────────────────┘                  └──────────┘
            └──────────── both reference Mada.Contracts (shared request/response types) ────────────┘
```

| Project | EN | AR |
|---|---|---|
| `Mada.Contracts` | Shared request/response records | أنواع الطلبات والاستجابات المشتركة |
| `Mada.Api` | .NET 8 API, **must run 32-bit** (the vendor DLLs are x86) | خدمة ‎.NET 8، **يجب تشغيلها بنظام 32-بت** (مكتبات المورّد x86) |
| `Mada.WinForms` | .NET 8 desktop client, calls the API asynchronously | واجهة سطح مكتب ‎.NET 8 تستدعي الخدمة بشكل غير متزامن |

## What improved vs. the old sample / ما الذي تحسّن مقارنة بالمثال القديم

| Old `CDLL` | New / الجديد |
|---|---|
| Ports and IPs hard-coded per button | All in `Mada.Api/appsettings.json` → `Terminal` section / كلها في قسم `Terminal` |
| Window freezes while the terminal waits | `async` HTTP calls, the window stays responsive / النافذة تبقى مستجيبة |
| Two clicks could hit the terminal at once | One call at a time, a second one gets **HTTP 409** / استدعاء واحد في كل مرة، والثاني يحصل على 409 |
| Output text had trailing `\0` chars | Fields cut at the first `\0` and trimmed / الحقول نظيفة |
| No input validation | Amount (max 2 decimals), 12-digit RRN, date sent as Gregorian / تحقق من المبلغ والمرجع والتاريخ الميلادي |
| Needs hardware to try anything | **Simulator mode**, no DLL or terminal needed / **وضع المحاكاة** بدون جهاز |

## Run it / التشغيل

**EN:** Requirements: .NET 8 SDK, plus the **x86** .NET 8 / ASP.NET Core runtime (installed in `C:\Program Files (x86)\dotnet`).
**AR:** المتطلبات: ‎.NET 8 SDK مع بيئة تشغيل ‎.NET 8 / ASP.NET Core **x86** (مثبّتة في `C:\Program Files (x86)\dotnet`).

### 1. Start the API / تشغيل الخدمة

```powershell
cd NewImplementations
dotnet run --project Mada.Api --launch-profile "Mada.Api (simulator)"        # no hardware / بدون جهاز
dotnet run --project Mada.Api --launch-profile "Mada.Api (real terminal)"    # real DLL + terminal / الجهاز الفعلي
```

**EN:** Swagger UI: http://localhost:5080/swagger. You can also use `Mada.Api/Mada.Api.http` in Visual Studio. In Visual Studio, set **multiple startup projects** (Mada.Api + Mada.WinForms) and press F5.
**AR:** واجهة Swagger على http://localhost:5080/swagger، ويمكن أيضاً استخدام الملف `Mada.Api/Mada.Api.http`. في Visual Studio اختر **عدة مشاريع بدء** (Mada.Api و Mada.WinForms) ثم اضغط F5.

### 2. Start the Windows app / تشغيل تطبيق Windows

```powershell
dotnet run --project Mada.WinForms
```

**EN:** Choose the connection (Tcp/Com) and press a button. The JSON result appears on the right.
**AR:** اختر نوع الاتصال (Tcp/Com) واضغط أي زر، وستظهر نتيجة JSON على اليمين.

## Configuration / الإعدادات (`Mada.Api/appsettings.json`)

| Key | EN | AR |
|---|---|---|
| `Terminal:UseSimulator` | `true` = fake terminal (on in `appsettings.Development.json`) | `true` = جهاز وهمي (مفعّل في بيئة التطوير) |
| `Terminal:DefaultTransport` | `Tcp` or `Com`, used when a request does not specify one | `Tcp` أو `Com` عند عدم تحديده في الطلب |
| `Terminal:Com:Port` | COM port number (5 = COM5) | رقم منفذ COM |
| `Terminal:Tcp:Ip` / `Port` | Terminal network address | عنوان الجهاز على الشبكة |
| `Terminal:IncludeRawResponse` | Return the full raw response. **Off by default**: it can contain card data | إرجاع الاستجابة الخام. **معطّل افتراضياً** لأنها قد تحتوي بيانات بطاقة |
| `Urls` | Listen address, `http://localhost:5080` (this PC only) | عنوان الاستماع (هذا الجهاز فقط) |

**EN:** Override any key with an environment variable, e.g. `Terminal__Com__Port=9`.
**AR:** يمكن تجاوز أي إعداد بمتغير بيئة، مثل `Terminal__Com__Port=9`.

## Endpoints / نقاط الخدمة

| Method & route | Old button | EN | AR |
|---|---|---|---|
| `GET /api/terminal/status?transport=Tcp\|Com` | Check status / TCP_Checkstatus | Terminal ready? | فحص الحالة |
| `GET /api/terminal/id` | Terminal ID | Terminal ID (TCP only) | رقم الجهاز (TCP فقط) |
| `GET /api/terminal/merchant` | Get Merchant | Merchant name/address (COM only) | بيانات التاجر (COM فقط) |
| `POST /api/transactions/purchase` `{amount, transport?}` | Purchase_USB / _TCP | Purchase | شراء |
| `POST /api/transactions/refund` `{amount, originalRrn, originalDate, transport?}` | Refund_USB / _TCP | Refund | استرجاع |
| `POST /api/transactions/repeat` `{transport?}` | Repeat_USB / _TCP | Last transaction result again | إعادة نتيجة آخر عملية |
| `POST /api/reconciliation` `{transport?}` | Recon_USB / _TCP | End-of-day settlement | التسوية |

**EN:** Errors use `application/problem+json`: **400** invalid input, **409** terminal busy, **503** DLL not loadable (missing, or the process is 64-bit).
**AR:** الأخطاء بصيغة `application/problem+json`: **400** مدخلات غير صحيحة، **409** الجهاز مشغول، **503** تعذر تحميل المكتبة (مفقودة أو العملية بنظام 64-بت).

## Important notes / ملاحظات مهمة

- **EN:** If the client times out or disconnects during a purchase, the terminal may still have charged the card. Call **repeat** before retrying.
  **AR:** إذا انتهت مهلة العميل أو انقطع الاتصال أثناء الشراء فقد يكون الجهاز خصم المبلغ فعلاً. استدعِ **repeat** قبل إعادة المحاولة.
- **EN:** The API listens on `localhost` only and has **no authentication**. Before exposing it on a network, add authentication and HTTPS.
  **AR:** الخدمة تستمع على `localhost` فقط و**بدون مصادقة**. قبل إتاحتها على الشبكة أضف المصادقة و HTTPS.
- **EN:** The trailing `;0;1` / `;1;1` flags in purchase/refund requests are copied from the old sample. Their meaning is unconfirmed, so check the vendor spec.
  **AR:** العلامات `;0;1` و `;1;1` في طلبات الشراء والاسترجاع منسوخة من المثال القديم ومعناها غير مؤكد، فراجع وثيقة المورّد.
- **EN:** `isApproved` = response code `000`. Confirm the full code list with the vendor.
  **AR:** `isApproved` = رمز الاستجابة `000`. تأكد من قائمة الرموز الكاملة من المورّد.
- **EN:** The DLLs are linked from `../CDLL/bin/Debug/`, so keep that folder.
  **AR:** المكتبات مرتبطة من `../CDLL/bin/Debug/`، فلا تحذف هذا المجلد.
