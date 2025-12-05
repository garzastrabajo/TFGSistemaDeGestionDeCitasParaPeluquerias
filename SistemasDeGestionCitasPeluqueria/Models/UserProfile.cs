namespace SistemasDeGestionCitasPeluqueria.Models;

public sealed class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? PhotoUrl { get; set; }
}