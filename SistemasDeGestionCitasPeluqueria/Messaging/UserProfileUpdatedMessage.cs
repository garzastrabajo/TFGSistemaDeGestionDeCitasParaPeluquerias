using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SistemasDeGestionCitasPeluqueria.Messaging;

public readonly record struct UserProfileUpdated(int UserId, string Username, string? Name, string? PhotoUrl);

// Mensaje para WeakReferenceMessenger
public sealed class UserProfileUpdatedMessage : ValueChangedMessage<UserProfileUpdated>
{
    public UserProfileUpdatedMessage(int userId, string username, string? name, string? photoUrl)
        : base(new UserProfileUpdated(userId, username, name, photoUrl)) { }
}