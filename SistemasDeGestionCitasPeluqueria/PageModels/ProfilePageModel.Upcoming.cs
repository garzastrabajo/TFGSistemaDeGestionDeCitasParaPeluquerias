using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemasDeGestionCitasPeluqueria.Models;

namespace SistemasDeGestionCitasPeluqueria.PageModels;

public partial class ProfilePageModel
{
    [ObservableProperty] private ObservableCollection<BookingDto> upcomingBookings = new();
    [ObservableProperty] private BookingDto? nextBooking;
    [ObservableProperty] private bool hasUpcoming;

    [ObservableProperty] private int totalBookings;
    [ObservableProperty] private int upcomingBookingsCount;
    [ObservableProperty] private int completedBookingsCount;
    [ObservableProperty] private int cancelledBookingsCount;

    private ObservableCollection<BookingDto>? _subscribedUpcoming;

    partial void OnUpcomingBookingsChanged(ObservableCollection<BookingDto> value)
    {
        if (!ReferenceEquals(_subscribedUpcoming, value))
        {
            if (_subscribedUpcoming is not null)
                _subscribedUpcoming.CollectionChanged -= UpcomingCollectionChanged;
            _subscribedUpcoming = value;
            if (_subscribedUpcoming is not null)
                _subscribedUpcoming.CollectionChanged += UpcomingCollectionChanged;
        }
        UpdateUpcomingSnapshot();
        RecalcStats();
    }

    partial void OnBookingsChanged(ObservableCollection<BookingDto> value)
        => RecalcStats();

    partial void OnNextBookingChanged(BookingDto? value)
        => HasUpcoming = value is not null;

    private void UpcomingCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateUpcomingSnapshot();
        RecalcStats();
    }

    private void UpdateUpcomingSnapshot()
    {
        NextBooking = UpcomingBookings?.FirstOrDefault();
    }

    private async Task LoadUpcomingInternalAsync(CancellationToken ct = default)
    {
        try
        {
            var list = await _bookingService.GetMyUpcomingAsync(10,
                new[] { "confirmed", "confirmada", "pendiente" }, ct);
            UpcomingBookings = new ObservableCollection<BookingDto>(list);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Error = ex.Message; }
    }

    private static string NormalizeStatus(string? s)
        => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

    private void RecalcStats()
    {
        TotalBookings = Bookings?.Count ?? 0;
        UpcomingBookingsCount = UpcomingBookings?.Count ?? 0;

        CompletedBookingsCount = Bookings?.Count(b => b.IsCompleted) ?? 0;
        CancelledBookingsCount = Bookings?.Count(b => b.IsCancelled) ?? 0;
    }
}