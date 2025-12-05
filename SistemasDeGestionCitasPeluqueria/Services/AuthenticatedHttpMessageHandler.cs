using System.Net.Http.Headers;

namespace SistemasDeGestionCitasPeluqueria.Services;

public sealed class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly IAuthService _auth;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    // Tiempo máximo para intentar refrescar 
    private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(10);

    public AuthenticatedHttpMessageHandler(IAuthService auth) => _auth = auth;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Obtener token SIN usar el token de la request (evita cancelaciones por navegación)
        var token = await _auth.GetAccessTokenAsync(CancellationToken.None).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancelación iniciada por el caller (navegación / cierre de página): dejar que la capa superior la ignore.
            throw;
        }
        catch (OperationCanceledException)
        {
            // Timeout interno u otra cancelación no iniciada por el caller.
            throw new TaskCanceledException("La petición HTTP se canceló (posible timeout). Revisa HttpClient.Timeout o la conectividad.");
        }

        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        // Intento coordinado de refresh con su propio CTS (evita que una cancelación de la pantalla lo aborte)
        using var refreshCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        refreshCts.CancelAfter(RefreshTimeout);

        await _refreshLock.WaitAsync(refreshCts.Token).ConfigureAwait(false);
        try
        {
            // Ver si otro hilo ya refrescó
            var freshToken = await _auth.GetAccessTokenAsync(CancellationToken.None).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(freshToken))
            {
                var ok = await _auth.RefreshAsync(refreshCts.Token).ConfigureAwait(false);
                if (!ok)
                    return response; // No se pudo refrescar, devolver 401 original

                freshToken = await _auth.GetAccessTokenAsync(CancellationToken.None).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(freshToken))
                return response; // Aún sin token

            // Reintentar con nuevo token
            response.Dispose();
            var cloned = await CloneHttpRequestMessageAsync(request).ConfigureAwait(false);
            cloned.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);

            // El reintento sí usa el token de la request (si el usuario canceló, ya no seguimos)
            return await base.SendAsync(cloned, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (refreshCts.IsCancellationRequested)
        {
            // El refresh excedió el tiempo → devolver 401 y no ocultar
            return response;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content is not null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var h in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        foreach (var h in request.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        clone.Version = request.Version;
        clone.VersionPolicy = request.VersionPolicy;

        return clone;
    }
}