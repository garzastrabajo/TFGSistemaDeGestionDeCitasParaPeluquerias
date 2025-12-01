using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SistemasDeGestionCitasPeluqueria.Services;

namespace SistemasDeGestionCitasPeluqueria.PageModels;

public partial class RegisterDialogPageModel : ObservableObject
{
    readonly TaskCompletionSource<RegisterRequestDto?> _tcs = new();

    [ObservableProperty] private string fullName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;
    [ObservableProperty] private bool hasError;

    [ObservableProperty] private bool isPasswordHintsVisible;
    [ObservableProperty] private bool isLenOk;
    [ObservableProperty] private bool isUpperOk;
    [ObservableProperty] private bool isLowerOk;
    [ObservableProperty] private bool isDigitOk;
    [ObservableProperty] private bool isSymbolOk;

    [ObservableProperty] private bool isConfirmVisible;
    [ObservableProperty] private bool isConfirmMatch;
    [ObservableProperty] private string confirmMessage = string.Empty;

    // Validación teléfono
    [ObservableProperty] private bool showPhoneError;
    [ObservableProperty] private string phoneError = string.Empty;
    [ObservableProperty] private bool isPhoneValid = true;

    // Validación email
    [ObservableProperty] private bool showEmailError;
    [ObservableProperty] private string emailError = string.Empty;
    [ObservableProperty] private bool isEmailValid = true;

    partial void OnPasswordChanged(string value) => EvaluatePassword();
    partial void OnConfirmPasswordChanged(string value) => EvaluateConfirm();
    partial void OnErrorChanged(string? value) => HasError = !string.IsNullOrEmpty(value);
    partial void OnUsernameChanged(string value) => RegisterCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => RegisterCommand.NotifyCanExecuteChanged();
    partial void OnPhoneChanged(string value) => EvaluatePhone();
    partial void OnEmailChanged(string value) => EvaluateEmail();

    void EvaluatePassword()
    {
        var pwd = Password ?? string.Empty;
        var (len, upper, lower, digit, symbol) = ValidatePassword(pwd);
        IsLenOk = len;
        IsUpperOk = upper;
        IsLowerOk = lower;
        IsDigitOk = digit;
        IsSymbolOk = symbol;
        IsPasswordHintsVisible = !string.IsNullOrEmpty(pwd);

        EvaluateConfirm();
        RegisterCommand.NotifyCanExecuteChanged();
    }

    void EvaluateConfirm()
    {
        var pwd = Password ?? string.Empty;
        var confirm = ConfirmPassword ?? string.Empty;
        if (!string.IsNullOrEmpty(confirm))
        {
            IsConfirmVisible = true;
            IsConfirmMatch = pwd == confirm;
            ConfirmMessage = IsConfirmMatch ? "Las contraseñas coinciden" : "Las contraseñas no coinciden";
        }
        else
        {
            IsConfirmVisible = false;
            IsConfirmMatch = false;
            ConfirmMessage = string.Empty;
        }

        RegisterCommand.NotifyCanExecuteChanged();
    }

    void EvaluatePhone()
    {
        var raw = Phone?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            // Teléfono opcional: si está vacío es válido y no muestra error
            IsPhoneValid = true;
            PhoneError = string.Empty;
            ShowPhoneError = false;
        }
        else
        {
            IsPhoneValid = IsValidPhone(raw);
            ShowPhoneError = !IsPhoneValid;
            PhoneError = IsPhoneValid ? string.Empty : "El teléfono debe tener exactamente 9 dígitos (solo números).";
        }

        RegisterCommand.NotifyCanExecuteChanged();
    }

    void EvaluateEmail()
    {
        var raw = Email?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            IsEmailValid = true;          // opcional: vacío es válido
            ShowEmailError = false;
            EmailError = string.Empty;
        }
        else
        {
            // Requisito: contiene '@' y formato general válido
            IsEmailValid = raw.Contains('@') && IsValidEmail(raw);
            ShowEmailError = !IsEmailValid;
            EmailError = IsEmailValid ? string.Empty : "Introduce un correo válido (debe contener '@').";
        }

        RegisterCommand.NotifyCanExecuteChanged();
    }

    static bool IsValidPhone(string value)
        => value.Length == 9 && value.All(char.IsDigit);

    static (bool len, bool upper, bool lower, bool digit, bool symbol) ValidatePassword(string pwd)
        => (pwd.Length >= 8,
            pwd.Any(char.IsUpper),
            pwd.Any(char.IsLower),
            pwd.Any(char.IsDigit),
            pwd.Any(ch => !char.IsLetterOrDigit(ch)));

    public Task<RegisterRequestDto?> WaitForResultAsync() => _tcs.Task;

    public bool CanRegister() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Username)
        && IsLenOk && IsUpperOk && IsLowerOk && IsDigitOk && IsSymbolOk
        && Password == ConfirmPassword
        && IsPhoneValid
        && IsEmailValid; // nuevo bloqueo por email inválido

    [RelayCommand(CanExecute = nameof(CanRegister))]
    async Task RegisterAsync()
    {
        if (IsBusy) return;
        Error = null;

        try
        {
            IsBusy = true;

            if (Username?.Trim().Length < 3)
            {
                Error = "El usuario debe tener al menos 3 caracteres.";
                return;
            }

            if (!(IsLenOk && IsUpperOk && IsLowerOk && IsDigitOk && IsSymbolOk))
            {
                Error = "La contraseña no cumple los requisitos.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                Error = "Las contraseñas no coinciden.";
                return;
            }

            if (!IsPhoneValid)
            {
                Error = "Revisa el teléfono: debe tener 9 dígitos.";
                return;
            }

            if (!IsEmailValid)
            {
                Error = "Introduce un email válido o déjalo vacío.";
                return;
            }

            string? emailVal = null;
            if (!string.IsNullOrWhiteSpace(Email))
            {
                if (!IsValidEmail(Email.Trim()))
                {
                    Error = "Introduce un email válido o déjalo vacío.";
                    return;
                }
                emailVal = Email.Trim();
            }

            string? phoneVal = null;
            if (!string.IsNullOrWhiteSpace(Phone))
            {
                var trimmed = Phone.Trim();
                if (!IsValidPhone(trimmed))
                {
                    Error = "Revisa el teléfono: debe tener 9 dígitos.";
                    return;
                }
                phoneVal = trimmed;
            }

            var dto = new RegisterRequestDto
            {
                Username = Username.Trim(),
                Password = Password,
                Email = emailVal,
                Name = string.IsNullOrWhiteSpace(FullName) ? null : FullName.Trim(),
                Phone = phoneVal
            };

            _tcs.TrySetResult(dto);

            if (Shell.Current?.Navigation is not null)
                await Shell.Current.Navigation.PopModalAsync();
            else if (Application.Current?.MainPage?.Navigation is not null)
                await Application.Current.MainPage.Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RegisterCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    async Task CancelAsync()
    {
        _tcs.TrySetResult(null);

        if (Shell.Current?.Navigation is not null)
            await Shell.Current.Navigation.PopModalAsync();
        else if (Application.Current?.MainPage?.Navigation is not null)
            await Application.Current.MainPage.Navigation.PopModalAsync();
    }

    static bool IsValidEmail(string value) => MailAddress.TryCreate(value, out _);
}
