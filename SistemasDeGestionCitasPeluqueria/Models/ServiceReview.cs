using System.Text.Json.Serialization;

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SistemasDeGestionCitasPeluqueria.Models;

public class ServiceReview
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public int? AppointmentId { get; set; }
    public int? BarberId { get; set; }
    public int? ServiceId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Comment { get; set; }

    public string? UserName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? UserPhotoUrl { get; set; }

    public string? BarberName { get; set; }
    public string? ServiceName { get; set; }

    // Compatibilidad: evitar romper código antiguo que aún usa Date
    [Obsolete("Usa CreatedAt")]
    [JsonIgnore]
    public DateTimeOffset Date
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    [JsonIgnore]
    public bool HasBarberOrService =>
        !string.IsNullOrWhiteSpace(BarberName) || !string.IsNullOrWhiteSpace(ServiceName);

    [JsonIgnore]
    public bool HasBothSelection =>
        !string.IsNullOrWhiteSpace(BarberName) && !string.IsNullOrWhiteSpace(ServiceName);

    [JsonIgnore]
    public string? BarberDisplay =>
        string.IsNullOrWhiteSpace(BarberName) ? null : $"Barbero: {BarberName}";

    [JsonIgnore]
    public string? ServiceDisplay =>
        string.IsNullOrWhiteSpace(ServiceName) ? null : $"Servicio: {ServiceName}";
}