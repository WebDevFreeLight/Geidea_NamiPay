// =====================================================================================
// EN: Mada.Api: .NET 8 minimal API that wraps the 32-bit mada terminal DLLs behind HTTP endpoints.
//     Any client (the new WinForms app, a web POS, Postman...) can now use the terminal without
//     loading native DLLs or caring about 32/64-bit.
// AR: Mada.Api: خدمة ‎.NET 8 (Minimal API) تغلّف مكتبات جهاز مدى (32-بت) خلف نقاط HTTP.
//     أي عميل (تطبيق WinForms الجديد، نقاط بيع ويب، Postman...) يستطيع الآن استخدام الجهاز بدون
//     تحميل المكتبات الأصلية أو الاهتمام بنظام 32/64-بت.
// =====================================================================================

using System.Text.Json.Serialization;
using Mada.Api.Endpoints;
using Mada.Api.Options;
using Mada.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// EN: Terminal settings from appsettings.json ("Terminal" section).   AR: إعدادات الجهاز من appsettings.json (قسم "Terminal").
builder.Services.Configure<TerminalOptions>(builder.Configuration.GetSection(TerminalOptions.SectionName));
var terminalOptions = builder.Configuration.GetSection(TerminalOptions.SectionName).Get<TerminalOptions>() ?? new TerminalOptions();

// EN: Real terminal or simulator. SINGLETON is required: the one-call-at-a-time gate lives inside it.
// AR: الجهاز الفعلي أو المحاكي. التسجيل كـ Singleton ضروري لأن بوابة "استدعاء واحد في كل مرة" موجودة داخله.
if (terminalOptions.UseSimulator)
    builder.Services.AddSingleton<IMadaTerminal, SimulatedMadaTerminal>();
else
    builder.Services.AddSingleton<IMadaTerminal, NativeMadaTerminal>();

// EN: Enums as strings in JSON ("Tcp"/"Com" instead of 0/1). The second line makes Swagger show them the same way.
// AR: القيم المعدودة كنصوص في JSON ("Tcp"/"Com" بدلاً من 0/1). السطر الثاني يجعل Swagger يعرضها بنفس الطريقة.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// EN: Exceptions -> problem+json with proper status codes (see TerminalExceptionHandler).
// AR: الاستثناءات -> problem+json برموز حالة مناسبة (راجع TerminalExceptionHandler).
builder.Services.AddExceptionHandler<TerminalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // EN: Browse to http://localhost:5080/swagger to try every endpoint.   AR: افتح http://localhost:5080/swagger لتجربة كل النقاط.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Logger.LogInformation("Mada.Api starting: mode={Mode}, defaultTransport={Transport}, process={Bits}-bit",
    terminalOptions.UseSimulator ? "SIMULATOR" : "REAL TERMINAL",
    terminalOptions.DefaultTransport,
    Environment.Is64BitProcess ? 64 : 32);

// EN: Fail loudly at startup instead of on the first payment.   AR: إظهار الخطأ عند التشغيل بدلاً من أول عملية دفع.
if (!terminalOptions.UseSimulator && Environment.Is64BitProcess)
    app.Logger.LogError("Running as 64-bit: the 32-bit mada DLLs cannot be loaded. Build/run the win-x86 configuration.");

app.MapMadaEndpoints();

app.Run();
