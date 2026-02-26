var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<AiEstimator.Services.PdfService>();
builder.Services.AddHttpClient<AiEstimator.Services.OpenRouterService>();
builder.Services.AddHttpClient<AiEstimator.Services.GameEstimationService>();
builder.Services.AddScoped<AiEstimator.Services.AiSummarizationService>();
builder.Services.AddScoped<AiEstimator.Services.PdfExportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
