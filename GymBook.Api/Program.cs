using System.Threading.RateLimiting;
using GymBook.Api;
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
    })
    .AddSignInManager()
    .AddEntityFrameworkStores<ApiDbContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
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
        };
    });

// Every endpoint requires a signed-in user unless it explicitly opts out with [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

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
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SyncProcessor>();
var openAi = builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>() ?? new OpenAiOptions();
builder.Services.AddSingleton(openAi);
builder.Services.AddHttpClient<OpenAiPlanGenerator>(c =>
{
    c.BaseAddress = new Uri(openAi.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(openAi.TimeoutSeconds);
});
// Each AI plan is a paid OpenAI call: PlanGenerationsLimit per user in any PlanGenerationsWindow (e.g. 2 per 1d, 5 per 30d),
// counted in the database so it survives restarts.
builder.Services.AddSingleton(PlanQuota.Parse(
    builder.Configuration.GetValue("RateLimiting:PlanGenerationsLimit", 10),
    builder.Configuration["RateLimiting:PlanGenerationsWindow"] ?? "1h"));
builder.Services.AddScoped<PlanQuota>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers(o =>
    {
        // Otherwise every non-nullable string becomes [Required], which rejects legitimately empty values.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        o.MaxModelValidationErrors = 50;
    })
    .AddJsonOptions(o => GymBookJson.Configure(o.JsonSerializerOptions));
builder.Services.AddSwagger(builder.Configuration);

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
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();
// Public, login-free pages for the store listing and the app: the privacy policy and how to delete an account.
app.MapGet("/privacy", GymBook.Api.Privacy.PrivacyPolicy.Page).AllowAnonymous().DisableRateLimiting();
app.MapGet("/delete-account", GymBook.Api.Privacy.DeleteAccountPage.Page).AllowAnonymous().DisableRateLimiting();

app.Run();

namespace GymBook.Api
{
    public static class RateLimits
    {
        public const string Auth = "auth";
        public const string Sync = "sync";
    }
}
