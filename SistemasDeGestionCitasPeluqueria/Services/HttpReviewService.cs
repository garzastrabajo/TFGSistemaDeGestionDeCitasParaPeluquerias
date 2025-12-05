using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using SistemasDeGestionCitasPeluqueria.Models;

namespace SistemasDeGestionCitasPeluqueria.Services
{
    public sealed class HttpReviewService(HttpClient http) : IReviewService
    {
        private readonly HttpClient _http = http;

        public async Task<IReadOnlyList<ServiceReview>> GetAllAsync(CancellationToken ct = default)
        {
            var list = await _http.GetFromJsonAsync<List<ServiceReview>>("reviews", JsonDefaults.Web, ct)
                       ?? new List<ServiceReview>();

            foreach (var r in list)
                r.UserPhotoUrl = UrlHelper.EnsureAbsolute(r.UserPhotoUrl, _http.BaseAddress);

            // Más recientes primero 
            return list.OrderByDescending(r => r.CreatedAt).ToList();
        }

        public async Task AddAsync(ServiceReview review, CancellationToken ct = default)
        {
            var payload = new
            {
                barberId = review.BarberId ?? 0,
                serviceId = review.ServiceId ?? 0,
                rating = review.Rating,
                comment = review.Comment ?? string.Empty
                // El backend resolverá siempre nombre y foto desde el perfil actual
            };

            var response = await _http.PostAsJsonAsync("reviews", payload, JsonDefaults.Web, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"POST /reviews -> {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }

            var created = await response.Content.ReadFromJsonAsync<ServiceReview>(JsonDefaults.Web, ct);
            if (created is not null)
            {
                review.Id = created.Id;
                review.CreatedAt = created.CreatedAt;
                review.BarberId = created.BarberId;
                review.ServiceId = created.ServiceId;
                review.UserName = created.UserName; 
                review.UserPhotoUrl = UrlHelper.EnsureAbsolute(created.UserPhotoUrl, _http.BaseAddress);
            }
            else
            {
                review.CreatedAt = DateTimeOffset.UtcNow;
                review.UserPhotoUrl = UrlHelper.EnsureAbsolute(review.UserPhotoUrl, _http.BaseAddress);
            }
        }
    }
}
