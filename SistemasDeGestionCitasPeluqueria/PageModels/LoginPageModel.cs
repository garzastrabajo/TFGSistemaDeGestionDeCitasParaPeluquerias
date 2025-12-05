using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using SistemasDeGestionCitasPeluqueria.Services;
using SistemasDeGestionCitasPeluqueria.Pages;

namespace SistemasDeGestionCitasPeluqueria.PageModels
{
    public partial class LoginPageModel : ObservableObject
    {
        private readonly IBarbershopService _barbershopService;
        private readonly IGalleryService _galleryService;
        private readonly IAuthService _authService;

        public LoginPageModel(IBarbershopService barbershopService, IGalleryService galleryService, IAuthService authService)
        {
            _barbershopService = barbershopService;
            _galleryService = galleryService;
            _authService = authService;
        }

        [ObservableProperty] private string heroImageUrl = string.Empty;
        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string password = string.Empty;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        public bool CanLogin => !string.IsNullOrWhiteSpace(Username)
                                && !string.IsNullOrWhiteSpace(Password)
                                && !IsBusy;

        partial void OnUsernameChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
        partial void OnPasswordChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
        partial void OnIsBusyChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();

        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(HeroImageUrl)) return;

            var shop = await _barbershopService.GetAsync(ct);
            var candidate = shop?.Images?
                .Select(SanitizePotentialDataUrl)
                .FirstOrDefault(IsSupportedImage);

            if (string.IsNullOrWhiteSpace(candidate))
            {
                var gallery = await _galleryService.GetAllAsync(ct);
                candidate = gallery?
                    .Select(g => SanitizePotentialDataUrl(g.ImageUrl))
                    .FirstOrDefault(IsSupportedImage);
            }

            HeroImageUrl = candidate ?? string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        public async Task LoginAsync(CancellationToken ct)
        {
            if (IsBusy) return;
            Error = null;

            try
            {
                IsBusy = true;

                var ok = await _authService.LoginAsync(Username.Trim(), Password, ct);
                if (!ok)
                {
                    Error = "Credenciales inválidas.";
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current!.MainPage = new AppShell();
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        //  abre el diálogo y registra sin hacer login automático
        [RelayCommand]
        public async Task RegisterAsync(CancellationToken ct)
        {
            if (IsBusy) return;
            Error = null;

            try
            {
                var dto = await RegisterDialogPage.ShowAsync();
                if (dto is null) return; // cancelado

                IsBusy = true;

                var ok = await _authService.RegisterAsync(
                    dto.Username.Trim(),
                    dto.Password,
                    dto.Email,
                    dto.Name,
                    dto.Phone,      
                    signIn: false,
                    ct: ct);

                if (!ok)
                {
                    Error = "No se pudo registrar el usuario.";
                    return;
                }

                Username = dto.Username.Trim();
                Password = dto.Password;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current!.MainPage!.DisplayAlert("Registro", "Usuario registrado correctamente. Introduce tus credenciales y pulsa Iniciar sesión.", "Aceptar");
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static bool IsSupportedImage(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            if (s.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) return true;

            if (Uri.TryCreate(s, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return true;

            return false;
        }

        private static string? SanitizePotentialDataUrl(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            if (s.StartsWith("data:http", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("data:https", StringComparison.OrdinalIgnoreCase))
            {
                return s[5..];
            }
            return s;
        }
    }
}
