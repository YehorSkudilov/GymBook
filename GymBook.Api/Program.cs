using System.Threading.RateLimiting;
using GymBook.Api;
using GymBook.Api.Admin;
using GymBook.Api.Auth;
using GymBook.Api.Data;
using GymBook.Api.Plans;
using GymBook.Api.Swagger;
using GymBook.Api.Sync;
using GymBook.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Settings from .env next to the project, the same file docker-compose uses (see .env.example). Must run before the
// builder reads environment variables.
DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
jwt.Validate();
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(TimeProvider.System);

// Nothing legitimate is large except sync, which raises its own limit.
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 1024 * 1024);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddDbContext<ApiDbContext>(o => o.UseNpgsql(
    builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.")));

builder.Services.AddIdentityCore<AppUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        // The user name is the email, so allow every character an email can contain.
        o.User.AllowedUserNameCharacters = "";
        // Length over composition rules (NIST SP 800-63B); lockout and rate limits handle guessing.
        o.Password.RequiredLength = GymBook.Contracts.AuthLimits.MinPasswordLength;
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.AllowedForNewUsers = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        // Email verification, password reset and email change codes are six digits to type from an email, not links.
        // They're derived from the security stamp, so nothing is stored and a used code stops working once the change
        // is made.
        o.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
        o.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
        o.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
    })
    .AddSignInManager()
    .AddEntityFrameworkStores<ApiDbContext>()
    .AddTokenProvider<EmailTokenProvider<AppUser>>(TokenOptions.DefaultEmailProvider);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(builder.Configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions());
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddSingleton(builder.Configuration.GetSection("Google").Get<GoogleOptions>() ?? new GoogleOptions());
builder.Services.AddSingleton<GoogleTokenVerifier>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        // The live sync hub's WebSocket can't always carry an Authorization header: SignalR then sends the access
        // token as ?access_token=, accepted for the hub only.
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs") && context.Request.Query["access_token"] is { Count: > 0 } token)
                    context.Token = token.ToString();
                return Task.CompletedTask;
            },
        };
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.Key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = AuthPolicies.RoleClaim,
        };
    });

// Every endpoint requires a signed-in user with a verified email unless it opts out: [AllowAnonymous], or the account
// endpoints' AnyAccount policy, which is where the email gets verified (or corrected).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireClaim(AuthPolicies.EmailVerifiedClaim, "true").Build())
    .AddPolicy(AuthPolicies.AnyAccount, p => p.RequireAuthenticatedUser())
    // The admin app (see Admin/AdminRoles.cs).
    .AddPolicy(AuthPolicies.AdminAccess, p => p.RequireAuthenticatedUser().RequireClaim(AuthPolicies.EmailVerifiedClaim, "true")
        .RequireRole(AdminRoles.Admin, AdminRoles.SuperAdmin))
    .AddPolicy(AuthPolicies.SuperAdminOnly, p => p.RequireAuthenticatedUser().RequireClaim(AuthPolicies.EmailVerifiedClaim, "true")
        .RequireRole(AdminRoles.SuperAdmin));

var authPerMinute = builder.Configuration.GetValue("RateLimiting:AuthPerMinute", 10);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(RateLimits.Auth, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPerMinute, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy(RateLimits.Sync, ctx => RateLimitPartition.GetTokenBucketLimiter(
        ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new TokenBucketRateLimiterOptions { TokenLimit = 30, TokensPerPeriod = 30, ReplenishmentPeriod = TimeSpan.FromMinutes(1) }));
    // Food search: anyone may use it (no account needed), so per address; generous for typing as you search.
    o.AddPolicy(RateLimits.Foods, ctx => RateLimitPartition.GetTokenBucketLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new TokenBucketRateLimiterOptions { TokenLimit = 60, TokensPerPeriod = 60, ReplenishmentPeriod = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SyncProcessor>();
// Public profiles, ranks, friends and shared plans (see Social/).
builder.Services.AddScoped<GymBook.Api.Social.PlanShares>();
builder.Services.AddScoped<GymBook.Api.Social.RankCalculator>();
var openAi = builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>() ?? new OpenAiOptions();
builder.Services.AddSingleton(openAi);
builder.Services.AddHttpClient<OpenAiPlanGenerator>(c =>
{
    c.BaseAddress = new Uri(openAi.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(openAi.TimeoutSeconds);
});
// Food databases that need a key (see Foods/FoodDatabases.cs); each is left out of the search while its key is unset.
builder.Services.AddSingleton(builder.Configuration.GetSection("Usda").Get<GymBook.Api.Foods.UsdaOptions>() ?? new GymBook.Api.Foods.UsdaOptions());
builder.Services.AddSingleton(builder.Configuration.GetSection("FatSecret").Get<GymBook.Api.Foods.FatSecretOptions>() ?? new GymBook.Api.Foods.FatSecretOptions());
builder.Services.AddHttpClient<GymBook.Api.Foods.FoodDatabases>(c => c.Timeout = TimeSpan.FromSeconds(10));
// Fetches links to import plans from; the handler keeps it off private networks.
builder.Services.AddHttpClient<LinkFetcher>(c => c.Timeout = TimeSpan.FromSeconds(15))
    .ConfigurePrimaryHttpMessageHandler(LinkFetcher.CreateHandler);
// Each AI plan and plan chat message is a paid OpenAI call: a limit per user in any window, set in code
// (PlanQuotaSettings.Default) and counted in the database so it survives restarts.
builder.Services.AddSingleton(PlanQuotaSettings.Default);
builder.Services.AddScoped<PlanQuota>();
// Gym Book Pro: store purchases checked with Google Play, the App Store and the Microsoft Store (see Billing/). Each
// store is left out while its settings are unset. Singletons, so their store tokens are kept between requests.
builder.Services.AddSingleton(builder.Configuration.GetSection("GooglePlay").Get<GymBook.Api.Billing.GooglePlayOptions>() ?? new GymBook.Api.Billing.GooglePlayOptions());
builder.Services.AddSingleton(builder.Configuration.GetSection("AppStore").Get<GymBook.Api.Billing.AppStoreOptions>() ?? new GymBook.Api.Billing.AppStoreOptions());
builder.Services.AddSingleton(builder.Configuration.GetSection("MicrosoftStore").Get<GymBook.Api.Billing.MicrosoftStoreOptions>() ?? new GymBook.Api.Billing.MicrosoftStoreOptions());
var billingHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<GymBook.Api.Billing.GooglePlayVerifier>(sp, billingHttp));
builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<GymBook.Api.Billing.AppStoreVerifier>(sp, billingHttp));
builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<GymBook.Api.Billing.MicrosoftStoreVerifier>(sp, billingHttp));
builder.Services.AddScoped<GymBook.Api.Billing.Subscriptions>();
builder.Services.AddHostedService<GymBook.Api.Billing.SubscriptionRefresher>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers(o =>
    {
        // Otherwise every non-nullable string becomes [Required], which rejects legitimately empty values.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        o.MaxModelValidationErrors = 50;
    })
    .AddJsonOptions(o => GymBookJson.Configure(o.JsonSerializerOptions));
builder.Services.AddSwagger(builder.Configuration);
// Live sync (see Sync/SyncHub.cs).
builder.Services.AddSignalR();
builder.Services.AddSingleton<GymBook.Api.Sync.SyncNotifier>();

var app = builder.Build();

// Only trusts X-Forwarded-* from loopback, plus any networks in ReverseProxy:KnownNetworks (comma-separated
// CIDRs) - otherwise behind a proxy, client IPs (used for rate limiting) would all be the proxy's.
var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
foreach (var network in (app.Configuration["ReverseProxy:KnownNetworks"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
app.UseForwardedHeaders(forwarded);

// Single-instance deployments apply pending migrations at startup (docker-compose.yml turns this on).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ApiDbContext>().Database.Migrate();
}

// The first SuperAdmin(s), from Admin__BootstrapEmails.
using (var scope = app.Services.CreateScope())
    await AdminBootstrapper.RunAsync(scope.ServiceProvider, app.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSwaggerWithAuth();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GymBook.Api.Sync.SyncHub>("/hubs/sync").DisableRateLimiting();
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();
// Public, login-free pages for the store listing and the app: the privacy policy and how to delete an account.
app.MapGet("/privacy", GymBook.Api.Privacy.PrivacyPolicy.Page).AllowAnonymous().DisableRateLimiting();
app.MapGet("/delete-account", GymBook.Api.Privacy.DeleteAccountPage.Page).AllowAnonymous().DisableRateLimiting();
// A plan its owner shared publicly, for anyone to read on the web (see Social/SharedPlanPage.cs).
app.MapGet("/p/{id}", GymBook.Api.Social.SharedPlanPage.Page).AllowAnonymous().RequireRateLimiting(RateLimits.Foods);

app.Run();

namespace GymBook.Api
{
    public static class RateLimits
    {
        public const string Auth = "auth";
        public const string Sync = "sync";
        public const string Foods = "foods";
    }
}
