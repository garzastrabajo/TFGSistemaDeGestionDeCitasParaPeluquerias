using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemasDeGestionCitasPeluqueria.Models;
using SistemasDeGestionCitasPeluqueria.Services;

namespace SistemasDeGestionCitasPeluqueria.PageModels
{
    // SE AÑADEN IBarberService e IServiceOfferingService para poder enriquecer los nombres
    public partial class ReviewsPageModel(
        IReviewService reviewService,
        IUserService userService,
        IBarberService barberService,
        IServiceOfferingService serviceOfferingService) : ObservableObject
    {
        private readonly IReviewService _reviewService = reviewService;
        private readonly IUserService _userService = userService;
        private readonly IBarberService _barberService = barberService;
        private readonly IServiceOfferingService _serviceOfferingService = serviceOfferingService;

        // Cachés simples para evitar pedir catálogos cada vez
        private IReadOnlyList<Barber>? _barbersCache;
        private IReadOnlyList<ServiceOffering>? _servicesCache;

        [ObservableProperty] private ObservableCollection<ServiceReview> reviews = [];
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        [ObservableProperty] private double averageRating;
        [ObservableProperty] private int totalReviews;
        [ObservableProperty] private int count5;
        [ObservableProperty] private int count4;
        [ObservableProperty] private int count3;
        [ObservableProperty] private int count2;
        [ObservableProperty] private int count1;

        [ObservableProperty] private ObservableCollection<RatingDistributionItem> ratingDistribution = [];

        [RelayCommand]
        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                Error = null;
                var all = await _reviewService.GetAllAsync(ct);
                await EnrichNamesAsync(all, ct); // NUEVO
                Reviews = new ObservableCollection<ServiceReview>(all);
                RecalcStats();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Error = ex.Message; }
            finally { IsBusy = false; }
        }

        public async Task AddAsync(ServiceReview review, CancellationToken ct = default)
        {
            if (review is null) return;

            // No completar UserName/UserPhotoUrl aquí: que lo haga siempre el backend
            await EnrichSingleAsync(review, ct);

            if (review.Id == 0)
                await _reviewService.AddAsync(review, ct);

            if (!Reviews.Any(r => r.Id == review.Id && review.Id != 0))
            {
                Reviews.Insert(0, review);
                RecalcStats();
            }
        }

        public async Task<string?> GetCurrentUserNameAsync(CancellationToken ct = default)
        {
            try
            {
                var me = await _userService.GetMeAsync(ct);
                if (me is null) return null;
                if (!string.IsNullOrWhiteSpace(me.Name)) return me.Name;
                if (!string.IsNullOrWhiteSpace(me.Username)) return me.Username;
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task EnsureCatalogsAsync(CancellationToken ct)
        {
            if (_barbersCache is null)
                _barbersCache = await _barberService.GetAllAsync(ct);
            if (_servicesCache is null)
                _servicesCache = await _serviceOfferingService.GetAllAsync(ct);
        }

        private async Task EnrichNamesAsync(IReadOnlyList<ServiceReview> list, CancellationToken ct)
        {
            // Sólo bajar catálogos si hay al menos una reseña que necesita datos
            var needBarbers = list.Any(r => r.BarberId.HasValue && string.IsNullOrWhiteSpace(r.BarberName));
            var needServices = list.Any(r => r.ServiceId.HasValue && string.IsNullOrWhiteSpace(r.ServiceName));
            if (!(needBarbers || needServices)) return;

            await EnsureCatalogsAsync(ct);

            foreach (var r in list)
            {
                if (r.BarberId.HasValue && string.IsNullOrWhiteSpace(r.BarberName))
                    r.BarberName = _barbersCache?.FirstOrDefault(b => b.Id == r.BarberId)?.Name;
                if (r.ServiceId.HasValue && string.IsNullOrWhiteSpace(r.ServiceName))
                    r.ServiceName = _servicesCache?.FirstOrDefault(s => s.Id == r.ServiceId)?.Name;
            }
        }

        private async Task EnrichSingleAsync(ServiceReview review, CancellationToken ct)
        {
            if ((review.BarberId.HasValue && string.IsNullOrWhiteSpace(review.BarberName)) ||
                (review.ServiceId.HasValue && string.IsNullOrWhiteSpace(review.ServiceName)))
            {
                await EnsureCatalogsAsync(ct);
                if (review.BarberId.HasValue && string.IsNullOrWhiteSpace(review.BarberName))
                    review.BarberName = _barbersCache?.FirstOrDefault(b => b.Id == review.BarberId)?.Name;
                if (review.ServiceId.HasValue && string.IsNullOrWhiteSpace(review.ServiceName))
                    review.ServiceName = _servicesCache?.FirstOrDefault(s => s.Id == review.ServiceId)?.Name;
            }
        }

        private void RecalcStats()
        {
            TotalReviews = Reviews.Count;
            if (TotalReviews == 0)
            {
                AverageRating = 0;
                Count1 = Count2 = Count3 = Count4 = Count5 = 0;
                RatingDistribution = [];
                return;
            }

            AverageRating = Math.Round(Reviews.Average(r => r.Rating), 1);
            Count5 = Reviews.Count(r => r.Rating == 5);
            Count4 = Reviews.Count(r => r.Rating == 4);
            Count3 = Reviews.Count(r => r.Rating == 3);
            Count2 = Reviews.Count(r => r.Rating == 2);
            Count1 = Reviews.Count(r => r.Rating == 1);

            RatingDistribution = new ObservableCollection<RatingDistributionItem>(
            [
                CreateItem(5, Count5),
                CreateItem(4, Count4),
                CreateItem(3, Count3),
                CreateItem(2, Count2),
                CreateItem(1, Count1)
            ]);
        }

        private RatingDistributionItem CreateItem(int rating, int count) =>
            new()
            {
                Rating = rating,
                Count = count,
                Percentage = TotalReviews == 0 ? 0 : (double)count / TotalReviews
            };
    }

    public sealed class RatingDistributionItem
    {
        public int Rating { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
