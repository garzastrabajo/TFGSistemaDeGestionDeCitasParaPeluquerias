using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemasDeGestionCitasPeluqueria.Models;
using SistemasDeGestionCitasPeluqueria.Services;

namespace SistemasDeGestionCitasPeluqueria.PageModels;

public partial class MainPageModel(
    IServiceOfferingService serviceOfferingService,
    IBarberService barberService,
    IInventoryService inventoryService,
    IBarbershopService barbershopService,
    IGalleryService galleryService
) : ObservableObject
{
    private readonly IServiceOfferingService _serviceOfferingService = serviceOfferingService;
    private readonly IBarberService _barberService = barberService;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IBarbershopService _barbershopService = barbershopService;
    private readonly IGalleryService _galleryService = galleryService;

    [ObservableProperty] private ObservableCollection<ServiceOffering> services = [];
    [ObservableProperty] private ObservableCollection<Barber> barbers = [];
    [ObservableProperty] private Barber? selectedBarber; 
    [ObservableProperty] private ObservableCollection<InventoryItem> featuredProducts = [];
    [ObservableProperty] private ObservableCollection<GalleryItem> gallery = [];

    [ObservableProperty] private string barbershopName = string.Empty;
    [ObservableProperty] private string heroImageUrl = string.Empty;

    [ObservableProperty] private string openingText = string.Empty;
    [ObservableProperty] private string openingLine1Label = string.Empty;
    [ObservableProperty] private string openingLine1Value = string.Empty;
    [ObservableProperty] private string openingLine2Label = string.Empty;
    [ObservableProperty] private string openingLine2Value = string.Empty;

    [ObservableProperty] private string addressText = string.Empty;
    [ObservableProperty] private string contactText = string.Empty;

    [ObservableProperty] private ObservableCollection<string> aboutParagraphs = [];
    [ObservableProperty] private ObservableCollection<string> aboutHighlights = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [RelayCommand]
    public async Task LoadHomeAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            Error = null;

            try
            {
                var shop = await _barbershopService.GetAsync(ct);
                if (shop is not null)
                {
                    BarbershopName = shop.Name;

                    // Selecciona la primera imagen válida 
                    HeroImageUrl = shop.Images?
                        .Select(SanitizePotentialDataUrl)
                        .Where(IsSupportedImage)
                        .FirstOrDefault() ?? string.Empty;

                    OnPropertyChanged(nameof(HeroImageUrl));

                    AddressText = string.IsNullOrWhiteSpace(shop.Address)
                        ? string.Empty
                        : $"{shop.Address}\n{shop.City}, {shop.Country}";

                    ContactText = $"{shop.Phone}\n{shop.Email}";

                    OpeningText = BuildOpeningText(shop.OpeningHours);
                    BuildOpeningLines(shop.OpeningHours);

                    ParseAbout(shop.About);
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }

            var servicesTask = _serviceOfferingService.GetAllAsync(ct);
            var barbersTask = _barberService.GetAllAsync(ct);
            var productsTask = _inventoryService.GetFeaturedAsync(6, ct);
            var galleryTask = _galleryService.GetAllAsync(ct);

            try { Services = new ObservableCollection<ServiceOffering>(await servicesTask); } catch (Exception ex) { Error = ex.Message; }
            try { Barbers = new ObservableCollection<Barber>(await barbersTask); } catch (Exception ex) { Error = ex.Message; }
            try { FeaturedProducts = new ObservableCollection<InventoryItem>(await productsTask); } catch (Exception ex) { Error = ex.Message; }
            try
            {
                var gal = await galleryTask;
                Gallery = new ObservableCollection<GalleryItem>(gal);
            }
            catch (Exception ex) { Error = ex.Message; }

            // Fallback: si la API no trae una imagen válida para la cabecera, usa la primera de la galería
            if (string.IsNullOrWhiteSpace(HeroImageUrl) && Gallery?.Count > 0)
            {
                var fallback = Gallery
                    .Select(g => SanitizePotentialDataUrl(g.ImageUrl))
                    .FirstOrDefault(IsSupportedImage);

                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    HeroImageUrl = fallback!;
                    OnPropertyChanged(nameof(HeroImageUrl));
                }
            }

            // Fallback si el backend no trae nada en about
            if (AboutParagraphs.Count == 0 && AboutHighlights.Count == 0)
            {
                AboutParagraphs.Add("Información de la barbería no disponible actualmente.");
            }
        }
        catch (OperationCanceledException) { }
        finally { IsBusy = false; }
    }

    private void ParseAbout(string? about)
    {
        AboutParagraphs.Clear();
        AboutHighlights.Clear();
        if (string.IsNullOrWhiteSpace(about)) return;

        var normalized = about.Replace("\r\n", "\n").Trim();
        var blocks = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var block in blocks)
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length > 0 && lines.All(l => l.StartsWith("- ") || l.StartsWith("* ")))
            {
                foreach (var l in lines)
                {
                    var text = l[2..].Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        AboutHighlights.Add(text);
                }
            }
            else
            {
                // Bloque como párrafo completo
                var paragraph = string.Join(" ", lines).Trim();
                if (!string.IsNullOrWhiteSpace(paragraph))
                    AboutParagraphs.Add(paragraph);
            }
        }
    }

    private static string BuildOpeningText(OpeningHours? oh)
    {
        if (oh is null) return "Horario no disponible";
        string Today(DayHours? d) => d is null ? "Cerrado" : $"{d.Open} - {d.Close}";
        var now = DateTime.Now.DayOfWeek;
        var today = GetDay(oh, now);
        var sunday = GetDay(oh, DayOfWeek.Sunday);
        return $"Hoy: {Today(today)}\nDom: {Today(sunday)}";
    }

    private void BuildOpeningLines(OpeningHours? oh)
    {
        if (oh is null)
        {
            OpeningLine1Label = "Hoy:";
            OpeningLine1Value = "Horario no disponible";
            OpeningLine2Label = "Dom:";
            OpeningLine2Value = "—";
            return;
        }

        string Today(DayHours? d) => d is null ? "Cerrado" : $"{d.Open} - {d.Close}";
        var now = DateTime.Now.DayOfWeek;
        OpeningLine1Label = "Hoy:";
        OpeningLine1Value = Today(GetDay(oh, now));
        OpeningLine2Label = "Dom:";
        OpeningLine2Value = Today(GetDay(oh, DayOfWeek.Sunday));
    }

    private static DayHours? GetDay(OpeningHours oh, DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => oh.Monday,
        DayOfWeek.Tuesday => oh.Tuesday,
        DayOfWeek.Wednesday => oh.Wednesday,
        DayOfWeek.Thursday => oh.Thursday,
        DayOfWeek.Friday => oh.Friday,
        DayOfWeek.Saturday => oh.Saturday,
        DayOfWeek.Sunday => oh.Sunday,
        _ => null
    };

    private static bool IsSupportedImage(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (s.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (Uri.TryCreate(s, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return true;
        return false;
    }

    private static string? SanitizePotentialDataUrl(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        // Convierte "data:https://..." y "data:http://..." en URL normal
        if (s.StartsWith("data:http", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("data:https", StringComparison.OrdinalIgnoreCase))
        {
            return s[5..];
        }
        return s;
    }

    [RelayCommand]
    private void SelectBarber(Barber barber) => SelectedBarber = barber;

}