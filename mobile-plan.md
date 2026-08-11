# School Paper Requests Mobile Application Plan

## 1. Goal

Create a production-quality mobile application for the existing School Paper Requests system.

The mobile application must use:

- **Mobile frontend:** React Native with Expo and JavaScript.
- **Backend:** ASP.NET Core Web API with C#.
- **Database:** The existing XAMPP MariaDB/MySQL `school_paper_requests` database.
- **Authentication:** The existing JWT, email verification, Google authentication, and password-recovery APIs.
- **Workflow:** The existing Camunda Platform 7 integration owned by the backend.

The new mobile app must provide the same functional level as the existing React web app. It must reuse the same database schema, business rules, roles, status values, and API routes so web and mobile users see the same accounts, services, and requests.

Do not replace or break the existing web frontend. The web application and mobile application must be able to run against the same backend and database.

## 2. Project boundary

The preferred structure is:

```text
Backend/                    Existing shared ASP.NET Core API
frontend/                   Existing React web application
mobile/                     New Expo React Native application
database/                   Existing schema/import files
Backend.Tests/              Existing and additional backend tests
```

Implementation rules:

1. Reuse the existing backend wherever possible.
2. Do not create a second database or duplicate backend solely for mobile.
3. Do not rename or remove existing API endpoints or response fields.
4. Backend changes are allowed only when needed to support mobile safely, such as adding the Expo development origin, mobile Google OAuth configuration, or backward-compatible error handling.
5. Any backend change must remain compatible with the existing web frontend.
6. React Native communicates only with the ASP.NET Core API. It never connects directly to MySQL, Gmail, Google token verification, or Camunda.
7. Never put database passwords, JWT signing keys, Gmail App Passwords, or other server secrets in the mobile bundle.

## 3. Technology choices

### Mobile

- Expo with the current stable SDK.
- React Native and JavaScript.
- Expo Router for file-based navigation.
- Axios for API calls.
- React Context plus hooks for authentication state.
- `expo-secure-store` for the JWT and cached user session.
- `expo-auth-session` and `expo-web-browser` for Google OAuth when Google sign-in is configured.
- `@react-native-async-storage/async-storage` only for non-sensitive preferences if needed; do not store the JWT there.
- Expo/vector icons or a consistent icon library supported by Expo.
- Safe-area handling through `react-native-safe-area-context`.
- Keyboard-aware forms using `KeyboardAvoidingView`, `ScrollView`, and appropriate keyboard settings.

### Shared server

- Existing ASP.NET Core Web API and C# code.
- Existing EF Core MariaDB entities and migrations.
- Existing JWT, Gmail, Google verification, and Camunda services.

Do not add a large mobile UI framework unless it materially improves consistency. Prefer reusable React Native components and a centralized theme.

## 4. Existing backend and database contract

The mobile app must work with the existing database tables:

- `Users`
- `Services`
- `Requests`
- `PendingRegistrations`
- `PasswordResets`

It must preserve these roles:

- `Student`
- `Admin`

It must preserve these request statuses exactly:

- `Submitted`
- `Approved`
- `Rejected`

The mobile app must not run EF migrations. Only the backend owns database migrations and startup seeding.

The mobile app must consume the following existing endpoints without changing their contract.

### Public authentication endpoints

- `POST /api/auth/register`
- `POST /api/auth/verify-email`
- `POST /api/auth/login`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/auth/google-config`
- `POST /api/auth/google`

### Authenticated profile endpoints

- `GET /api/profile`
- `PUT /api/profile/password`

### Student endpoints

- `GET /api/services`
- `POST /api/requests`
- `GET /api/requests/mine`

### Admin endpoints

- `GET /api/admin/requests`
- `POST /api/admin/requests/{id}/approve`
- `POST /api/admin/requests/{id}/reject`
- `GET /api/admin/services`
- `POST /api/admin/services`
- `DELETE /api/admin/services/{id}`

Expected login/verification response:

```json
{
  "token": "jwt-token",
  "user": {
    "id": 1,
    "fullName": "Sample Student",
    "email": "student@school.com",
    "role": "Student"
  }
}
```

Expected error shape:

```json
{
  "message": "Human-readable error message"
}
```

The mobile app must display the backend `message` when available and use a safe fallback for network or unexpected errors.

## 5. Mobile roles and features

### Student features

1. Register with name, email, password, and password confirmation.
2. Verify registration using the six-digit email code.
3. Register or sign in with Google when configured.
4. Log in with email and password.
5. Request and complete a forgot-password flow.
6. View all available school-paper services.
7. Open a service and submit a request with an optional note.
8. View only their own requests.
9. See request status, submission date, original note, and admin comment.
10. View their profile and change a local account password.
11. Log out securely.

### Admin features

1. Log in with email and password.
2. View all student requests and status totals.
3. Open or expand a request to inspect its details.
4. Approve or reject a Submitted request with an optional comment.
5. View completed decisions and comments.
6. View all services.
7. Add a unique service.
8. Remove an unused service after confirmation.
9. View profile and change password.
10. Log out securely.

## 6. Navigation architecture

Use Expo Router with public and protected route groups.

Recommended route structure:

```text
mobile/app/
  _layout.js
  index.js
  (auth)/
    _layout.js
    login.js
    register.js
    verify-email.js
    forgot-password.js
    reset-password.js
  (student)/
    _layout.js
    services/
      index.js
      [id].js
    requests/
      index.js
  (admin)/
    _layout.js
    requests/
      index.js
      [id].js
    services/
      index.js
      create.js
  profile.js
```

Navigation requirements:

- Show an initialization/splash state while restoring the secure session.
- Unauthenticated users can access only the auth group.
- Students cannot enter Admin routes.
- Admins cannot enter Student routes.
- After login, route Students to Services and Admins to Admin Requests.
- Use role-specific tab navigation for main areas.
- Student tabs: Services, My Requests, Profile.
- Admin tabs: Requests, Services, Profile.
- Use stack navigation for service details, request submission, request detail, registration verification, password reset, and service creation.
- Hardware and gesture back navigation must behave naturally.
- Logout clears secure credentials and resets navigation so protected screens are not available through Back.

## 7. Authentication and secure session design

Create an authentication provider with these states:

- `initializing`
- `authenticated`
- `unauthenticated`

It must expose at least:

- `user`
- `token`
- `login(session)`
- `logout()`
- `restoreSession()`

Session requirements:

1. Store the JWT in Expo SecureStore.
2. Store the cached user in SecureStore or another appropriately protected store.
3. Restore both values during app startup before choosing a route.
4. Configure Axios to attach `Authorization: Bearer <token>`.
5. On HTTP 401, clear the session and return to login.
6. Avoid redirect loops when several requests fail with 401 simultaneously.
7. Never log JWTs, passwords, Google credentials, or verification codes.
8. Password fields must start empty and use secure text entry.
9. Support show/hide password controls without changing stored form values.
10. Disable repeated submission while an authentication request is running.

The backend JWT expires after two hours. No refresh-token endpoint currently exists, so expiration must sign the user out cleanly rather than attempting an unsupported refresh.

## 8. API and networking layer

Create one reusable Axios client in the mobile project.

Requirements:

- Read the API base URL from `EXPO_PUBLIC_API_URL`.
- Do not hardcode `localhost` as the only address.
- For a physical device, document that the API URL must use the computer's LAN IP, for example `http://192.168.1.10:5001/api`.
- For Android Emulator, document `http://10.0.2.2:5001/api` when appropriate.
- For iOS Simulator, `http://localhost:5001/api` may be used when supported by the local setup.
- Provide a clear network-error message when the device cannot reach the backend.
- Set reasonable request timeouts.
- Centralize backend message extraction.
- Do not duplicate request logic inside every screen; create small API modules for auth, profile, services, and requests.
- Cancel or ignore state updates when a screen unmounts during a request.
- Refresh lists when a screen regains focus after creating or changing data.

Development networking must bind the ASP.NET API to an address reachable from the device, such as `http://0.0.0.0:5001`, and the computer firewall may need to allow the development port. Document this clearly.

## 9. Authentication screens

### Login screen

- Branded School Requests header and short explanatory copy.
- Empty email and password fields on every clean launch of the screen.
- Email keyboard, lowercase behavior, and no unwanted capitalization.
- Secure password input with show/hide control.
- Sign-in button with loading state.
- Continue with Google when configured and supported.
- Links to Create Account and Forgot Password.
- Inline backend/network error message.
- Successful role-based navigation.

### Register screen

- Full name, email, password, and confirm password.
- Client validation matching backend limits.
- Password guidance: 8–100 characters with uppercase, lowercase, and a number.
- Continue with Google option.
- Submit details to `/auth/register` and navigate to verification without losing the email.
- Prevent accidental duplicate submissions.

### Verify email screen

- Explain which email received the code and the 10-minute expiration.
- Six-digit numeric input.
- Allow paste and automatically remove non-digits.
- Disable verification until six digits are entered.
- On success, securely save the returned session and open the Student app.
- Allow returning to edit registration details.
- Display expiration, throttling, and attempt-limit errors from the backend.

### Forgot/reset password screens

- Forgot Password collects email and uses the neutral server response.
- Reset Password collects six-digit code, new password, and confirmation.
- Validate confirmation before submitting.
- On success, return to Login and show the server success message.
- Do not reveal whether an email exists through client wording.

### Google sign-in

- Use Expo AuthSession/WebBrowser with platform-specific Google OAuth client configuration.
- Obtain the Google ID token/credential required by `POST /api/auth/google`.
- Never trust profile data only from the device; the backend must verify the token.
- Existing linked Students sign in immediately.
- First-time Google Students proceed to the six-digit verification UI when the backend returns `requiresVerification` and `email`.
- Hide or gracefully disable Google sign-in when the backend/client configuration is absent.
- Do not attempt to use the web-only Google Identity Services browser script inside React Native.
- Document Expo Go limitations. If the selected Google library requires native configuration, provide a development build configuration and instructions.

## 10. Student mobile experience

### Services screen

- Friendly greeting using the Student's first name.
- Pull-to-refresh service list.
- Service count.
- Responsive cards with icon, name, description, and Request action.
- Loading skeleton/spinner, error with Retry, and empty state.
- Cards must support long names and descriptions without clipping.

### Service/request screen

- Load the selected service by using the service passed through navigation or by fetching the list and finding its ID.
- Display service name and description.
- Optional multiline note with a 1000-character maximum and visible counter.
- Explain the three workflow stages: submitted, administration review, decision.
- Submit `{ serviceId, note }` to the existing endpoint.
- Disable controls while submitting.
- On success, replace/navigate to My Requests with success feedback.
- On workflow HTTP 503, clearly state that the request was not submitted.

### My Requests screen

- Pull-to-refresh and refresh-on-focus.
- Summary cards for total, under review, and approved.
- Requests ordered by the server response, newest first.
- Each card shows a padded request number, service, Student note, localized date, status badge, and admin comment.
- Use distinct accessible styling for Submitted, Approved, and Rejected.
- Provide loading, Retry, empty, and success states.
- Keep long notes and comments readable using wrapping or expandable text.

## 11. Admin mobile experience

### Admin Requests screen

- Pull-to-refresh and refresh-on-focus.
- Summary cards for all, awaiting decision, approved, and rejected.
- List all records with status, Student name/email, service, and date.
- Use a dedicated detail screen or expandable card for note, comment, and actions.
- Only Submitted requests show decision actions.
- Let the Admin enter an optional comment up to 1000 characters.
- Require a native confirmation alert before Approve or Reject.
- Prevent both buttons from being used while a decision is processing.
- Reload the request after a successful decision.
- Completed requests show the stored decision comment and no action buttons.
- Display HTTP 409 for an already processed request and refresh its current state.
- Display HTTP 503 without changing the local status when Camunda is unavailable.

### Manage Services screen

- Pull-to-refresh alphabetized service list.
- Service count, loading state, Retry, and empty state.
- Add Service action opens a focused form screen.
- Create form validates name 2–100 and description 2–500.
- Show backend conflict errors for duplicate names.
- Each service has a Remove action with native confirmation.
- Disable the row while removal is processing.
- On HTTP 409, explain that the service is already referenced and cannot be removed.
- Refresh the list after create/delete.

## 12. Profile screen

- Available to both roles.
- Show avatar/initial, full name, email, and role.
- Treat identity fields as read-only because the API does not support editing them.
- Provide Current Password, New Password, and Confirm New Password fields.
- Apply secure entry, show/hide controls, and keyboard-safe scrolling.
- Validate matching new passwords and standard strength rules.
- Clear all password fields after success.
- Show the backend message when the current password is wrong, the new password is unchanged, or the account is Google-only.
- Place Logout in an obvious but non-accidental location and confirm if appropriate.

## 13. Reusable mobile components

Create reusable components instead of copying UI logic between screens:

- `Screen` or `SafeScreen` wrapper.
- App header and role-aware navigation elements.
- `FormField` with label, error, password visibility, and accessibility support.
- Primary, secondary, approval, and danger buttons.
- `StatusBadge` for the three exact statuses.
- `ServiceCard`.
- `RequestCard`.
- `StatCard`.
- Loading indicator or skeleton.
- Error state with Retry.
- Empty state.
- Inline success/error banner.
- Confirmation helper using React Native `Alert`.

Centralize colors, spacing, typography, radii, shadows/elevation, and component sizes in a theme file.

## 14. Design and mobile quality requirements

The app must feel like a deliberate mobile product, not a web page copied into React Native.

- Use the same School Requests identity and navy/blue visual direction as the web app.
- Design touch targets at least 44×44 points.
- Respect safe areas, notches, status bars, and bottom navigation areas.
- Use platform-appropriate scrolling and pull-to-refresh.
- Keep forms visible when the keyboard opens.
- Use `FlatList` for request/service collections rather than rendering large lists in a plain `ScrollView`.
- Provide light-theme consistency. Add dark mode only if it is completed across every screen.
- Support small phones and larger tablets without horizontal scrolling.
- Use accessible color contrast and never communicate status through color alone.
- Add accessibility labels/roles/hints to controls and meaningful icons.
- Support dynamic text reasonably without cutting off critical actions.
- Use localized device date/time display while preserving UTC server values.
- Avoid unsupported web elements, CSS, browser globals, and localStorage.
- Avoid unnecessary animations. Any animation should be short and respect reduced-motion preferences where possible.

## 15. Backend compatibility work

Before changing backend code, test the mobile app against the existing endpoints. If changes are required, they must be additive and backward compatible.

Potential required work:

1. Configure Kestrel development URLs so a physical device can reach the API over the LAN.
2. Keep web CORS configuration intact. Note that native HTTP clients do not rely on browser CORS, while Expo Web does.
3. If supporting Expo Web, add its exact development/production origin rather than allowing every origin.
4. Extend Google configuration to support Android/iOS OAuth client IDs without removing the existing web client ID.
5. Make Google token verification accept only explicitly configured valid audiences for the supported clients.
6. Keep all current endpoint routes, request DTOs, response DTOs, status codes, and web behavior.
7. Add tests for any modified configuration or Google-audience logic.

Do not weaken authentication, CORS, token validation, Google audience checks, or TLS expectations to make development easier.

## 16. Environment configuration

Create `mobile/.env.example` with non-secret public configuration:

```text
EXPO_PUBLIC_API_URL=http://YOUR_COMPUTER_LAN_IP:5001/api
EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID=
EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID=
EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID=
```

Only values safe to expose in a compiled mobile app may use the `EXPO_PUBLIC_` prefix. OAuth client IDs are identifiers, not secrets. Gmail App Passwords, JWT keys, database credentials, and Google client secrets remain exclusively in backend environment variables or .NET user secrets.

If production deployment is in scope:

- Use an HTTPS API URL.
- Configure Android package name and iOS bundle identifier.
- Configure matching Google OAuth clients and redirect URIs.
- Create `eas.json` for development, preview, and production builds.
- Add app name, icon, splash image, adaptive Android icon, version, and platform permissions in Expo configuration.
- Request no device permissions that the app does not need.

## 17. Error and offline behavior

- Distinguish validation/API errors from unreachable-server errors.
- Show a useful Retry action for failed list requests.
- Do not silently treat failed writes as successful.
- Do not optimistically change an Admin decision before the server confirms Camunda completion.
- Preserve typed form content after a recoverable server/network error.
- Avoid automatic duplicate POST retries.
- When the device is offline, explain that a connection is required.
- The app does not need full offline data synchronization in this version.
- Handle malformed or missing cached session data by clearing it and returning to login.

## 18. Testing requirements

### Mobile unit/component tests

Use Jest and React Native Testing Library. Test at minimum:

- Login validation, loading, successful role routing, and error display.
- Secure session restoration and logout.
- Axios bearer-token injection and single logout behavior on 401.
- Protected route behavior for Student, Admin, and unauthenticated states.
- Registration and six-digit verification behavior.
- Forgot/reset password validation.
- Service loading, Retry, empty state, and request navigation.
- Request form character limit and successful submission.
- My Requests counts and status rendering.
- Admin request action availability only for Submitted requests.
- Admin decision confirmation, processing state, conflict, and failure behavior.
- Service creation validation and deletion conflict handling.
- Profile password validation and success clearing.

Mock network calls, SecureStore, Expo Router, Google authentication, and native alerts. Unit tests must not call a live backend.

### Backend regression tests

- Run the existing backend suite unchanged.
- Add tests for any backward-compatible backend modifications.
- Confirm the web frontend production build still succeeds after backend work.

### Manual device testing

Test on at least:

- Android emulator or physical Android device.
- iOS simulator/device when the development machine supports it.
- A small phone viewport and a tablet-sized viewport.

Verify keyboard behavior, safe areas, Back behavior, token expiration, slow network, server unavailable, repeated taps, long content, and pull-to-refresh.

## 19. Implementation phases

### Phase 1 — Analyze and scaffold

1. Read the current backend DTOs, controllers, authentication, database schema, and web behavior.
2. Confirm all endpoint contracts with direct API tests.
3. Scaffold `mobile/` as an Expo app without altering `frontend/`.
4. Configure Expo Router, SafeAreaProvider, theme, environment variables, linting, formatting, and tests.

### Phase 2 — Networking and authentication foundation

1. Build the Axios client, API modules, safe error extraction, and timeout behavior.
2. Build SecureStore session persistence and authentication provider.
3. Build initialization, public/protected route groups, role guards, and logout.
4. Implement Login and verify Student/Admin routing.

### Phase 3 — Account lifecycle

1. Build email registration and verification.
2. Build forgot/reset password.
3. Configure mobile Google OAuth and first-use email verification.
4. Build shared Profile and password change.

### Phase 4 — Student application

1. Build Student tabs and responsive navigation.
2. Build service catalog and details/request form.
3. Build My Requests statistics, list, status display, comments, refresh, and empty/error states.
4. Test a real request against the shared backend and Camunda process.

### Phase 5 — Admin application

1. Build request dashboard, statistics, list, and detail.
2. Build approve/reject confirmation and comment flow.
3. Build service list, creation, confirmation, and safe deletion.
4. Verify changes immediately appear in both mobile and web clients.

### Phase 6 — Quality and delivery

1. Complete reusable components, responsive layouts, accessibility, and keyboard behavior.
2. Add automated mobile tests and run backend regression tests.
3. Validate Android and, where possible, iOS behavior.
4. Add mobile setup/run/build documentation.
5. Confirm no secrets or generated build artifacts are committed.

## 20. Documentation deliverables

Update the root README or add `mobile/README.md` with:

- Required Node, Expo, .NET, XAMPP, and Camunda versions.
- Mobile dependency installation.
- Backend/database/Camunda startup order.
- How to determine the computer's LAN IP.
- Emulator, simulator, physical-device, and Expo Go API URL examples.
- Firewall and Kestrel binding guidance.
- `.env` configuration.
- Google OAuth configuration for web, Android, and iOS.
- How to run Expo with `npx expo start`.
- How to run tests and linting.
- How to create an Expo development build if Google login needs native configuration.
- EAS preview/production build instructions if configured.
- Development Student/Admin accounts.
- A complete shared web/mobile workflow test.

## 21. Final acceptance checklist

### Shared compatibility

- [ ] Mobile uses the existing ASP.NET Core API and MariaDB database.
- [ ] No existing endpoint or database contract was broken.
- [ ] Existing React web frontend still builds and works.
- [ ] Data created on mobile appears on web and data created on web appears on mobile.
- [ ] Backend tests pass.

### Authentication

- [ ] App restores a valid session from SecureStore without flashing the wrong route.
- [ ] Login fields start empty and support secure password entry.
- [ ] Student and Admin route to different protected tab groups.
- [ ] Email registration and six-digit verification work.
- [ ] Google Student registration/login works when configured.
- [ ] Forgot/reset password works without revealing account existence.
- [ ] HTTP 401 and token expiration securely clear the session.
- [ ] Logout prevents returning to protected screens with Back.

### Student

- [ ] Services display with loading, Retry, empty, and pull-to-refresh states.
- [ ] Student can submit a service request with an optional note.
- [ ] Camunda starts the existing workflow through the backend.
- [ ] Student sees only their own requests and correct status counts.
- [ ] Status, note, date, and admin comment render correctly.

### Admin

- [ ] Admin sees all requests and accurate status statistics.
- [ ] Admin can inspect a request and approve or reject it once.
- [ ] Optional comments are stored and visible to the Student.
- [ ] Camunda failure does not falsely update the mobile UI.
- [ ] Admin can add a service and remove only an unused service.

### Mobile quality

- [ ] Navigation, hardware Back, keyboard, and safe areas work correctly.
- [ ] Screens work on small phones and tablet-sized displays.
- [ ] Lists use efficient React Native list components.
- [ ] Interactive controls have accessible labels and adequate touch targets.
- [ ] Loading, empty, success, disabled, confirmation, offline, and error states are polished.
- [ ] No server secret exists in Expo configuration or the JavaScript bundle.
- [ ] Mobile tests pass and Expo starts without warnings that indicate broken functionality.
- [ ] Documentation is sufficient for another developer to run the entire system.

## 22. Complete end-to-end scenario

The project is finished when this scenario succeeds:

1. Start XAMPP MySQL, Camunda 7, and the shared ASP.NET backend.
2. Start the Expo mobile application on a device or emulator.
3. Register a new Student and verify the emailed six-digit code.
4. View services and submit a request with a note.
5. Confirm the request appears as Submitted in the mobile Student app and in the existing web app.
6. Confirm Camunda contains the process instance and `adminReview` task.
7. Log in as Admin on mobile, add a decision comment, and approve or reject the request.
8. Confirm Camunda completes the task.
9. Log back in as the Student and confirm the new status/comment.
10. Confirm the same result appears in the existing web frontend.
11. Add an unused service as Admin, see it as a Student, then remove it safely.
12. Run mobile tests, backend tests, and the existing web production build successfully.

Only after all applicable acceptance items pass should the mobile project be considered complete.
