# Complete School Paper Request Application Plan

## 1. Purpose and success standard

Build a polished, secure, responsive web application where students can create accounts, sign in, request official school papers, track decisions, and manage their password. Administrators can review requests and manage the paper-service catalog. Every student request must be coordinated with a Camunda 7 workflow.

This document is an implementation specification, not only a feature list. Another agent should be able to use it to recreate the application at the same functional and visual level.

The application is complete only when:

- All features and edge cases in this plan work end to end.
- Backend authorization is enforced independently of frontend routing.
- Database migrations, seed data, setup documentation, and automated tests are included.
- The interface has the same consistent, production-style quality across desktop and mobile.
- Loading, empty, success, validation, disabled, and error states are handled on every page.

## 2. Technology and architecture

- **Frontend:** React 19, JavaScript, Vite, React Router, Axios, and custom responsive CSS.
- **Backend:** ASP.NET Core Web API with C#.
- **Database:** MariaDB/MySQL running through XAMPP.
- **ORM:** Entity Framework Core with Pomelo MySQL/MariaDB provider and migrations.
- **Authentication:** JWT bearer tokens and ASP.NET Core password hashing.
- **Google authentication:** Google Identity Services with backend ID-token verification.
- **Email:** Gmail SMTP through a Node helper process using a Gmail App Password.
- **Workflow:** Camunda Platform 7 REST API and a BPMN process.
- **Testing:** xUnit backend tests with an EF Core in-memory database and fake external services.

Architecture rules:

1. React communicates only with the ASP.NET Core API.
2. The backend owns all database, authentication, authorization, email, Google-token verification, and Camunda operations.
3. Never call Camunda, Gmail, or the database directly from React.
4. Controllers return purpose-built DTOs, never EF entities.
5. External integration logic must be behind interfaces so it can be replaced with fakes in tests.

## 3. Roles and permissions

### Student

- Register with email and password, then verify a six-digit email code.
- Register or sign in with Google; a new Google account must also verify its email once.
- Sign in with email and password.
- Recover a forgotten password through an email code.
- View the available paper services.
- Submit a request with an optional note.
- View only their own requests, statuses, and admin comments.
- View their profile and change their password when the account has a local password.

### Admin

- Sign in with email and password.
- View every student request.
- Approve or reject a submitted request with an optional comment.
- Add services to the paper catalog.
- Remove only services that have never been used by a request.
- View their profile and change their password.

### Permission rules

- Public endpoints are limited to login, registration, email verification, Google authentication/configuration, and password recovery.
- Student endpoints require a valid JWT with the `Student` role.
- Admin endpoints require a valid JWT with the `Admin` role.
- Profile endpoints require any authenticated user.
- A student's ID always comes from JWT claims, never from a submitted request body.
- Students can never retrieve another student's request.
- Google authentication must never sign in or link an Admin account.
- Frontend protected routes improve UX but are not a security boundary.

## 4. User journeys

### Email registration

1. The user can reach registration only from the login page.
2. Collect full name, email, password, and confirmation.
3. Normalize email by trimming and converting to lowercase.
4. Require a name of at least 2 characters and a password of 8–100 characters containing uppercase, lowercase, and numeric characters.
5. Reject mismatched passwords and an email already owned by a user.
6. Store a pending registration, not a real user, and email a random six-digit code.
7. The code expires after 10 minutes; resend attempts are limited to once every 30 seconds.
8. Store only a keyed hash of the code and a secure hash of the password.
9. Allow five incorrect code attempts. Remove the pending record after the fifth failure or expiration.
10. On successful verification, create a Student, delete the pending record, return a JWT/user response, and sign the user in automatically.

### Google registration and login

1. Fetch the configured Google client ID from the backend and hide the Google button when it is unavailable.
2. Load Google Identity Services and render a **Continue with Google** button on login and registration.
3. Send the Google credential to the backend and verify it against the configured client ID.
4. Require a verified email, subject identifier, and valid token.
5. An existing Student already linked to the subject signs in immediately.
6. A first-time Google Student receives the same six-digit email-verification flow before account creation/linking.
7. Prevent one email from being connected to different Google subjects.
8. Prevent Google from accessing an Admin account.
9. Mark Google-only accounts distinctly so the profile cannot offer a password change that cannot succeed.

### Login and session

1. The login page starts with empty email and password fields and discourages browser credential autofill.
2. Normalize the email and verify the password hash.
3. Return a JWT plus `{ id, fullName, email, role }`.
4. Store the token and user in local storage and restore the user on refresh.
5. Add `Authorization: Bearer <token>` to authenticated API calls.
6. Redirect Students to `/services` and Admins to `/admin/requests`.
7. On an HTTP 401, clear the local session and return to login.
8. Logout clears the token/user and returns to login.

### Forgot password

1. From login, open a public recovery page.
2. Accept an email and always return a neutral accepted message, even if no account exists, to prevent account enumeration.
3. For an existing account, create a six-digit code with the same 10-minute expiry, 30-second resend limit, keyed hashing, and five-attempt limit.
4. Email the code with password-reset wording.
5. Collect code, new password, and confirmation.
6. Enforce the standard password rules, update the secure password hash, and delete the reset record.
7. Redirect to login with a success message.

### Profile and password change

1. Both roles can open `/profile`.
2. Display full name, email, and role as read-only values.
3. Allow changing the password with current password, new password, and confirmation.
4. Validate the current password, password strength, confirmation, and that the new password differs from the current one.
5. Reject password changes for Google-only accounts with a clear message.
6. Clear password fields and show success feedback after completion.

### Student request

1. Show the catalog of services ordered by name.
2. Each service card includes name, description, an icon, and a request action.
3. The submit page shows the selected service, optional note, 1000-character counter, and a three-step “what happens next” explanation.
4. Validate that the service still exists on the backend.
5. Save the request and start a Camunda process as one consistent operation.
6. On success, go to My Requests with a success message.
7. On Camunda failure, do not leave a database request behind.

### Admin decision

1. Show all requests newest first with student name/email, service, note, timestamp, status, and decision comment.
2. Show summary counts for total, submitted, approved, and rejected.
3. Only Submitted cards display comment, Approve, and Reject controls.
4. Find the request's active `adminReview` Camunda task and complete it with an `Approved` or `Rejected` process variable.
5. Update the database only after Camunda task completion succeeds.
6. A request may be processed only once; repeated decisions return HTTP 409.
7. Completed cards display the stored admin comment and no action controls.

### Admin service management

1. List all services alphabetically.
2. Create a service with a unique case-insensitive name (2–100 characters) and description (2–500 characters).
3. Make a newly created service immediately visible in the Student catalog.
4. Confirm before removal.
5. Allow deletion only if no request references the service; otherwise return HTTP 409 with a useful message.

## 5. Camunda workflow

Use Camunda Platform 7 only; do not mix Camunda 7 and Camunda 8 APIs.

The BPMN file must be committed with process definition key `school-paper-request` and this flow:

`Start -> Admin Review user task -> Approved/Rejected gateway -> End`

Required integration behavior:

- Start through `POST process-definition/key/school-paper-request/start`.
- Use business key `request-{requestId}`.
- Send `requestId` as an Integer process variable.
- Persist the returned process instance ID on the request.
- Find the review task by process instance ID and task definition key `adminReview`.
- Complete it with the String variable `decision` equal to `Approved` or `Rejected`.
- Wrap request creation in a database transaction.
- If workflow start fails, roll back the request.
- If Camunda starts but the final database save fails, attempt to delete the process instance as compensation.
- If decision completion fails, keep the database status unchanged and return HTTP 503.
- Log external failures, but return safe, concise messages without implementation details.

## 6. Database design

### `Users`

- `Id` integer primary key.
- `FullName` required, maximum 100.
- `Email` required, normalized lowercase, maximum 200, unique index.
- `PasswordHash` required.
- `Role` required, maximum 20; only `Student` or `Admin`.
- `GoogleSubject` nullable, maximum 100, unique index.

### `Services`

- `Id` integer primary key.
- `Name` required, maximum 100.
- `Description` required, maximum 500.

### `Requests`

- `Id` integer primary key.
- `StudentId` required foreign key to Users with restricted deletion.
- `ServiceId` required foreign key to Services with restricted deletion.
- `Note` nullable, maximum 1000.
- `Status` required, maximum 20, default `Submitted`.
- `AdminComment` nullable, maximum 1000.
- `CreatedAt` UTC timestamp.
- `CamundaProcessInstanceId` nullable, maximum 100.

Valid status values are exactly `Submitted`, `Approved`, and `Rejected`.

### `PendingRegistrations`

- `Id` integer primary key.
- `FullName`, `Email`, `PasswordHash`, and `CodeHash` required.
- `Email` unique and maximum 200.
- `CodeHash` maximum 64.
- `ExpiresAt`, `LastSentAt`, and `FailedAttempts`.
- Nullable unique `GoogleSubject` for pending Google onboarding.

### `PasswordResets`

- `Id` integer primary key.
- Unique required `Email`, maximum 200.
- Required `CodeHash`, maximum 64.
- `ExpiresAt`, `LastSentAt`, and `FailedAttempts`.

Provide EF Core migrations and an importable SQL file that create the same schema and migration history. At API startup, apply migrations and seed data only when the relevant tables are empty.

Seed these development records:

| Role | Email | Password |
|---|---|---|
| Student | `student@school.com` | `Student123!` |
| Admin | `admin@school.com` | `Admin123!` |

Seed services:

- Enrollment Certificate — Official certificate proving student enrollment.
- Grade Transcript — Official academic grade transcript.
- Attendance Certificate — Official attendance certificate.

Hash seeded passwords; never store plaintext passwords.

## 7. Backend API contract

### Public authentication

- `POST /api/auth/register` — validate details, create/update pending registration, and send code; return 202.
- `POST /api/auth/verify-email` — verify the code, create/link Student, and return 201 with JWT/user.
- `POST /api/auth/login` — return JWT/user or 401 with a generic invalid-credentials message.
- `POST /api/auth/forgot-password` — request reset code and return a neutral 202 response.
- `POST /api/auth/reset-password` — verify reset code and update password.
- `GET /api/auth/google-config` — return `{ clientId }`, using an empty value when not configured.
- `POST /api/auth/google` — verify credential, sign in a linked Student, or start verified onboarding.

### Authenticated profile

- `GET /api/profile` — return current user DTO.
- `PUT /api/profile/password` — securely change current user's password.

### Student

- `GET /api/services` — list available services alphabetically.
- `POST /api/requests` — create request and start workflow.
- `GET /api/requests/mine` — list only the authenticated Student's requests newest first.

### Admin

- `GET /api/admin/requests` — list all requests newest first with Student data.
- `POST /api/admin/requests/{id}/approve` — complete workflow and approve.
- `POST /api/admin/requests/{id}/reject` — complete workflow and reject.
- `GET /api/admin/services` — list all services.
- `POST /api/admin/services` — create a unique service.
- `DELETE /api/admin/services/{id}` — remove an unused service.

Use these general HTTP semantics:

- 200 for successful reads/actions, 201 for created resources/accounts, 202 for email-code initiation, and 204 for deletion.
- 400 for invalid input or expired/incorrect codes.
- 401 for invalid/missing authentication, 403 for the wrong role, 404 for missing resources, 409 for state/uniqueness conflicts, 429 for resend throttling, and 503 for unavailable external services.
- Validation and expected failures return `{ message: "..." }`.
- A global exception handler returns a generic HTTP 500 message without stack traces.

## 8. Security requirements

- Use ASP.NET Core `PasswordHasher<User>` for every local password.
- Hash verification codes with HMAC-SHA256 using a server secret and compare hashes in constant time.
- JWTs include user ID, name, email, and role, expire after two hours, and use issuer/audience/signature/lifetime validation with at most one minute clock skew.
- Require a JWT signing key of at least 32 bytes.
- Configure CORS for only the frontend origin.
- Normalize email consistently before all lookups.
- Do not expose whether a password-recovery email belongs to a user.
- Do not commit real database credentials, JWT secrets, Google client IDs, or Gmail App Passwords.
- Use cancellation tokens for database and external calls where appropriate.
- Keep external error bodies and stack traces out of client responses.

## 9. Frontend routes and pages

### Public routes

- `/login` — branded two-column login, Google sign-in, empty email/password inputs, forgot-password link, and link to email registration.
- `/register` — guarded so it is entered from login; Google option, details step, and six-digit verification step.
- `/forgot-password` — email step followed by code/new-password step.

### Student routes

- `/services` — greeting, catalog heading/count, and responsive service-card grid.
- `/submit-request/:serviceId` — service details, optional note form, counter, and workflow explanation.
- `/my-requests` — summary statistics and request history with formatted IDs/dates, statuses, notes, and admin comments.

### Admin routes

- `/admin/requests` — workflow indicator, statistics, and responsive decision cards.
- `/admin/services` — create-service form plus removable service list.

### Shared authenticated route

- `/profile` — read-only identity details and password-change form.

### Routing behavior

- `/` redirects to login, and unknown paths redirect through `/`.
- Unauthenticated protected access redirects to login.
- A signed-in user with the wrong role redirects to their correct home route.
- Hide the navigation on login and registration.
- Navigation for Students: Services, My Requests, Profile, Logout, user identity.
- Navigation for Admins: Admin Requests, Manage Services, Profile, Logout, user identity.

## 10. Frontend state and API behavior

- Centralize session behavior in an authentication context and `useAuth` hook.
- Centralize Axios base URL, bearer-token injection, 401 handling, and backend message extraction.
- Default API URL: `http://localhost:5001/api`; support `VITE_API_URL` override.
- Avoid duplicate submissions with loading/processing state and disabled buttons.
- Preserve clear inline error and success messages.
- Include spinners/loading panels while fetching.
- Include useful empty states for no services or requests.
- Reload affected data after admin create/delete/decision operations.
- Use client validation for fast feedback while treating backend validation as authoritative.
- Code inputs accept digits only, have a length of six, and disable verification until complete.

## 11. Visual and responsive quality

The result must not look like an unstyled CRUD prototype. Create one coherent design system in custom CSS:

- A school-portal visual identity with consistent navy/blue accent colors, neutral surfaces, typography, spacing, borders, shadows, and radii.
- Branded authentication pages with an informational story panel on wide screens and compact branding on small screens.
- A responsive max-width page container and polished navigation bar.
- Reusable buttons, labels, inputs, textareas, messages, badges, avatars, icons, cards, panels, statistics, and empty/loading states.
- Status badges with distinct accessible styling for Submitted, Approved, and Rejected.
- Responsive grids that collapse cleanly for tablets and phones.
- Forms must remain usable without horizontal scrolling.
- Buttons need hover, focus, disabled, primary, approval, and danger states.
- Use semantic labels, required attributes, sensible autocomplete values, and visible keyboard focus.
- Use a reusable inline SVG icon component or equivalent consistent icon system.
- Ensure long notes, email addresses, comments, and service names wrap without breaking layouts.

## 12. Configuration and local startup

Provide checked-in development placeholders and document environment overrides for:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience`
- `FrontendUrl`
- `Camunda:BaseUrl` and `Camunda:ProcessDefinitionKey`
- `Google:ClientId`
- `Gmail:Username`, `Gmail:AppPassword`, and `Gmail:FromName`
- Frontend `VITE_API_URL`

Expected local defaults:

- Frontend: `http://localhost:3000`
- Backend: `http://localhost:5001`
- Camunda REST: `http://localhost:8080/engine-rest/`
- XAMPP MariaDB: `127.0.0.1:3306`, database `school_paper_requests`

Include a root `npm start` script/helper that launches frontend and backend together and stops both when interrupted. Document that MySQL and Camunda must already be running. The README must explain database import/migrations, Gmail App Password configuration, Google OAuth authorized origin, Camunda BPMN deployment, environment variables, startup, development accounts, tests, and the complete manual workflow.

## 13. Implementation sequence

### Phase 1 — Foundation

1. Scaffold React/Vite and ASP.NET Core projects.
2. Install routing, Axios, EF Core, MariaDB provider, JWT, Identity password hashing, and test packages.
3. Add configuration, CORS, global error handling, DI interfaces, and one-command startup.
4. Create the responsive design foundation and shared components.

### Phase 2 — Database and core authentication

1. Create all five entities, relationships, indexes, migrations, and SQL import.
2. Add automatic migration and development seeding.
3. Implement JWT service, password hashing, login, auth context, Axios interceptors, protected routes, and role redirects.

### Phase 3 — Registration and recovery

1. Implement pending email registration, Gmail sender, secure codes, expiry, throttling, and attempt limits.
2. Build registration details and verification interfaces.
3. Add Google Identity Services, backend token verification, safe account linking, and first-use verification.
4. Implement forgot/reset password and profile password change.

### Phase 4 — Student catalog and requests

1. Implement Student service listing and polished catalog cards.
2. Implement request form and validation.
3. Deploy BPMN and implement Camunda start/task/cancel operations.
4. Add transactional request creation and compensation.
5. Build My Requests, status badges, statistics, comments, and empty/loading states.

### Phase 5 — Admin workspace

1. Implement all-request listing and DTO projection.
2. Implement approve/reject with Camunda-first consistency and one-decision rule.
3. Build admin statistics, decision cards, optional comments, and completed states.
4. Implement service creation/deletion rules and management UI.

### Phase 6 — Hardening and verification

1. Test authorization boundaries and ownership rules.
2. Test normal, empty, invalid, repeated, unavailable-integration, and mobile-layout states.
3. Run backend tests and frontend production build.
4. Perform the full manual flow with MySQL and Camunda.
5. Finish README and confirm no secrets are committed.

## 14. Automated test requirements

At minimum, add backend tests for:

- Registration creates only a pending record until code verification.
- Valid verification creates a Student with a hashed password and returns a session.
- Existing email registration returns conflict.
- Correct login returns token and role; wrong password returns unauthorized.
- Forgot-password code changes the password and consumes the reset record.
- Invalid/expired codes and attempt limits behave correctly.
- Google cannot sign in an Admin or link conflicting identities.
- Students retrieve only their own requests.
- Request creation starts workflow and stores the process ID.
- Workflow-start failure rolls back request creation.
- Admin approves/rejects a submitted request only once.
- Missing requests and missing workflow IDs return appropriate errors.
- Workflow-decision failure does not change database status.
- Admin can add and remove an unused service.
- Duplicate service names conflict.
- A referenced service cannot be removed.
- Profile password change validates current/new passwords and Google-only restrictions.

Use fakes for email, Google verification, token creation where suitable, and Camunda. Tests must not depend on live external services.

## 15. Final manual acceptance checklist

### Authentication and account

- [ ] Login fields are empty when the page first opens.
- [ ] Seeded Student and Admin can sign in and reach the correct dashboard.
- [ ] Invalid credentials show a safe error.
- [ ] Email registration requires a valid code before account creation.
- [ ] Registration codes expire, throttle resends, and enforce the attempt limit.
- [ ] Google can register/sign in Students and cannot access Admin accounts.
- [ ] Forgot password sends a code without revealing account existence.
- [ ] Both roles can view profile details and change a local password.
- [ ] Unauthorized and wrong-role navigation is blocked by both UI and API.
- [ ] Logout and expired/invalid-token behavior clear the session.

### Student

- [ ] Services load with accurate count and responsive cards.
- [ ] A Student can open a service and submit a note up to 1000 characters.
- [ ] Submission starts Camunda and stores its process instance ID.
- [ ] Camunda failure leaves no orphan database request.
- [ ] My Requests contains only that Student's records, newest first.
- [ ] Status statistics, notes, dates, and admin comments render correctly.

### Admin

- [ ] All requests load newest first with student and service details.
- [ ] Statistics correctly count all statuses.
- [ ] Admin can approve or reject a Submitted request with an optional comment.
- [ ] The Camunda task completes and the Student sees the updated result.
- [ ] A completed request cannot be processed again.
- [ ] Admin can add a unique service and Students see it.
- [ ] Admin can delete an unused service after confirmation.
- [ ] Admin cannot delete a service referenced by any request.

### Quality and delivery

- [ ] Desktop, tablet, and mobile layouts are polished and usable.
- [ ] Every asynchronous view has loading, error, and applicable empty/success states.
- [ ] API failures do not expose stack traces or secrets.
- [ ] Database migrations and SQL import represent the same schema.
- [ ] The production frontend build succeeds.
- [ ] All automated backend tests pass.
- [ ] README setup can be followed from a clean local environment.

The project is finished only when every applicable checkbox passes and the complete Student → Camunda → Admin → Student workflow succeeds.
