using Microsoft.AspNetCore.Authentication.Cookies;
using TeamAI.Configuration;
using TeamAI.Services;
using TeamAI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration: bind the Hrms section (BaseUrl only; the bearer token is per-user) ---
builder.Services.AddOptions<HrmsOptions>()
    .Bind(builder.Configuration.GetSection(HrmsOptions.Section))
    .ValidateOnStart();

var hrms = builder.Configuration.GetSection(HrmsOptions.Section).Get<HrmsOptions>() ?? new HrmsOptions();

// In production, source non-secret settings (Hrms:BaseUrl) from Key Vault via DefaultAzureCredential.
// Set "KeyVault:Uri" in configuration to enable. Left inert when unset so dev/QA needs no Azure setup.
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (builder.Environment.IsProduction() && !string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new Azure.Identity.DefaultAzureCredential());
}

// --- HTTP: named client for the one upstream (HRMS). 30s timeout. ---
builder.Services.AddHttpClient("VoyonFolks", client =>
{
    if (!string.IsNullOrWhiteSpace(hrms.BaseUrl))
        client.BaseAddress = new Uri(hrms.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// --- Services ---
// TokenManager reads the signed-in user's JWT from the auth cookie; it needs the ambient HttpContext.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenManager, TokenManager>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();

// Builds the signed-in user's display profile, enriched from HRMS GetEmployeeDetails (best-effort).
builder.Services.AddScoped<IProfileService, HrmsProfileService>();

// Executes the agent's function-tool calls in-process during the chat request, so each HRMS call
// runs under the signed-in user's token (replaces the old Foundry-side OpenAPI tool callback).
builder.Services.AddScoped<IAgentToolDispatcher, AgentToolDispatcher>();

// HRMS credential -> JWT exchange for login. Stateless; uses the shared "VoyonFolks" HTTP client.
builder.Services.AddScoped<IHrmsAuthClient, HrmsAuthClient>();

// --- Auth: cookie session holding the per-user HRMS JWT (httpOnly, encrypted). API endpoints
// return 401/403 as JSON rather than redirecting to a login page. ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "voyon.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;       // same-origin (Vite proxy / co-hosted SPA)
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Secure over HTTPS
        options.SlidingExpiration = false;                // session ends with the HRMS token lifetime
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

// --- Phase 2: chat relay over the existing Foundry agent (additive; tools/playground unchanged). ---
builder.Services.AddOptions<AgentOptions>()
    .Bind(builder.Configuration.GetSection(AgentOptions.Section));

// Singleton so the resolved agent id is cached across requests.
builder.Services.AddSingleton<IAgentService, AgentService>();

// CORS only matters when the SPA is a separate origin (e.g. Vite dev server). The session cookie
// must travel with chat/login calls, so AllowCredentials is required when origins are configured.
// (A separate origin in prod also needs SameSite=None + Secure on the cookie above.) When the SPA
// is served same-origin / behind the Vite proxy, AllowedOrigins can be empty.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
const string SpaCorsPolicy = "spa";
builder.Services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy =>
{
    if (corsOrigins.Length > 0)
        policy.WithOrigins(corsOrigins).AllowAnyHeader().WithMethods("GET", "POST").AllowCredentials();
}));

builder.Services.AddControllers();

var app = builder.Build();

// Any unhandled error still returns the { error: { code, message } } contract (never a stack
// trace) so the Foundry-facing prod environment stays consistent. The dev exception page
// remains in Development for local debugging.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errApp => errApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = new { code = "internal_error", message = "An unexpected error occurred." }
        });
    }));
}

// CORS for the SPA's chat calls (no-op when no origins configured / same-origin).
app.UseCors(SpaCorsPolicy);

// Authentication resolves the session cookie before authorization gates the chat/me endpoints.
app.UseAuthentication();
app.UseAuthorization();

// The agent's tools now run in-process during the chat request (no inbound Foundry callback).
// HTTPS redirect stays off in dev to keep things simple; App Service terminates TLS in prod.
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Resolve the existing Foundry agent id once at startup. Guarded: a misconfigured or
// unreachable Foundry must not stop the app — the MVP tool endpoints stay available and the
// chat relay reports a clean error per request.
using (var scope = app.Services.CreateScope())
{
    var agent = scope.ServiceProvider.GetRequiredService<IAgentService>();
    try
    {
        await agent.InitializeAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Foundry agent could not be initialized at startup; chat relay will retry per request.");
    }
}

app.Run();

// Exposed so the integration test project can reference the entry point via WebApplicationFactory.
public partial class Program { }
