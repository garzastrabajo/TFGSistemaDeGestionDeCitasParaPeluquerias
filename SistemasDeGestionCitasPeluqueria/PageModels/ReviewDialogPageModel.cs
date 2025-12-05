using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using SistemasDeGestionCitasPeluqueria.Models;
using SistemasDeGestionCitasPeluqueria.Services;

namespace SistemasDeGestionCitasPeluqueria.PageModels;

public partial class ReviewDialogPageModel : ObservableObject
{
    private readonly TaskCompletionSource<ServiceReview?> _tcs;
    private readonly INavigation _navigation;
    private readonly IReviewService? _reviewService;
    private readonly IBarberService? _barberService;
    private readonly IServiceOfferingService? _serviceOfferingService;
    private readonly CancellationTokenSource _cts = new();

    public int? BarberId { get; }
    public int? ServiceId { get; }
    public string? UserName { get; }

    public ObservableCollection<StarItem> Stars { get; } =
        new(Enumerable.Range(1, 5).Select(i => new StarItem(i)));

    [ObservableProperty] private ObservableCollection<Barber> barbers = [];
    [ObservableProperty] private Barber? selectedBarber;

    [ObservableProperty] private ObservableCollection<ServiceOffering> services = [];
    [ObservableProperty] private ServiceOffering? selectedService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPublish))]
    private int rating;

    [ObservableProperty]
    private string? comment;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPublish))]
    private bool isPublishing;

    public bool CanPublish => Rating > 0 && !IsPublishing;

    public IRelayCommand SetRatingCommand { get; }
    public IAsyncRelayCommand PublishAsyncCommand { get; }
    public IAsyncRelayCommand CancelAsyncCommand { get; }

    [RelayCommand]
    private void ClearBarber() => SelectedBarber = null;

    [RelayCommand]
    private void ClearService() => SelectedService = null;

    public ReviewDialogPageModel(
        INavigation navigation,
        TaskCompletionSource<ServiceReview?> tcs,
        int? barberId = null,
        int? serviceId = null,
        string? userName = null,
        IReviewService? reviewService = null,
        IBarberService? barberService = null,
        IServiceOfferingService? serviceOfferingService = null)
    {
        _navigation = navigation;
        _tcs = tcs;
        _reviewService = reviewService;
        _barberService = barberService;
        _serviceOfferingService = serviceOfferingService;

        BarberId = barberId;
        ServiceId = serviceId;
        UserName = string.IsNullOrWhiteSpace(userName) ? null : userName;

        SetRatingCommand = new RelayCommand<int>(value => Rating = value);
        PublishAsyncCommand = new AsyncRelayCommand(PublishAsync, () => CanPublish);
        CancelAsyncCommand = new AsyncRelayCommand(CancelAsync);

        _ = LoadPickersAsync(_cts.Token);
    }

    partial void OnRatingChanged(int value)
    {
        foreach (var star in Stars)
            star.IsFilled = value >= star.Value;
        PublishAsyncCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsPublishingChanged(bool value)
        => PublishAsyncCommand.NotifyCanExecuteChanged();

    private async Task LoadPickersAsync(CancellationToken ct)
    {
        try
        {
            if (_barberService is not null)
            {
                var list = await _barberService.GetAllAsync(ct);
                Barbers = new ObservableCollection<Barber>(list);
                if (BarberId is int bId)
                    SelectedBarber = Barbers.FirstOrDefault(b => b.Id == bId);
            }

            if (_serviceOfferingService is not null)
            {
                var list = await _serviceOfferingService.GetAllAsync(ct);
                Services = new ObservableCollection<ServiceOffering>(list);
                if (ServiceId is int sId)
                    SelectedService = Services.FirstOrDefault(s => s.Id == sId);
            }
        }
        catch (OperationCanceledException) { }
        catch
        {
            // Campos opcionales: ignorar errores de carga
        }
    }

    private async Task PublishAsync()
    {
        if (!CanPublish) return;

        IsPublishing = true;

        try
        {
            var review = new ServiceReview
            {
                Rating = Rating,
                Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment!.Trim(),
                BarberId = SelectedBarber?.Id ?? BarberId,
                ServiceId = SelectedService?.Id ?? ServiceId,
                BarberName = SelectedBarber?.Name,
                ServiceName = SelectedService?.Name,
                UserName = UserName,
                CreatedAt = DateTimeOffset.UtcNow,
                UserPhotoUrl = await TryGetCurrentUserPhotoAsync()
            };

            if (_reviewService is not null)
                await _reviewService.AddAsync(review, _cts.Token);

            _tcs.TrySetResult(review);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _navigation.PopModalAsync();
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Shell.Current.DisplayAlert("Error", $"No se pudo publicar la reseña: {ex.Message}", "OK"));
        }
        finally
        {
            IsPublishing = false;
        }
    }

    private async Task CancelAsync()
    {
        _cts.Cancel();
        _tcs.TrySetResult(null);
        await _navigation.PopModalAsync();
    }

    public static async Task<ServiceReview?> ShowAsync(int? barberId = null, int? serviceId = null, string? userName = null)
    {
        var navigation = Shell.Current?.Navigation ?? throw new InvalidOperationException("Shell.Current no está disponible.");
        var sp = Application.Current?.Handler?.MauiContext?.Services;

        if (string.IsNullOrWhiteSpace(userName))
        {
            var userService = sp?.GetService<IUserService>();
            if (userService is not null)
            {
                var me = await userService.GetMeAsync();
                if (me is not null)
                {
                    userName = !string.IsNullOrWhiteSpace(me.Name)
                        ? me.Name
                        : (!string.IsNullOrWhiteSpace(me.Username) ? me.Username : null);
                }
            }
        }

        var reviewService = sp?.GetService<IReviewService>();
        var barberService = sp?.GetService<IBarberService>();
        var serviceOfferingService = sp?.GetService<IServiceOfferingService>();

        var tcs = new TaskCompletionSource<ServiceReview?>();
        var vm = new ReviewDialogPageModel(
            navigation, tcs, barberId, serviceId, userName,
            reviewService, barberService, serviceOfferingService);

        var page = new Pages.ReviewDialogPage(vm);
        await navigation.PushModalAsync(page);
        return await tcs.Task;
    }

    private async Task<string?> TryGetCurrentUserPhotoAsync()
    {
        try
        {
            var sp = Application.Current?.Handler?.MauiContext?.Services;
            var userService = sp?.GetService<IUserService>();
            var me = userService is not null ? await userService.GetMeAsync() : null;
            return me?.PhotoUrl;
        }
        catch { return null; }
    }
}

public partial class StarItem(int value) : ObservableObject
{
    public int Value { get; } = value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Glyph))]
    [NotifyPropertyChangedFor(nameof(Color))]
    private bool isFilled;

    public string Glyph => IsFilled ? "★" : "☆";

    public Color Color =>
        IsFilled
            ? (Color)Application.Current.Resources["Accent"]
            : (Color)Application.Current.Resources["TextSecondary"];
}
