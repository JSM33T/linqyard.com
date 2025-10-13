# Linqyard


# Migrations

Add migration

```bash
dotnet ef migrations add "Initial Rebase" --project Linqyard.Data --startup-project Linqyard.Api
```

Update Database

```bash
dotnet ef database update --project Linqyard.Data --startup-project Linqyard.Api
```