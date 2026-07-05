using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using TesisatTeklifApp.Infrastructure;
using TesisatTeklifApp.Infrastructure.Data;
using TesisatTeklifApp.Infrastructure.Identity;
using TesisatTeklifApp.Web.Components;
using TesisatTeklifApp.Web.Endpoints;
using TesisatTeklifApp.Web.Identity;

// Npgsql: uygulama yerel (Local/Unspecified) DateTime kullanıyor. PostgreSQL'de
// bunları "timestamp without time zone" olarak sakla (SQLite ile aynı davranış).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// --- Bulut (Render): PORT ortam değişkenine bağlan ---
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Ters proxy (Render HTTPS sonlandırır) arkasında doğru şema/cookie için.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// --- Infrastructure (DbContext + tüm servisler + QuestPDF lisans) ---
var logoPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "images", "logo.png");
builder.Services.AddInfrastructure(builder.Configuration, logoPath);

// --- Identity (cookie tabanlı, Blazor Server ile uyumlu) ---
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
}).AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    // Güvenli çerez: JS erişemez, HTTPS'te güvenli işaretlenir, CSRF için SameSite.
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Kaba kuvvet denemelerine karşı hesap kilitleme.
builder.Services.Configure<IdentityOptions>(o =>
{
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    o.Lockout.AllowedForNewUsers = true;
});

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// --- Blazor ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// --- Veritabanı oluştur + seed (roller, kullanıcılar, ürünler) ---
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
    await DbSeeder.SeedAsync(db, userManager, roleManager, app.Environment.ContentRootPath);
}

// Ters proxy başlıklarını (X-Forwarded-Proto vb.) en başta uygula → Render HTTPS'i doğru görülür.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Bulutta (PORT tanımlı) TLS'i proxy yapar; container içi HTTPS yönlendirmesi kapalı.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Identity yardımcı endpoint'leri (logout) ve dosya indirme (PDF/Excel).
app.MapAccountEndpoints();
app.MapDownloadEndpoints();

// Sağlık kontrolü (bulut/Render için; kimlik gerektirmez).
app.MapGet("/health", () => Results.Ok(new { ok = true })).AllowAnonymous();

app.Run();
