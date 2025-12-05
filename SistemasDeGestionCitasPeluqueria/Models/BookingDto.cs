using System;
using System.Globalization;

namespace SistemasDeGestionCitasPeluqueria.Models;

public sealed class BookingDto
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public int ServiceId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerPhone { get; set; }
    public string Start { get; set; } = ""; // "YYYY-MM-DDTHH:MM"
    public string End { get; set; } = "";
    public string Status { get; set; } = ""; // "confirmed"

    public string? BarberName { get; set; }
    public string? ServiceName { get; set; }

    public string DisplayService => !string.IsNullOrWhiteSpace(ServiceName) ? ServiceName : $"Servicio #{ServiceId}";
    public string DisplayBarber => !string.IsNullOrWhiteSpace(BarberName) ? BarberName : $"Barbero #{BarberId}";

    public bool IsCancelled =>
        string.Equals(Status, "cancelled", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "canceled", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "cancelada", StringComparison.OrdinalIgnoreCase);

    // Detecta completada por status o por fecha End/Start en pasado
    public bool IsCompleted
    {
        get
        {
            if (IsCancelled) return false;

            if (!string.IsNullOrWhiteSpace(Status))
            {
                if (string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Status, "completada", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            DateTime dt;
            var formats = new[] { "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "s", "o" };

            if (!string.IsNullOrWhiteSpace(End))
            {
                if (DateTime.TryParseExact(End, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                    return dt <= DateTime.Now;
                if (DateTime.TryParse(End, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                    return dt <= DateTime.Now;
            }

            if (!string.IsNullOrWhiteSpace(Start))
            {
                if (DateTime.TryParseExact(Start, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                    return dt <= DateTime.Now;
                if (DateTime.TryParse(Start, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                    return dt <= DateTime.Now;
            }

            return false;
        }
    }

    // Estado efectivo que la UI debería usar (cancelled > completed > status)
    public string EffectiveStatus
        => IsCancelled ? "cancelled"
           : IsCompleted ? "completed"
           : (string.IsNullOrWhiteSpace(Status) ? string.Empty : Status);

    // helper para aplicar estilo amarillo a “confirmada”
    public bool IsConfirmed =>
        string.Equals(EffectiveStatus, "confirmed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(EffectiveStatus, "confirmada", StringComparison.OrdinalIgnoreCase);

    public string StatusLabel
    {
        get
        {
            var s = (EffectiveStatus ?? string.Empty).Trim().ToLowerInvariant();
            return s switch
            {
                "completed" => "completada",
                "completada" => "completada",
                "cancelled" => "cancelada",
                "canceled" => "cancelada",
                "cancelada" => "cancelada",
                "confirmed" => "confirmada",
                "confirmada" => "confirmada",
                "pending" => "pendiente",
                "pendiente" => "pendiente",
                _ => string.IsNullOrWhiteSpace(Status) ? string.Empty : Status
            };
        }
    }
}
