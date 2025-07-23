using MauiAOIViewer.Services;
using MauiAOIViewer.Shared.Service;
using MauiAOIViewer.Shared.Services;
using Microsoft.Extensions.Logging;

namespace MauiAOIViewer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the MauiAOIViewer.Shared project
            builder.Services.AddScoped<ImageDecodeService>();
            builder.Services.AddSingleton<IAppPathService, AppPathService>();
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
