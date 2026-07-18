# Clinic Appointments API

A REST API for managing clinic appointments — with enough business rules that a real receptionist wouldn't hate it.

No Entity Framework here. This project talks to SQL Server the old-fashioned way: **raw ADO.NET** (`SqlConnection`, `SqlCommand`, parameterized queries). You feel every JOIN. That's the point.

## What it does

- **List appointments** — filter by status and/or patient last name
- **View details** — one appointment with doctor licensing info and patient contacts
- **Book a visit** — checks future date, active doctor & patient, no double-booking
- **Update / reschedule** — with guardrails (you can't move a **Completed** appointment)
- **Delete** — same rule: completed visits stay in the archive

## Tech stack

- **C# / ASP.NET Core Web API**
- **ADO.NET** (`Microsoft.Data.SqlClient`) — no ORM
- **SQL Server**
- **Swagger** in Development

## API endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/appointments` | Optional query: `status`, `patientLastName` |
| `GET` | `/api/appointments/{id}` | Full appointment details |
| `POST` | `/api/appointments` | Create — validation runs first |
| `PUT` | `/api/appointments/{id}` | Update — blocked if completed |
| `DELETE` | `/api/appointments/{id}` | Delete — blocked if completed |

## Run it locally

1. **Create the database** — run `Sql/01_create_and_seed_clinic.sql` in SSMS (or your SQL tool).
   - Creates `ClinicAdoNet` with tables: `Specializations`, `Patients`, `Doctors`, `Appointments`
   - Seeds sample data so you're not staring at empty tables
2. **Add a connection string** — create `appsettings.json` (or extend `appsettings.Development.json`):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=ClinicAdoNet;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```
3. **Run the API:**
   ```bash
   dotnet run
   ```
4. Open Swagger at **http://localhost:5211/swagger**

Cleanup scripts if you want a fresh start: `Sql/02_drop_clinic_tables.sql`, `Sql/03_drop_clinic_database.sql`.
