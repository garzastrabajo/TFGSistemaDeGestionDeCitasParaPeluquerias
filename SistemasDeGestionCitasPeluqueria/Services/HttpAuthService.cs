using System;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SistemasDeGestionCitasPeluqueria.Services;

public sealed class HttpAuthService(HttpClient http, ITokenStore tokenStore) : IAuthService
{
    private readonly HttpClient _http = http;
    private readonly ITokenStore _tokenStore = tokenStore;

    public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var req = new LoginRequestDto { Username = username, Password = password };
        var resp = await _http.PostAsJsonAsync("auth/login", req, JsonDefaults.Web, ct);
        if (!resp.IsSuccessStatusCode) return false;

        var tokens = await resp.Content.ReadFromJsonAsync<TokenResponse>(JsonDefaults.Web, ct);
        if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken) || string.IsNullOrWhiteSpace(tokens.RefreshToken))
            return false;

        var expiry = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, tokens.ExpiresIn - 30));
        await _tokenStore.SaveAsync(tokens.AccessToken, tokens.RefreshToken, expiry);
        return true;
    }

    public async Task<bool> RegisterAsync(
        string username,
        string password,
        string? email = null,
        string? name = null,
        string? phone = null,          
        bool signIn = true,
        CancellationToken ct = default)
    {
        var req = new RegisterRequestDto
        {
            Username = username,
            Password = password,
            Email = email,
            Name = name,
            Phone = phone 
        };

        var resp = await _http.PostAsJsonAsync("auth/register", req, JsonDefaults.Web, ct);
        if (!resp.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await resp.Content.ReadAsStringAsync(ct); } catch { }

            Debug.WriteLine($"Register failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}");

            string message = body;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("detail", out var detail))
                    {
                        if (detail.ValueKind == JsonValueKind.Array)
                        {
                            var sb = new StringBuilder();
                            foreach (var item in detail.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("msg", out var m))
                                    sb.AppendLine(m.GetString());
                                else
                                    sb.AppendLine(item.ToString());
                            }
                            message = sb.ToString().Trim();
                        }
                        else
                        {
                            message = detail.ToString();
                        }
                    }
                    else if (root.TryGetProperty("message", out var msgProp))
                    {
                        message = msgProp.ToString();
                    }
                }
            }
            catch { }

            throw new InvalidOperationException($"Registro fallido: {message}");
        }

        if (!signIn) return true;

        var tokens = await resp.Content.ReadFromJsonAsync<TokenResponse>(JsonDefaults.Web, ct);
        if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken) || string.IsNullOrWhiteSpace(tokens.RefreshToken))
            return false;

        var expiry = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, tokens.ExpiresIn - 30));
        await _tokenStore.SaveAsync(tokens.AccessToken, tokens.RefreshToken, expiry);
        return true;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var (access, _, expiry) = await _tokenStore.ReadAsync();
        if (string.IsNullOrWhiteSpace(access)) return null;
        if (expiry is DateTimeOffset e && e <= DateTimeOffset.UtcNow) return null;
        return access;
    }

    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        var (_, refresh, _) = await _tokenStore.ReadAsync();
        if (string.IsNullOrWhiteSpace(refresh)) return false;

        var req = new RefreshRequestDto { RefreshToken = refresh! };
        var resp = await _http.PostAsJsonAsync("auth/refresh", req, JsonDefaults.Web, ct);
        if (!resp.IsSuccessStatusCode) return false;

        var dto = await resp.Content.ReadFromJsonAsync<AccessTokenResponse>(JsonDefaults.Web, ct);
        if (dto is null || string.IsNullOrWhiteSpace(dto.AccessToken)) return false;

        var expiry = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, dto.ExpiresIn - 30));
        await _tokenStore.SaveAccessTokenAsync(dto.AccessToken, expiry);
        return true;
    }

    public Task<bool> IsLoggedInAsync(CancellationToken ct = default) =>
        GetAccessTokenAsync(ct).ContinueWith(t => !string.IsNullOrWhiteSpace(t.Result), ct);

    public Task LogoutAsync() => _tokenStore.ClearAsync();
}