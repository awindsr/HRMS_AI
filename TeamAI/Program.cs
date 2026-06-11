using TeamAI.Configuration;
using TeamAI.Services;
using TeamAI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration: bind the Hrms section (Token/BaseUrl from user-secrets in dev, Key Vault in prod) ---
builder.Services.AddOptions<HrmsOptions>()
    .Bind(builder.Configuration.GetSection(HrmsOptions.Section))
    .ValidateOnStart();

var hrms = builder.Configuration.GetSection(HrmsOptions.Section).Get<HrmsOptions>() ?? new HrmsOptions();

// In production, source secrets (Hrms:Token, Hrms:BaseUrl) from Key Vault via DefaultAzureCredential.
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

// --- Services: Scoped so the Phase 2 per-user JWT swap is a one-method change. ---
builder.Services.AddScoped<ITokenManager, TokenManager>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();

// --- Phase 2: chat relay over the existing Foundry agent (additive; tools/playground unchanged). ---
builder.Services.AddOptions<AgentOptions>()
    .Bind(builder.Configuration.GetSection(AgentOptions.Section));

// Singleton so the resolved agent id is cached across requests.
builder.Services.AddSingleton<IAgentService, AgentService>();

// CORS only matters when the SPA is a separate origin (e.g. Vite dev server). No cookies →
// no AllowCredentials. When the SPA is served same-origin, AllowedOrigins can be empty.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
const string SpaCorsPolicy = "spa";
builder.Services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy =>
{
    if (corsOrigins.Length > 0)
        policy.WithOrigins(corsOrigins).AllowAnyHeader().WithMethods("GET", "POST");
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

// Foundry calls the tool endpoints server-to-server (no CORS needed there). HTTPS redirect
// stays off in dev to keep the dev tunnel simple; App Service terminates TLS in prod.
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
