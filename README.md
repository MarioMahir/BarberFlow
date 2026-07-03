Language: **English** | [Español](README.es.md)

# BarberFlow

BarberFlow is a web application for managing and booking appointments at a barber shop. It supports two authenticated roles (Admin and Barber) and a public, login-free flow that lets clients self-book appointments.

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Seed Data](#seed-data)
- [Running the Application](#running-the-application)
- [Testing](#testing)
- [Roles and Permissions](#roles-and-permissions)
- [License](#license)
- [Contributing](#contributing)

## Features

- Public, login-free booking flow: pick a service, pick a barber, pick an available time slot, and confirm with name/email/phone.
- Availability engine that validates a slot against the barber's working hours, prevents overlapping bookings, and uses a transactional lock per barber to prevent double bookings under concurrent requests.
- Admin management of barbers, services, working hours, and clients.
- Appointment management (create, edit, cancel, delete) with role-based restrictions.
- Weekly calendar view.
- Role-aware dashboard showing today's and upcoming appointments, scoped to the signed-in barber or shop-wide for admins.

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 |
| Web framework | ASP.NET Core MVC (Razor views and controllers) |
| Data access | Entity Framework Core 9 |
| Database | SQL Server (LocalDB in development) |
| Authentication | ASP.NET Core Identity |
| Frontend | Bootstrap, jQuery, jQuery Validation (vendored under `wwwroot/lib`, no npm build step) |
| Testing | xUnit, EF Core InMemory provider, coverlet |

## Project Structure

The solution `BarberFlow.sln` contains two projects:

| Project | Purpose |
|---|---|
| `BarberFlow.Web` | The ASP.NET Core MVC application |
| `BarberFlow.Web.Tests` | xUnit test project covering the appointment availability logic |

Inside `BarberFlow.Web`:

| Folder | Purpose |
|---|---|
| `Controllers/` | MVC controllers: accounts, appointments, barbers, working hours, public booking, calendar, clients, home, services |
| `Data/` | `BarberFlowDbContext`, EF Core migrations seed data, and runtime seeders (`SeedData.cs`, `IdentitySeeder.cs`, `AppointmentSeeder.cs`) |
| `Extensions/` | Helper extensions, including claims-based role/ownership checks |
| `Migrations/` | EF Core migrations and model snapshot |
| `Models/` | View models and `Models/Entities/` domain entities (`Appointment`, `Barber`, `Client`, `Service`, `ApplicationUser`, etc.) |
| `Services/` | Business logic, including `AppointmentAvailabilityService` (working hours, overlap checks, slot generation, transactional booking) |
| `Views/` | Razor views organized per controller, plus shared layout partials |
| `wwwroot/` | Static assets: CSS, JS, and vendored frontend libraries |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (installed with Visual Studio) or a reachable SQL Server instance
- The EF Core CLI tool, if not already installed:
  ```
  dotnet tool install --global dotnet-ef
  ```

## Getting Started

```
dotnet restore
dotnet user-secrets set "IdentitySeed:Password" "<your-dev-password>" --project BarberFlow.Web
dotnet run --project BarberFlow.Web
```

Pending EF Core migrations are applied automatically on startup (`db.Database.Migrate()`), so a manual `dotnet ef database update` is not required for the first run.

To manage migrations manually:

```
dotnet ef migrations add <Name> --project BarberFlow.Web
dotnet ef database update --project BarberFlow.Web
```

## Configuration

`appsettings.json` holds non-sensitive defaults (logging, allowed hosts). `appsettings.Development.json` adds the local connection string:

```json
{
  "ConnectionStrings": {
    "BarberFlowDbConnection": "Server=(localdb)\\mssqllocaldb;Database=BarberFlowDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

No secrets are stored in either file. The seed admin password is read from the configuration key `IdentitySeed:Password`, which is expected to be provided through [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) in development (see the command above) or through a secure configuration provider in other environments. If the key is not set outside of Development, identity seeding is skipped.

## Seed Data

On startup, the application seeds:

- **Services and barbers** (`Data/SeedData.cs`) — a small catalog of services and two example barbers with working hours, baked into the EF Core migrations.
- **Identity accounts** (`Data/IdentitySeeder.cs`) — the `Admin` and `Barber` roles, an admin account, and one login per seeded barber. This only runs when `IdentitySeed:Password` is configured.
- **Demo clients and appointments** (`Data/AppointmentSeeder.cs`) — sample clients and appointments around the current date, useful for exercising the dashboard and calendar views. This only runs when the relevant tables are empty.

## Running the Application

`Properties/launchSettings.json` defines two profiles:

| Profile | URL |
|---|---|
| `http` | `http://localhost:5050` |
| `https` | `https://localhost:7251` (and `http://localhost:5050`) |

Both default to the `Development` environment and open a browser automatically.

## Testing

```
dotnet test
```

`BarberFlow.Web.Tests` exercises `AppointmentAvailabilityService` against the EF Core InMemory provider, covering working-hours validation, overlap detection, slot generation, and booking logic.

## Roles and Permissions

| Capability | Anonymous | Barber | Admin |
|---|---|---|---|
| Book an appointment (public flow) | Yes | Yes | Yes |
| View own dashboard and appointments | No | Yes | Yes (shop-wide) |
| View/manage clients | No | View only | Full CRUD |
| Manage barbers, services, working hours | No | No | Full CRUD |
| Delete appointments | No | No | Yes |

## License

No license has been chosen for this project yet.

## Contributing

This is a personal project under active development. There is no formal contribution process at this time.
