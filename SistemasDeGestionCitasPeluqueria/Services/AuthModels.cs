using System.Text.Json.Serialization;

namespace SistemasDeGestionCitasPeluqueria.Services;

public sealed class TokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
    [JsonPropertyName("token_type")]   public string TokenType { get; set; } = "bearer";
    [JsonPropertyName("expires_in")]   public int ExpiresIn { get; set; }
}

public sealed class AccessTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("token_type")]   public string TokenType { get; set; } = "bearer";
    [JsonPropertyName("expires_in")]   public int ExpiresIn { get; set; }
}

public sealed class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshRequestDto
{
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // No serializa si es null (así el backend no intenta validar un valor inexistente)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Phone { get; set; } 
}