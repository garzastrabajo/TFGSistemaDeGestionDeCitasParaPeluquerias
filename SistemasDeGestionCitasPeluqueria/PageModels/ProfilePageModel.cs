using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SistemasDeGestionCitasPeluqueria.Models;
using SistemasDeGestionCitasPeluqueria.Services;
using System.Net.Mail;
using SistemasDeGestionCitasPeluqueria.Messaging;
using SistemasDeGestionCitasPeluqueria.Pages;

namespace SistemasDeGestionCitasPeluqueria.PageModels
{
    public partial class ProfilePageModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _services;
        private readonly IUserService _userService;
        private readonly IBookingService _bookingService;

        private int _userId;

        public ProfilePageModel(IAuthService authService, IServiceProvider services, IUserService userService, IBookingService bookingService)
        {
            _authService = authService;
            _services = services;
            _userService = userService;
            _bookingService = bookingService;
        }

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string? name;
        [ObservableProperty] private string? email;
        [ObservableProperty] private string? phone;
        [ObservableProperty] private string clientSinceText = string.Empty;
        [ObservableProperty] private string? photoUrl;
        [ObservableProperty] private DateTime birthDate = DateTime.Today.AddYears(-30);

        [ObservableProperty] private bool isEmailValid = true;
        [ObservableProperty] private bool showEmailError;
        [ObservableProperty] private string emailError = string.Empty;

        [ObservableProperty] private bool isPhoneValid = true;
        [ObservableProperty] private bool showPhoneError;
        [ObservableProperty] private string phoneError = string.Empty;

        public DateTime MinBirthDate { get; } = DateTime.Today.AddYears(-120);
        public DateTime MaxBirthDate { get; } = DateTime.Today;

        [ObservableProperty] private ObservableCollection<BookingDto> bookings = new();

        public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name! : Username;
        partial void OnNameChanged(string? value) => OnPropertyChanged(nameof(DisplayName));
        partial void OnUsernameChanged(string value) => OnPropertyChanged(nameof(DisplayName));

        partial void OnIsBusyChanged(bool value)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        partial void OnEmailChanged(string? value)
        {
            ValidateEmail();
            SaveCommand.NotifyCanExecuteChanged();
        }

        partial void OnPhoneChanged(string? value)
        {
            ValidatePhone();
            SaveCommand.NotifyCanExecuteChanged();
        }

        private void ValidateEmail()
        {
            var raw = Email?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(raw))
            {
                // Email opcional
                IsEmailValid = true;
                ShowEmailError = false;
                EmailError = string.Empty;
                return;
            }

            // Requisito mínimo: contiene '@' y formato general correcto
            IsEmailValid = MailAddress.TryCreate(raw, out _);
            ShowEmailError = !IsEmailValid;
            EmailError = IsEmailValid ? string.Empty : "Introduce un correo válido (debe contener '@').";
        }

        private void ValidatePhone()
        {
            var raw = Phone?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(raw))
            {
                // Teléfono opcional
                IsPhoneValid = true;
                ShowPhoneError = false;
                PhoneError = string.Empty;
                return;
            }

            IsPhoneValid = raw.All(char.IsDigit) && raw.Length == 9;
            ShowPhoneError = !IsPhoneValid;
            PhoneError = IsPhoneValid ? string.Empty : "Teléfono inválido: 9 dígitos numéricos.";
        }

        public bool CanSave() => !IsBusy && IsEmailValid && IsPhoneValid;

        [RelayCommand]
        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                Error = null;

                var me = await _userService.GetMeAsync(ct);
                if (me is not null)
                {
                    _userId = me.Id;
                    Username = me.Username;
                    Name = me.Name;
                    Email = me.Email;
                    Phone = me.Phone;
                    ClientSinceText = me.CreatedAt.Year > 0 ? $"Cliente desde {me.CreatedAt.Year}" : string.Empty;
                    BirthDate = me.BirthDate ?? DateTime.Today.AddYears(-30);
                    PhotoUrl = me.PhotoUrl;

                    // Recalcular validaciones con datos iniciales
                    ValidateEmail();
                    ValidatePhone();

                    await LoadBookingsInternalAsync(ct);
                    await LoadUpcomingInternalAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Error = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task LoadBookingsInternalAsync(CancellationToken ct = default)
        {
            try
            {
                var list = await _bookingService.GetMineAsync(ct);
                Bookings = new ObservableCollection<BookingDto>(list);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Error = ex.Message; }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        public async Task SaveAsync(CancellationToken ct = default)
        {
            if (IsBusy) return;
            if (!IsEmailValid || !IsPhoneValid)
            {
                // Mensaje rápido si intenta guardar con errores
                await Application.Current!.MainPage!.DisplayAlert("Validación", "Revisa los campos marcados en rojo.", "Aceptar");
                return;
            }

            try
            {
                IsBusy = true;
                Error = null;

                var req = new UpdateUserProfileRequest
                {
                    Name = string.IsNullOrWhiteSpace(Name) ? null : Name!.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email!.Trim(),
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone!.Trim(),
                    BirthDate = BirthDate == default ? null : BirthDate.Date
                };

                await _userService.UpdateMeAsync(req, ct);
                await LoadAsync(ct);

                WeakReferenceMessenger.Default.Send(
                    new UserProfileUpdatedMessage(_userId, Username, Name, PhotoUrl)
                );

                await Application.Current!.MainPage!.DisplayAlert("Perfil", "Los cambios se guardaron correctamente.", "Aceptar");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Error = ex.Message;
                await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        public async Task ChangePhotoAsync(CancellationToken ct = default)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Error = null;

                string? fileName;
                Stream? stream;

                if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    var file = await MediaPicker.Default.PickPhotoAsync();
                    if (file is null) return;

                    fileName = file.FileName;
                    stream = await file.OpenReadAsync();
                }
                else
                {
                    var file = await FilePicker.Default.PickAsync(new PickOptions
                    {
                        PickerTitle = "Selecciona una foto",
                        FileTypes = FilePickerFileType.Images
                    });
                    if (file is null) return;

                    fileName = file.FileName;
                    stream = await file.OpenReadAsync();
                }

                using (stream)
                {
                    var url = await _userService.UploadPhotoAsync(stream, fileName!, ct);
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        PhotoUrl = url;

                        WeakReferenceMessenger.Default.Send(
                            new UserProfileUpdatedMessage(_userId, Username, Name, PhotoUrl)
                        );

                        await Application.Current!.MainPage!.DisplayAlert("Perfil", "Foto actualizada.", "Aceptar");
                    }
                }
            }
            catch (FeatureNotSupportedException)
            {
                await Application.Current!.MainPage!.DisplayAlert("No soportado", "La selección de fotos no está soportada en este dispositivo.", "Aceptar");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Error = ex.Message;
                await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        private void ClearSensitiveData()
        {
            Username = string.Empty;
            Name = null;
            Email = null;
            Phone = null;
            ClientSinceText = string.Empty;
            PhotoUrl = null;
            BirthDate = DateTime.Today.AddYears(-30);
            Error = null;
            Bookings.Clear();
            UpcomingBookings?.Clear();
        }

        [RelayCommand]
        public async Task LogoutAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Error = null;

                ClearSensitiveData();

                var loginPage = _services.GetRequiredService<LoginPage>();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Application.Current!.MainPage = loginPage;
                });

                if (_services.GetService<ITokenStore>() is ITokenStore tokenStore)
                {
                    await tokenStore.ClearAsync();
                }

                await _authService.LogoutAsync();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        public async Task CancelBookingAsync(BookingDto? booking)
        {
            if (booking is null) return;
            if (IsBusy) return;

            try
            {
                var confirm = await Application.Current!.MainPage!.DisplayAlert(
                    "Cancelar cita",
                    $"¿Deseas cancelar la cita #{booking.Id} del {FormatoFecha(booking.Start)}?",
                    "Sí", "No");
                if (!confirm) return;

                IsBusy = true;
                Error = null;

                var cancelled = await _bookingService.CancelAsync(booking.Id);

                var up = UpcomingBookings?.FirstOrDefault(b => b.Id == booking.Id);
                if (up is not null)
                {
                    up.Status = cancelled.Status;
                    if (up.IsCancelled)
                        UpcomingBookings.Remove(up);
                }

                var hist = Bookings.FirstOrDefault(b => b.Id == booking.Id);
                if (hist is not null)
                    hist.Status = cancelled.Status;

                await Application.Current!.MainPage!.DisplayAlert("Cita", "La cita se canceló correctamente.", "Aceptar");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Error = ex.Message;
                await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string FormatoFecha(string iso)
        {
            if (DateTime.TryParse(iso, out var dt))
                return $"{dt:dd/MM/yyyy HH:mm}";
            return iso;
        }
    }
}
