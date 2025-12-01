using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace SistemasDeGestionCitasPeluqueria.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddBackendClients(this IServiceCollection services, Uri baseAddress, bool replaceFakes = true, TimeSpan? httpTimeout = null)
    {
        // Servicios de Auth + handler
        var timeout = httpTimeout ?? TimeSpan.FromSeconds(15);
        services.AddSingleton<ITokenStore, SecureTokenStore>();
        services.AddTransient<AuthenticatedHttpMessageHandler>();
        services.AddHttpClient<IAuthService, HttpAuthService>(c =>
        {
            c.BaseAddress = baseAddress;
            c.Timeout = timeout;
        });

        if (replaceFakes)
        {
            services.AddHttpClient<IBarberService, HttpBarberService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            services.AddHttpClient<IInventoryService, HttpInventoryService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            services.AddHttpClient<IServiceOfferingService, HttpServiceOfferingService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            services.AddHttpClient<IReviewService, HttpReviewService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            services.AddHttpClient<IProductCategoryService, HttpProductCategoryService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            services.AddHttpClient<IBarbershopService, HttpBarbershopService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            services.AddHttpClient<IGalleryService, HttpGalleryService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            // NUEVOS: disponibilidad y reservas
            services.AddHttpClient<IAvailabilityService, HttpAvailabilityService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            services.AddHttpClient<IBookingService, HttpBookingService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

            services.AddHttpClient<IUserService, HttpUserService>(c =>
            {
                c.BaseAddress = baseAddress;
                c.Timeout = timeout;
            }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();
        }
        return services;
    }

    public static Uri GetDevBaseAddress()
    {
        var env = Environment.GetEnvironmentVariable("API_BASEURL");
        if (!string.IsNullOrWhiteSpace(env) && Uri.TryCreate(env, UriKind.Absolute, out var u))
            return u;

#if ANDROID
        return new Uri("http://10.0.2.2:25007/");
#elif IOS || MACCATALYST
        return new Uri("http://localhost:25007/");
#else
        return new Uri("http://localhost:25007/");
#endif
    }
}
