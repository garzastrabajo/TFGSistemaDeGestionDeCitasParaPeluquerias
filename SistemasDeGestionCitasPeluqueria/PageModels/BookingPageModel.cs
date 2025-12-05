using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SistemasDeGestionCitasPeluqueria.Models;
using SistemasDeGestionCitasPeluqueria.Services;

namespace SistemasDeGestionCitasPeluqueria.PageModels;

public partial class BookingPageModel(IBarberService barberService, IAvailabilityService availabilityService, IBookingService bookingService) : ObservableObject
{
    private readonly IBarberService _barberService = barberService;
    private readonly IAvailabilityService _availabilityService = availabilityService;
    private readonly IBookingService _bookingService = bookingService;

    private CancellationTokenSource? _cts;
    private bool _suppressDateChanged;

    // Límites para el calendario 
    public DateTime MinBookingDate { get; } = DateTime.Today;
    public DateTime MaxBookingDate { get; } = DateTime.Today.AddMonths(2);

    [ObservableProperty] private int serviceId;
    [ObservableProperty] private string serviceName = string.Empty;
    [ObservableProperty] private decimal price;
    [ObservableProperty] private int durationMinutes;

    [ObservableProperty] private ObservableCollection<Barber> barbers = new ObservableCollection<Barber>();
    [ObservableProperty] private Barber? selectedBarber;

    [ObservableProperty] private DateTime selectedDate = DateTime.Today;

    [ObservableProperty] private ObservableCollection<TimeSpan> availableSlots = new ObservableCollection<TimeSpan>();
    [ObservableProperty] private TimeSpan? selectedSlot;

    // Datos del cliente
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private string? customerPhone;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;
    [ObservableProperty] private bool hasError;

    // Notificadores parciales generados por CommunityToolkit
    partial void OnSelectedBarberChanged(Barber? value)
    {
        _ = UpdateSlotsAsync(_cts?.Token ?? CancellationToken.None);
        ConfirmCommand?.NotifyCanExecuteChanged();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        if (_suppressDateChanged) return;

        // Normalizar fuera de rango (evita seleccionar días pasados)
        if (value.Date < MinBookingDate)
        {
            _suppressDateChanged = true;
            SelectedDate = MinBookingDate;
            _suppressDateChanged = false;
            return;
        }

        if (value.Date > MaxBookingDate.Date)
        {
            _suppressDateChanged = true;
            SelectedDate = MaxBookingDate;
            _suppressDateChanged = false;
            return;
        }

        _ = UpdateSlotsAsync(_cts?.Token ?? CancellationToken.None);
    }

    partial void OnSelectedSlotChanged(TimeSpan? value)
    {
        ConfirmCommand?.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        ConfirmCommand?.NotifyCanExecuteChanged();
    }

    partial void OnErrorChanged(string? value) => HasError = !string.IsNullOrEmpty(value);

    // Carga inicial / refresco de barberos
    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            Error = null;

            var list = await _barberService.GetAllAsync(ct);
            Barbers = new ObservableCollection<Barber>(list);

            // selecciona por defecto el primer barbero si no hay selección
            if (SelectedBarber is null && Barbers.Count > 0)
            {
                SelectedBarber = Barbers.First();
                // El setter ya dispara UpdateSlotsAsync
            }
            else
            {
                await UpdateSlotsAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Error = ex.Message; }
        finally { IsBusy = false; }
    }

    // Permite que la Page reenvíe parámetros de Shell al VM
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ServiceId", out var id) && id is int i) ServiceId = i;
        if (query.TryGetValue("ServiceName", out var name) && name is string s) ServiceName = s;
        if (query.TryGetValue("Price", out var p) && p is decimal d) Price = d;
        if (query.TryGetValue("DurationMinutes", out var dur) && dur is int dm) DurationMinutes = dm;

        _ = UpdateSlotsAsync(_cts?.Token ?? CancellationToken.None);
    }

    // Helpers de ciclo de vida
    public async Task OnAppearingAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            // Asegurar SelectedDate dentro de rango al aparecer
            if (SelectedDate.Date < MinBookingDate)
                SelectedDate = MinBookingDate;
            else if (SelectedDate.Date > MaxBookingDate.Date)
                SelectedDate = MaxBookingDate;

            await LoadAsync(_cts.Token);
        }
        catch (OperationCanceledException) { }
    }

    public void OnDisappearing()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task UpdateSlotsAsync(CancellationToken ct)
    {
        AvailableSlots.Clear();
        SelectedSlot = null;

        if (SelectedBarber is null) return;

        try
        {
            Error = null;

            // Llamada a API: disponibilidad en pasos de 30 minutos
            var dateStr = SelectedDate.ToString("yyyy-MM-dd");
            var dto = await _availabilityService.GetAsync(
                SelectedBarber.Id,
                dateStr,
                slotMinutes: 30, // media hora
                serviceId: ServiceId > 0 ? ServiceId : null,
                ct
            );

            // Si la fecha es hoy, no mostrar horas pasadas; redondear al siguiente bloque de 30'
            TimeSpan? minTimeToday = null;
            var now = DateTime.Now;
            if (SelectedDate.Date == now.Date)
            {
                var nextMinutes = Math.Ceiling(now.TimeOfDay.TotalMinutes / 30) * 30;
                minTimeToday = TimeSpan.FromMinutes(nextMinutes);
            }

            foreach (var hhmm in dto.Available)
            {
                if (TimeSpan.TryParseExact(hhmm, @"hh\:mm", null, out var ts))
                {
                    // Solo 00 o 30 y no pasado si es hoy
                    if ((ts.Minutes == 0 || ts.Minutes == 30) &&
                        (minTimeToday is null || ts >= minTimeToday.Value))
                    {
                        AvailableSlots.Add(ts);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    // Habilita/deshabilita Confirm según selección y estado 
    public bool CanConfirm() =>
        SelectedBarber != null &&
        SelectedSlot != null &&
        !IsBusy;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    public async Task ConfirmAsync()
    {
        if (SelectedBarber is null || SelectedSlot is null) return;

        // Defensa adicional: no permitir reservar en fecha pasada
        if (SelectedDate.Date < MinBookingDate)
        {
            Error = "No se puede reservar en una fecha pasada.";
            await Shell.Current.DisplayAlert("Fecha inválida", "Selecciona una fecha a partir de hoy.", "OK");
            SelectedDate = MinBookingDate;
            return;
        }

        try
        {
            IsBusy = true;
            Error = null;

            // Pre-check de disponibilidad
            try
            {
                var dateStr = SelectedDate.ToString("yyyy-MM-dd");
                var avail = await _availabilityService.GetAsync(
                    SelectedBarber.Id,
                    dateStr,
                    slotMinutes: 30,
                    serviceId: ServiceId > 0 ? ServiceId : null,
                    _cts?.Token ?? CancellationToken.None
                );

                var slotStr = SelectedSlot.Value.ToString(@"hh\:mm");
                if (!avail.Available.Contains(slotStr))
                {
                    Error = "El horario ya no está disponible.";
                    await Shell.Current.DisplayAlert("Horario no disponible", "El horario seleccionado ya fue reservado. Se actualizarán los huecos disponibles.", "OK");
                    await UpdateSlotsAsync(_cts?.Token ?? CancellationToken.None);
                    return;
                }
            }
            catch
            {
                // Si el pre-check falla por red/errores, seguimos y dejamos que el backend valide.
            }

            var name = string.IsNullOrWhiteSpace(CustomerName) ? string.Empty : CustomerName.Trim();

            var req = new CreateBookingRequest
            {
                BarberId = SelectedBarber.Id,
                ServiceId = ServiceId,
                Date = SelectedDate.ToString("yyyy-MM-dd"),
                Time = SelectedSlot.Value.ToString(@"hh\:mm"),
                CustomerName = name,
                CustomerPhone = string.IsNullOrWhiteSpace(CustomerPhone) ? null : CustomerPhone!.Trim()
            };

            var created = await _bookingService.CreateAsync(req, _cts?.Token ?? CancellationToken.None);

            await Shell.Current.DisplayAlert(
                "Reserva confirmada",
                $"Servicio: {ServiceName}\n" +
                $"Barbero: {SelectedBarber.Name}\n" +
                $"Fecha: {SelectedDate:dd/MM/yyyy}\n" +
                $"Hora: {SelectedSlot:hh\\:mm}",
                "OK");

            // Refrescar huecos tras reservar
            await UpdateSlotsAsync(_cts?.Token ?? CancellationToken.None);
            SelectedSlot = null;
        }
        catch (OperationCanceledException) { }
        catch (HttpRequestException httpEx)
        {
            // Detectar 409 Conflict 
            if (httpEx.StatusCode.HasValue && httpEx.StatusCode.Value == System.Net.HttpStatusCode.Conflict)
            {
                Error = "El horario ya fue reservado por otro cliente.";
                await Shell.Current.DisplayAlert("Error al reservar", "El horario seleccionado ya fue reservado. Se actualizarán los huecos disponibles.", "OK");
                await UpdateSlotsAsync(_cts?.Token ?? CancellationToken.None);
                SelectedSlot = null;
            }
            else
            {
                Error = httpEx.Message;
                await Shell.Current.DisplayAlert("Error al reservar", httpEx.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
