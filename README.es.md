Idioma: [English](README.md) | **Español**

# BarberFlow

BarberFlow es una aplicación web para gestionar y reservar citas en una barbería. Soporta dos roles autenticados (Admin y Barber) y un flujo público, sin necesidad de iniciar sesión, que permite a los clientes reservar su propia cita.

## Tabla de Contenidos

- [Características](#características)
- [Stack Tecnológico](#stack-tecnológico)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Requisitos Previos](#requisitos-previos)
- [Puesta en Marcha](#puesta-en-marcha)
- [Configuración](#configuración)
- [Datos Semilla](#datos-semilla)
- [Ejecutar la Aplicación](#ejecutar-la-aplicación)
- [Pruebas](#pruebas)
- [Roles y Permisos](#roles-y-permisos)
- [Licencia](#licencia)
- [Contribuir](#contribuir)

## Características

- Flujo público de reserva sin login: elegir servicio, elegir barbero, elegir horario disponible y confirmar con nombre/correo/teléfono.
- Motor de disponibilidad que valida el horario contra las horas laborales del barbero, evita citas solapadas y usa un bloqueo transaccional por barbero para prevenir dobles reservas en solicitudes concurrentes.
- Gestión administrativa de barberos, servicios, horarios laborales y clientes.
- Gestión de citas (crear, editar, cancelar, eliminar) con restricciones según el rol.
- Vista de calendario semanal.
- Panel principal según el rol, mostrando citas de hoy y próximas, acotado al barbero conectado o a toda la barbería para administradores.

## Stack Tecnológico

| Capa | Tecnología |
|---|---|
| Runtime | .NET 9 |
| Framework web | ASP.NET Core MVC (vistas Razor y controladores) |
| Acceso a datos | Entity Framework Core 9 |
| Base de datos | SQL Server (LocalDB en desarrollo) |
| Autenticación | ASP.NET Core Identity |
| Frontend | Bootstrap, jQuery, jQuery Validation (incluidos en `wwwroot/lib`, sin proceso de build con npm) |
| Pruebas | xUnit, proveedor EF Core InMemory, coverlet |

## Estructura del Proyecto

La solución `BarberFlow.sln` contiene dos proyectos:

| Proyecto | Propósito |
|---|---|
| `BarberFlow.Web` | La aplicación ASP.NET Core MVC |
| `BarberFlow.Web.Tests` | Proyecto de pruebas xUnit que cubre la lógica de disponibilidad de citas |

Dentro de `BarberFlow.Web`:

| Carpeta | Propósito |
|---|---|
| `Controllers/` | Controladores MVC: cuentas, citas, barberos, horarios laborales, reserva pública, calendario, clientes, inicio, servicios |
| `Data/` | `BarberFlowDbContext`, datos semilla de las migraciones de EF Core, y sembradores en tiempo de ejecución (`SeedData.cs`, `IdentitySeeder.cs`, `AppointmentSeeder.cs`) |
| `Extensions/` | Extensiones auxiliares, incluyendo validaciones de rol/propiedad basadas en claims |
| `Migrations/` | Migraciones de EF Core y snapshot del modelo |
| `Models/` | View models y entidades de dominio en `Models/Entities/` (`Appointment`, `Barber`, `Client`, `Service`, `ApplicationUser`, etc.) |
| `Services/` | Lógica de negocio, incluyendo `AppointmentAvailabilityService` (horarios laborales, detección de solapamientos, generación de horarios disponibles, reserva transaccional) |
| `Views/` | Vistas Razor organizadas por controlador, más los parciales de layout compartidos |
| `wwwroot/` | Recursos estáticos: CSS, JS y librerías de frontend incluidas |

## Requisitos Previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (se instala con Visual Studio) o una instancia de SQL Server accesible
- La herramienta de línea de comandos de EF Core, si no está instalada:
  ```
  dotnet tool install --global dotnet-ef
  ```

## Puesta en Marcha

```
dotnet restore
dotnet user-secrets set "IdentitySeed:Password" "<tu-password-de-desarrollo>" --project BarberFlow.Web
dotnet run --project BarberFlow.Web
```

Las migraciones de EF Core pendientes se aplican automáticamente al iniciar (`db.Database.Migrate()`), por lo que no es necesario ejecutar `dotnet ef database update` manualmente en el primer arranque.

Para gestionar migraciones manualmente:

```
dotnet ef migrations add <Nombre> --project BarberFlow.Web
dotnet ef database update --project BarberFlow.Web
```

## Configuración

`appsettings.json` contiene valores por defecto no sensibles (logging, hosts permitidos). `appsettings.Development.json` agrega la cadena de conexión local:

```json
{
  "ConnectionStrings": {
    "BarberFlowDbConnection": "Server=(localdb)\\mssqllocaldb;Database=BarberFlowDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

Ninguno de los dos archivos contiene secretos. La contraseña del administrador sembrado se lee de la clave de configuración `IdentitySeed:Password`, que se espera proporcionar mediante [User Secrets de .NET](https://learn.microsoft.com/aspnet/core/security/app-secrets) en desarrollo (ver el comando anterior) o mediante un proveedor de configuración seguro en otros entornos. Si la clave no está definida fuera de Development, el sembrado de identidad se omite.

## Datos Semilla

Al iniciar, la aplicación siembra:

- **Servicios y barberos** (`Data/SeedData.cs`) — un catálogo pequeño de servicios y dos barberos de ejemplo con sus horarios laborales, incluidos en las migraciones de EF Core.
- **Cuentas de Identity** (`Data/IdentitySeeder.cs`) — los roles `Admin` y `Barber`, una cuenta de administrador, y un login por cada barbero sembrado. Esto solo se ejecuta cuando `IdentitySeed:Password` está configurado.
- **Clientes y citas de demostración** (`Data/AppointmentSeeder.cs`) — clientes y citas de ejemplo alrededor de la fecha actual, útiles para probar el panel principal y el calendario. Esto solo se ejecuta cuando las tablas correspondientes están vacías.

## Ejecutar la Aplicación

`Properties/launchSettings.json` define dos perfiles:

| Perfil | URL |
|---|---|
| `http` | `http://localhost:5050` |
| `https` | `https://localhost:7251` (y `http://localhost:5050`) |

Ambos usan por defecto el entorno `Development` y abren el navegador automáticamente.

## Pruebas

```
dotnet test
```

`BarberFlow.Web.Tests` ejercita `AppointmentAvailabilityService` contra el proveedor EF Core InMemory, cubriendo la validación de horarios laborales, la detección de solapamientos, la generación de horarios disponibles y la lógica de reserva.

## Roles y Permisos

| Capacidad | Anónimo | Barber | Admin |
|---|---|---|---|
| Reservar una cita (flujo público) | Sí | Sí | Sí |
| Ver panel y citas propias | No | Sí | Sí (toda la barbería) |
| Ver/gestionar clientes | No | Solo ver | CRUD completo |
| Gestionar barberos, servicios, horarios | No | No | CRUD completo |
| Eliminar citas | No | No | Sí |

## Licencia

Todavía no se ha elegido una licencia para este proyecto.

## Contribuir

Este es un proyecto personal en desarrollo activo. Por el momento no existe un proceso formal de contribución.
