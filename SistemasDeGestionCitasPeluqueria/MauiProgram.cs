using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SistemasDeGestionCitasPeluqueria.PageModels;
using SistemasDeGestionCitasPeluqueria.Pages;
using SistemasDeGestionCitasPeluqueria.Services;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Licensing;

namespace SistemasDeGestionCitasPeluqueria;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH9dcXRSQmhdUUB1WEJWYEg=");

        var builder = MauiApp.CreateBuilder();

        builder.ConfigureSyncfusionCore();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registro de clientes HTTP hacia FastAPI 
        builder.Services.AddBackendClients(ServiceRegistration.GetDevBaseAddress());

        // Login: TRANSIENT para no reutilizar valores tras logout
        builder.Services.AddTransient<LoginPageModel>();
        builder.Services.AddTransient<LoginPage>();

        // VM y páginas
        builder.Services.AddSingleton<MainPageModel>();
        builder.Services.AddSingleton<MainPage>();

        builder.Services.AddSingleton<ServicesPageModel>();
        builder.Services.AddSingleton<ServicesPage>();

        // Products page + VM
        builder.Services.AddTransient<ProductsPageModel>();
        builder.Services.AddTransient<ProductsPage>();

        builder.Services.AddTransient<BookingPageModel>();
        builder.Services.AddTransient<BookingPage>();

        builder.Services.AddSingleton<ReviewsPageModel>();
        builder.Services.AddSingleton<ReviewsPage>();

        // Profile page + VM (puede ser singleton o transient; no guarda credenciales)
        builder.Services.AddSingleton<ProfilePageModel>();
        builder.Services.AddSingleton<ProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
