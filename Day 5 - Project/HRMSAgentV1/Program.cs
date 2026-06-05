using HrmsAgent.Logging;
using HrmsAgent.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- HRMS tool stack: logger -> API wrapper -> tools ---
builder.Services.AddSingleton<ApiLogger>();
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["Hrms:ApiBaseUrl"] ?? "http://localhost:5047";
    var apiKey  = cfg["Hrms:ApiKey"] ?? "local-dev-key";
    var timeout = int.Parse(cfg["Hrms:HttpTimeoutSeconds"] ?? "10");
    return new HrmsApiClient(baseUrl, apiKey, timeout, sp.GetRequiredService<ApiLogger>());
});
builder.Services.AddSingleton<HrmsTools>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
