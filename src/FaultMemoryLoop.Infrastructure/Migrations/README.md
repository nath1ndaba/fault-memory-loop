# Migrations

This folder is intentionally empty right now. I don't have the .NET SDK or
`dotnet ef` tooling available where this code was written, so rather than
hand-write EF Core migration files (which are normally auto-generated and
contain a full model snapshot — fragile to fabricate by hand and likely to
be subtly wrong), run this locally once:

```bash
cd src/FaultMemoryLoop.Infrastructure
dotnet tool install --global dotnet-ef   # if you don't already have it
dotnet ef migrations add InitialCreate --startup-project ../FaultMemoryLoop.Api
```

This generates the real migration files in this folder. `Program.cs` already
calls `Database.Migrate()` on startup, so once the migration exists, the
SQLite database (and the `Employees` table) will be created automatically
the first time you run the API.
