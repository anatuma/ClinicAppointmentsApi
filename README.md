# Clinic Appointments API
This is a simple REST API built with ASP.NET Core to manage a clinic's daily appointments. Instead of using a heavy ORM like Entity Framework, this project uses **raw ADO.NET** (`SqlConnection`, `SqlCommand`) to talk directly to the SQL Server database.
It handles the core scheduling rules, like making sure you can't book a doctor who is already busy or schedule a visit in the past.

## Features
* **View Appointments:** Fetch a list of all appointments, or filter them by status and patient's last name.
* **Detailed View:** Grab the full details of a specific appointment, including doctor licensing info and patient contact details.
* **Book a Visit:** Create a new appointment. The API automatically checks if the date is in the future, if both the doctor and patient are currently active, and ensures the doctor isn't double-booked.
* **Update & Reschedule:** Change appointment details. However, there are guardrails—for instance, you can't reschedule a visit that is already marked as "Completed".
* **Delete:** Remove an appointment (unless it's already completed).

## Tech Stack
* **Framework:** C# + ASP.NET Core Web API
* **Database Access:** Pure ADO.NET
* **Database:** SQL Server

## How to run this project
1. **Set up the database:** Open SQL Server Management Studio (or your preferred tool) and run the `01_create_and_seed_clinic.sql` script.
    * This will create a database named `ClinicAdoNet`.
    * It sets up the schema (`Specializations`, `Patients`, `Doctors`, `Appointments`).
    * It seeds the database with some dummy data so you have something to play with immediately.
2. **Configure the connection:**
   Make sure you have an `appsettings.json` file in the root of your API project with your connection string. It should look something like this:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=ClinicAdoNet;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }