using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SistemasDeGestionCitasPeluqueria.Models;

namespace SistemasDeGestionCitasPeluqueria.Services;

public interface IUserService
{
    Task<UserProfile?> GetMeAsync(CancellationToken ct = default);
    Task UpdateMeAsync(UpdateUserProfileRequest request, CancellationToken ct = default);

    Task<string?> UploadPhotoAsync(Stream photoStream, string fileName, CancellationToken ct = default);
}