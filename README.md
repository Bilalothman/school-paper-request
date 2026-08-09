# School Paper Request Application

A small end-to-end application where students request school papers and administrators approve or reject them.

## Technology

- React 19 with JavaScript, React Router, Axios, and CSS
- ASP.NET Core 10 Web API with JWT role authorization
- Entity Framework Core 9 with XAMPP MariaDB/MySQL
- Camunda Platform 7 REST API and BPMN workflow

Camunda 7 was selected because its REST API maps directly to this small human-task workflow. The application does not mix Camunda 7 and Camunda 8 APIs.

## Requirements

- .NET SDK 10
- Node.js 22 or newer
- XAMPP with its MySQL/MariaDB service
- Camunda Platform 7 with the REST API available at `http://localhost:8080/engine-rest/`

Docker is optional and is not required by this project.

## 1. Configure XAMPP MariaDB

1. Open the XAMPP Control Panel.
2. Start **MySQL**.
3. Open phpMyAdmin at `http://localhost/phpmyadmin`.
4. Select **Import** and import `database/school_paper_requests.sql`. The script creates the database, tables, migration record, and services.

The default XAMPP connection string is in `Backend/appsettings.json`:

```text
Server=127.0.0.1;Port=3306;Database=school_paper_requests;User=root;Password=;
```

XAMPP commonly has a blank root password in local development. If your database has a password or uses another port, set an environment variable without editing the file:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=school_paper_requests;User=root;Password=YOUR_PASSWORD;"
```

The API applies the MariaDB migration and seeds the database when it starts. To update it manually:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project Backend/Backend.csproj
```

## 2. Configure Camunda 7

1. Start Camunda Platform 7 locally.
2. Open Camunda Modeler.
3. Open `Backend/Workflow/school-paper-request.bpmn`.
4. Deploy the diagram to the local Camunda engine.
5. Confirm that the REST API responds at `http://localhost:8080/engine-rest/engine`.

If Camunda uses another URL, set:

```powershell
$env:Camunda__BaseUrl="http://localhost:8080/engine-rest/"
```

The backend starts process definition key `school-paper-request`. A request is rolled back if its process cannot be started. If a process starts but the final database save fails, the backend attempts to delete that process as compensation.

## 3. Configure Gmail verification

Student registration sends a six-digit verification code through Gmail. Enable 2-Step Verification on the Gmail account, create a Google App Password, and set these variables in the same PowerShell window before `npm start`:

```powershell
$env:Gmail__Username="your-school-account@gmail.com"
$env:Gmail__AppPassword="your-16-character-app-password"
$env:Gmail__FromName="School Requests"
```

Use the App Password, not the normal Gmail password. Do not save or commit the App Password in `appsettings.json`. Codes expire after 10 minutes, only their secure hashes are stored, and five incorrect attempts invalidate a registration.

### Google Sign-In

Create a **Web application** OAuth client in Google Cloud, add `http://localhost:3000` as an authorized JavaScript origin, then store its Client ID locally:

```powershell
dotnet user-secrets set "Google:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com" --project Backend/Backend.csproj
```

The same **Continue with Google** button registers a new Student on first use and signs in that Student afterward. Google accounts cannot be used to enter an Admin account.

## 4. Configure security

The checked-in JWT key is only a development placeholder. Set a private value of at least 32 bytes before running outside local development:

```powershell
$env:Jwt__Key="replace-with-a-long-random-private-development-key"
```

Do not commit real database passwords, JWT keys, or Camunda credentials. XAMPP Apache is not used to execute the C# backend; keep MySQL running in XAMPP and run the API with `dotnet run`.

## 5. Start the application with one command

Install the frontend packages once:

```powershell
cd "C:\Users\Admin\Desktop\web month 2"
npm --prefix frontend install
```

After XAMPP MySQL and Camunda 7 are running, start the backend and frontend together:

```powershell
npm start
```

The API runs at `http://localhost:5001`. Open the application at `http://localhost:3000`. Press `Ctrl+C` to stop both development servers.

If the API URL changes, copy `.env.example` to `.env` and update `VITE_API_URL`.

## Development accounts

| Role | Email | Password |
|---|---|---|
| Student | `student@school.com` | `Student123!` |
| Admin | `admin@school.com` | `Admin123!` |

Passwords are hashed with ASP.NET Core's password hasher before database storage. These accounts are for development only.

## API endpoints

- `POST /api/auth/login`
- `POST /api/auth/register` — creates a Student account
- `GET /api/services` — Student
- `POST /api/requests` — Student
- `GET /api/requests/mine` — Student
- `GET /api/admin/requests` — Admin
- `POST /api/admin/requests/{id}/approve` — Admin
- `POST /api/admin/requests/{id}/reject` — Admin

## Complete workflow test

1. Start MySQL in XAMPP, Camunda 7, the backend, and the frontend.
2. Log in as the student.
3. Open Services and request a paper.
4. Confirm that it appears as `Submitted` in My Requests.
5. In Camunda Cockpit or Tasklist, confirm that the process and `Admin Review` task exist.
6. Log out and log in as the admin.
7. Open Admin Requests, add an optional comment, and approve or reject the request.
8. Confirm the task completes in Camunda.
9. Log back in as the student and confirm that the status and admin comment are visible.

Run the automated backend tests with:

```powershell
dotnet test Backend.Tests/Backend.Tests.csproj
```

## Security behavior

- JWTs contain the user ID and role and expire after two hours.
- Backend role attributes enforce Student and Admin access independently from React.
- Student IDs are taken only from the authenticated JWT, never from request input.
- Students can query only requests matching their authenticated ID.
- Only `Submitted` requests can be processed; repeated decisions return HTTP 409.
- EF entities are not returned directly; controllers return DTOs.
- Unexpected server exceptions return a generic message without a stack trace.

## Project structure

```text
Backend/                  ASP.NET Core API, EF migration, authentication, workflow
frontend/src/components/  Shared React components
frontend/src/context/     Authentication state
frontend/src/pages/       The five application pages
frontend/src/services/    Axios API configuration
frontend/src/styles/      Responsive global CSS
```
