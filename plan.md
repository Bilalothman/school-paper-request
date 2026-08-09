# Simple School Paper Request Application

## 1. Goal

Create a small web application where a student can request a paper from the school and an administrator can approve or reject the request.

The application will use:

- **Frontend:** React
- **Backend:** ASP.NET Core Web API with C#
- **Database:** XAMPP MariaDB/MySQL
- **Authentication:** JWT login
- **Workflow:** Camunda

## 2. Users

The application has only two roles:

- **Student:** views services, submits a request, and checks its status.
- **Admin:** views all submitted requests and approves or rejects them.

User accounts can be added directly to the database for this first version. A registration page is not required.

## 3. Main features

### Student features

1. Log in.
2. View all available paper services.
3. Select a service.
4. Submit a request with a short note.
5. View their submitted requests and current statuses.

### Admin features

1. Log in.
2. View all requests submitted by students.
3. Open a request and read its details.
4. Approve or reject the request.
5. Add an optional comment.

## 4. Simple workflow

The Camunda workflow only needs these steps:

`Submitted -> Admin Review -> Approved or Rejected`

Process explanation:

1. A student submits a request.
2. The backend saves it in the database.
3. The backend starts a Camunda process.
4. Camunda creates an admin review task.
5. The admin approves or rejects the request.
6. The backend completes the Camunda task and updates the request status.
7. The student sees the new status on the My Requests page.

React should communicate only with the .NET backend. The backend is responsible for communicating with Camunda.

## 5. Database tables

### Users

- `Id`
- `FullName`
- `Email`
- `PasswordHash`
- `Role` (`Student` or `Admin`)

### Services

- `Id`
- `Name`
- `Description`

Example services:

- Enrollment certificate
- Grade transcript
- Attendance certificate

### Requests

- `Id`
- `StudentId`
- `ServiceId`
- `Note`
- `Status` (`Submitted`, `Approved`, or `Rejected`)
- `AdminComment`
- `CreatedAt`
- `CamundaProcessInstanceId`

No attachments, configurable forms, notifications, payments, or reporting are needed.

## 6. Backend API

### Authentication

- `POST /api/auth/login` — logs in a student or admin and returns a JWT token.

### Student endpoints

- `GET /api/services` — returns the available paper services.
- `POST /api/requests` — submits a new request.
- `GET /api/requests/mine` — returns requests belonging to the logged-in student.

### Admin endpoints

- `GET /api/admin/requests` — returns all student requests.
- `POST /api/admin/requests/{id}/approve` — approves a request.
- `POST /api/admin/requests/{id}/reject` — rejects a request.

The backend must verify roles. Students cannot use admin endpoints, and students can only view their own requests.

## 7. Frontend pages

Only five pages are required:

1. **Login page** — email and password fields.
2. **Services page** — displays all available services with a Request button.
3. **Submit request page** — displays the selected service and a note field.
4. **My Requests page** — displays the student's requests and statuses.
5. **Admin Requests page** — displays all requests with Approve and Reject buttons.

A simple navigation bar can contain Services, My Requests, and Logout. Admins only need Admin Requests and Logout.

## 8. Implementation plan

### Step 1 — Create the projects

- Create the React frontend.
- Create the ASP.NET Core Web API.
- Connect the backend to the MariaDB/MySQL service in XAMPP.

**Result:** the frontend and backend run successfully.

### Step 2 — Build login

- Create the user table and add one student account and one admin account.
- Add JWT authentication to the backend.
- Create the login endpoint and React login page.
- Redirect each role to the correct page after login.

**Result:** students and admins can log in.

### Step 3 — Build services

- Create the service table.
- Add a few paper services to the database.
- Create the services API endpoint.
- Display services in React.

**Result:** students can see all services.

### Step 4 — Build student requests

- Create the request table.
- Add the submit-request endpoint.
- Build the request form.
- Add the My Requests endpoint and page.

**Result:** students can submit requests and view their own requests.

### Step 5 — Add the Camunda workflow

- Create a BPMN process with an admin review task.
- Start the process when a request is submitted.
- Save the Camunda process instance ID with the request.

**Result:** every submitted request starts a workflow.

### Step 6 — Build admin actions

- Create the admin requests endpoint and page.
- Add Approve and Reject actions.
- Complete the Camunda task after an action.
- Update the request status in the database.

**Result:** the admin can process requests, and students can see the updated status.

### Step 7 — Test the main flow

Test this complete scenario:

1. Log in as a student.
2. View services.
3. Submit a request.
4. View it in My Requests with the `Submitted` status.
5. Log in as an admin.
6. View the submitted request.
7. Approve or reject it.
8. Log in as the student and confirm that the status changed.

## 9. Completion checklist

### Student

- [ ] Can log in.
- [ ] Can see all services.
- [ ] Can select and submit a service request.
- [ ] Can see only their own requests.
- [ ] Can see whether a request is submitted, approved, or rejected.

### Admin

- [ ] Can log in.
- [ ] Can see all submitted requests.
- [ ] Can approve or reject a request.
- [ ] The request status is updated after the action.

The application is complete when all items in this checklist work. Extra features should only be added after this basic version is finished.
