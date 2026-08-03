@echo off
set ConnectionStrings__bdsGTE=Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project "C:\CODE\GTE\src\GTE.WebApi" --urls http://localhost:5088
