# App Desktop / Móvil Barbería 💈

Cliente multiplataforma (.NET MAUI) para la gestión de barbería: consulta servicios, productos, galería y permite realizar reservas. Implementa MVVM, inyección de dependencias y consume la API FastAPI del backend.

Aplicación lista para Windows/Android (y macOS/iOS con el entorno adecuado), con manejo de autenticación JWT, disponibilidad y reservas.

> Nota: si tu entorno usa .NET 8, ajusta los comandos y `TargetFramework`. Este README asume .NET 9.

## Contenido
- [Características](#características)
- [Stack Tecnológico](#stack-tecnológico)
- [Requisitos](#requisitos)
- [Quick Start](#quick-start)
- [Configuración del Backend](#configuración-del-backend)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Arquitectura y Patrones](#arquitectura-y-patrones)
- [Páginas y PageModels](#páginas-y-pagemodels)
- [Servicios HTTP y Autenticación](#servicios-http-y-autenticación)
- [Resumen de Servicios HTTP](#resumen-de-servicios-http)
- [Flujo de Reserva](#flujo-de-reserva)
- [Personalización](#personalización)
- [Seguridad y Permisos](#seguridad-y-permisos)
- [Troubleshooting](#troubleshooting)
- [Scripts Útiles](#scripts-útiles)
- [Roadmap](#roadmap)
- [Contribución](#contribución)
- [Licencia](#licencia)

## Características
- Login / registro con JWT (almacenamiento seguro de tokens).
- Catálogo de servicios y productos con filtrado por categoría.
- Página principal: barbería, barberos, horario, galería, destacados.
- Sistema de reservas: selección de barbero, fecha y huecos disponibles dinámicos.
- Perfil de usuario: edición de datos, foto, histórico y próximas citas (cancelación).
- Consumo de endpoints REST del backend (FastAPI).
- Normalización automática de URLs de imágenes.
- Manejo de estados (`IsBusy`, `Error`) y mensajes de usuario.
- Uso de Syncfusion (componentes UI) y CommunityToolkit.Mvvm.

## Stack Tecnológico
- .NET 9 + .NET MAUI (Windows / Android / macOS / iOS*)
- CommunityToolkit.Mvvm (atributos `[ObservableProperty]`, `[RelayCommand]`)
- Syncfusion.Maui.* (requiere licencia; clave registrada en arranque)
- Inyección de dependencias (`Microsoft.Extensions.DependencyInjection`)
- `HttpClient` + handlers autenticados por servicio
- MVVM + Shell Navigation
- Almacenamiento seguro de tokens (`ITokenStore`) dependiente de plataforma

## Requisitos
- Visual Studio 2022 (versión con soporte .NET 9 y workload MAUI)
- .NET 9 SDK instalado (o .NET 8 si no migrado)
- Workloads:  
	- Android (emulador / dispositivo)  
	- Windows 11 (para escritorio)  
	- macOS + Xcode (solo si compilas iOS/macCatalyst)
- Backend en ejecución (por defecto: `http://localhost:25007/`)

## Quick Start
Clona el monorepo (backend + frontend):

```powershell
git clone https://github.com/garzastrabajo/TFGSistemaDeGestionDeCitasParaPeluquerias.git
cd TFGSistemaDeGestionDeCitasParaPeluquerias/TFGSistemaDeGestionDeCitasParaPeluquerias
dotnet restore
dotnet build
```

Ejecutar desde CLI según plataforma:
```powershell
# Windows Desktop
dotnet run -f net9.0-windows10.0.19041.0

# Android Emulator / Dispositivo
dotnet build -t:Run -f net9.0-android
```

En Visual Studio: abre la solución `SistemasDeGestionCitasPeluqueria.sln`, elige Windows o Android y pulsa F5.

## Configuración del Backend
La BaseAddress se determina en `ServiceRegistration.GetDevBaseAddress()` (o similar en tu registro de servicios):
- Windows / macOS: `http://localhost:25007/`
- Android Emulator: `http://10.0.2.2:25007/`

> Nota: para emulador Android, `localhost` del host se mapea como `10.0.2.2`.
> Warning: Syncfusion requiere licencia registrada; sin ella verás watermark en los componentes.

Para sobrescribir la base vía variable de entorno:
```powershell
$env:API_BASEURL = 'http://localhost:25007/'
dotnet build
```

En Visual Studio: `Project > Properties > Debug > Environment Variables` añadir `API_BASEURL`.

## Estructura del Proyecto
```
SistemasDeGestionCitasPeluqueria/
├─ App.xaml             # Recursos globales
├─ AppShell.xaml        # Shell Navigation (rutas)
├─ MauiProgram.cs       # DI, Syncfusion, fuentes
├─ GlobalXmlns.cs       # Alias XMLNS globales XAML
├─ Properties/          # Configuración de proyecto
├─ Behaviors/           # Behaviors reutilizables
├─ Converters/          # IValueConverters para UI
├─ Helpers/             # Utilidades y helpers
├─ Messaging/           # Mensajería interna
├─ Models/              # DTOs y modelos
├─ PageModels/          # ViewModels (MVVM)
├─ Pages/               # Vistas XAML
├─ Platforms/           # Código por plataforma
├─ Resources/           # Estilos, fuentes, imágenes
├─ Services/            # Servicios y clientes HTTP
└─ README.md
```

## Arquitectura y Patrones
- MVVM: `PageModels` heredan de `ObservableObject`; propiedades con `[ObservableProperty]` y comandos con `[RelayCommand]`.
- DI: servicios HTTP registrados en `MauiProgram` mediante métodos de extensión (p.ej. `AddBackendClients`).
- Behaviors: validaciones ligeras y reutilizables en XAML (p. ej. `DigitsOnlyBehavior`).
- Messaging: comunicación desacoplada con `WeakReferenceMessenger` (p. ej. perfil actualizado).
- `HttpClient` por servicio + `AuthenticatedHttpMessageHandler` para `Authorization: Bearer <token>`.
- Shell Navigation para rutas (`await Shell.Current.GoToAsync("booking", parms)`).
- Normalización de imágenes: `UrlHelper.EnsureAbsolute(string relativeOrAbsolute)`.
- Gestión de estado consistente (`IsBusy`, `Error`) y actualización reactiva de colecciones.

## Páginas y PageModels
| Página    | ViewModel            | Descripción                                          |
|-----------|----------------------|------------------------------------------------------|
| Login     | `LoginPageModel`     | Autenticación (login / registro)                     |
| Main      | `MainPageModel`      | Barbería, barberos, destacados, galería              |
| Services  | `ServicesPageModel`  | Lista de servicios + acción reservar                 |
| Products  | `ProductsPageModel`  | Inventario / filtrado por categoría                  |
| Booking   | `BookingPageModel`   | Selección de fecha/hora y confirmación               |
| Reviews   | `ReviewsPageModel`   | Lectura / creación de reseñas                        |
| Profile   | `ProfilePageModel`   | Datos del usuario, próximas citas, foto, historial   |

## Servicios HTTP y Autenticación
- Autenticación: `IAuthService` (login / registro) no usa handler con token.
- `ITokenStore`: guarda y recupera tokens de forma segura (KeyChain / SecureStorage / etc.).
- Resto de servicios (`IServiceOfferingService`, `IInventoryService`, `IAvailabilityService`, `IBookingService`, `IReviewService`, `IBarbershopService`, `IGalleryService`, `IUserService`, `IProductCategoryService`...) inyectan handler para añadir cabecera `Authorization`.
- Serialización JSON tolerante: opciones tipo camelCase (ej. `JsonDefaults.Web`).

## Resumen de Servicios HTTP
| Servicio/Interfaz          | Ámbito principal                 |
|----------------------------|----------------------------------|
| `IAuthService`             | Registro, login, gestión de JWT  |
| `IAvailabilityService`     | Consulta de disponibilidad       |
| `IBookingService`          | Creación y gestión de reservas   |
| `IServiceOfferingService`  | Listado de servicios             |
| `IInventoryService`        | Catálogo de productos / inventario|
| `IProductCategoryService`  | Categorías de productos          |
| `IBarbershopService`       | Datos de la barbería             |
| `IGalleryService`          | Galería de imágenes              |
| `IUserService`             | Perfil de usuario y foto         |
| `IReviewService`           | Reseñas: listar/crear            |

## Flujo de Reserva
1. Usuario elige servicio (o inicia desde listado de servicios). Comando `ReserveAsync` navega a `booking` pasando parámetros (ServiceId, Price...).
2. `BookingPageModel` carga barberos y disponibilidad vía `IAvailabilityService.GetAsync` filtrando fecha.
3. Usuario selecciona hueco → comando `ConfirmAsync` crea cita (`IBookingService.CreateAsync`).
4. Control de concurrencia: si el hueco ya fue tomado el backend responde 409 y se muestra mensaje apropiado.

Diagrama rápido:
```
🛎️ Servicios → 🧑‍🦱 Barbero → 📅 Fecha/Hora → ✅ Confirmación → 🧾 Reserva
```

## Personalización
- Fuentes: OpenSans registrada en `MauiProgram` (puedes añadir más en `ConfigureFonts`).
- Syncfusion: clave de licencia registrada (añadir tu propia si cambias de entorno).
- Estilos globales: puedes centralizarlos en `App.xaml` o crear `Styles.xaml` y fusionar.
- Imágenes destacadas: gestionadas desde el backend (`barbershop.images`).

## Seguridad y Permisos
- Tokens: almacenados con `ITokenStore` usando `SecureStorage`/KeyChain según plataforma.
- Expiración: maneja 401 forzando re-login o implementa refresh tokens.
- Permisos Android/iOS: acceso a cámara/galería (MediaPicker), almacenamiento si aplica.
- Transporte: usa siempre HTTPS en despliegues públicos.

## Troubleshooting
| Problema                      | Causa                            | Solución                                      |
|------------------------------|----------------------------------|-----------------------------------------------|
| Imágenes no cargan           | URL relativa                     | Revisar `EnsureAbsolute` y `BaseAddress`      |
| Error 401 frecuente          | Token expirado                   | Implementar refresh tokens / forzar re-login  |
| Android sin acceso backend   | Uso de localhost                 | Usar `10.0.2.2` en emulador                   |
| Huecos vacíos                | Sin disponibilidad en API        | Verificar rango fecha/barbero                 |
| Reserva duplicada (409)      | Carrera en confirmación          | Mostrar aviso y refrescar disponibilidad      |
| Foto no sube                 | Permisos plataforma              | Revisar permisos `MediaPicker` / FilePicker   |

## Scripts Útiles
Actualizar y compilar rápido (PowerShell):
```powershell
git pull
dotnet clean
dotnet build -c Debug
```

Ejecutar en línea de comandos (Windows Desktop):
```powershell
dotnet build
dotnet run -f net9.0-windows10.0.19041.0
```

Ejecutar para Android desde CLI (si configurado):
```powershell
dotnet build -t:Run -f net9.0-android
```

## Roadmap
- [ ] Modo offline / caché local básica
- [ ] Refresh tokens automático
- [ ] Notificaciones push (confirmación de reservas)
- [ ] Tema oscuro (XAML + recursos dinámicos)
- [ ] Internacionalización (RESX + binding)
- [ ] Accesibilidad (contraste y tamaños adaptativos)

## Contribución
1. Crea branch descriptiva: `feat/dark-theme` o `fix/booking-409-handling`.
2. Asegura compilación en al menos una plataforma (Windows / Android).
3. Sigue el patrón MVVM (no lógica en code-behind salvo UI trivial).
4. Usa comandos async con manejo de `IsBusy`.
5. Ajusta README si añades secciones relevantes.

Enlaces útiles:
- Backend (README): [TFG Backend](https://github.com/garzastrabajo/TFGSistemaDeGestionDeCitasParaPeluqueriasBackend)
- URL por defecto backend: `http://localhost:25007/`

## Licencia
Proyecto académico (TFG). Revisa condiciones del backend para alineación si se evoluciona a producción.

---

Hecho con ❤️ usando .NET MAUI + CommunityToolkit + Syncfusion.
