using MauiAOIViewer.Services;
using MauiAOIViewer.Shared.Service;
using MauiAOIViewer.Shared.Services;
using MauiAOIViewer.Web.Components;
using MauiAOIViewer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the MauiAOIViewer.Shared project
builder.Services.AddScoped<ImageDecodeService>();
builder.Services.AddSingleton<IAppPathService, AppPathService>();
builder.Services.AddSingleton<IFormFactor, FormFactor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(MauiAOIViewer.Shared._Imports).Assembly);

app.Run();
