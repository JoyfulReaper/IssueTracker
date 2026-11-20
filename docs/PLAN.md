# Issue Tracker — MVP Plan

> **Status:** Proposed plan for Kyle (JoyfulReaper) and stormchiller to review together. This is a learning and portfolio project, not a rigid specification. Make decisions together and keep the first working version small.

## 1. Goal

Build a small issue/ticket tracking web application. Users can register, create projects, invite existing users into those projects, and create, update, assign, and track issues. The project should teach practical full-stack development, authorization, testing, deployment, and GitHub collaboration.

**Definition of MVP success:** Two users can register, collaborate on a project with different permissions, and manage assigned tickets through the Angular UI. Important authorization behavior has automated tests.

## 2. Proposed stack

| Layer | Choice |
| --- | --- |
| Frontend | Angular + TypeScript |
| Backend | ASP.NET Core Web API + C# |
| Database | SQLite initially |
| Data access | Entity Framework Core + migrations |
| Accounts | ASP.NET Core Identity |
| Authorization | Project-specific membership and permission checks |
| Backend tests | xUnit + ASP.NET Core integration tests |
| Frontend tests | Angular's configured test tooling |
| Collaboration | GitHub issues, feature branches, pull requests |
| Deployment | Decide after the core app and auth work locally |

We will use a **single backend application** and a separate Angular frontend. Do not add microservices, a generic repository layer, CQRS, message queues, or other infrastructure without a concrete reason.

### Decisions already agreed

- Authentication and basic project-specific roles belong in the MVP.
- Issues belong to exactly one project.
- An issue can be **unassigned or assigned to one individual user**. Assignment to a team/role is a later possibility, **not an MVP requirement**.
- An assignee must be an eligible member of the issue's project.
- Project roles are **Admin**, **Member**, and **Viewer**; they are not global Identity roles.
- The creator of a project becomes its first Admin.
- Removing a member unassigns their issues in that project, but does not delete those issues.

### Decisions to make before implementing authentication

- **Browser auth transport:** Prefer secure, HttpOnly cookie-based authentication with a same-origin frontend/API deployment where practical. Decide the actual cookie, CSRF, and hosting design before implementing login. Do not place long-lived bearer credentials in local storage by default.
- **Registration:** Open registration for the learning MVP, or admin-created accounts? Open registration is simplest locally; revisit abuse protection before public deployment.
- **Project visibility:** Recommendation: projects and their issues are private to project members. No public projects in the MVP.
- **Project deletion:** Recommendation: require Admin confirmation; explicitly decide whether to cascade-delete all related issues or disallow deletion while issues exist.

## 3. MVP data model

### User

Managed by ASP.NET Core Identity. Use Identity's stable user ID for relationships. We do not implement our own password storage or login cryptography.

### Project

| Field | Notes |
| --- | --- |
| Id | Primary key |
| Name | Required, bounded length |
| Description | Optional |
| CreatedAtUtc | Server-generated |

### ProjectMembership

| Field | Notes |
| --- | --- |
| ProjectId | Foreign key to Project |
| UserId | Foreign key to Identity user |
| Role | Admin, Member, or Viewer |

Use a unique constraint on `(ProjectId, UserId)` so a user has only one membership/role per project. Membership changes must not leave a project without an Admin: decide whether to block removal/demotion of the last Admin.

### Issue

| Field | Notes |
| --- | --- |
| Id | Primary key |
| ProjectId | Required foreign key |
| Title | Required, bounded length |
| Description | Optional |
| Status | Open, InProgress, Resolved, Closed |
| Priority | Low, Medium, High, Critical |
| AssignedToUserId | Nullable Identity user ID |
| CreatedAtUtc | Server-generated |
| UpdatedAtUtc | Server-generated on updates |

The frontend cannot simply submit arbitrary owner/project/role fields and assume the server will trust them. The backend must check membership and permissions for **every** relevant operation, including reads. Use explicit request/response DTOs rather than binding EF Core entities directly from client input.

## 4. Permissions (proposed MVP policy)

| Operation | Admin | Member | Viewer |
| --- | --- | --- | --- |
| View project and issues | Yes | Yes | Yes |
| Create and edit issues | Yes | Yes | No |
| Assign/unassign issues | Yes | Yes | No |
| Delete issues | Yes | No | No |
| Edit project details | Yes | No | No |
| Add/remove project members | Yes | No | No |
| Change project roles | Yes | No | No |
| Delete project | Yes | No | No |

An unauthenticated user has no project access. A registered user who is not a member cannot read private project/issue information. Do not rely on hiding Angular buttons for authorization; enforce permission checks in the API.

Additional rules:

- Assignment must target an Admin or Member of **the same project**; Viewers are not assignable in this proposed policy.
- Removing a member and unassigning their issues should happen atomically in one database transaction.
- Prevent removing or demoting the last project Admin.
- Check the requested issue belongs to the requested project when endpoints contain both IDs.
- Return appropriate HTTP errors for unauthenticated, unauthorized, missing, and invalid requests. Avoid leaking private resource details to nonmembers.

## 5. API outline

These are candidate routes, not a contract frozen before implementation.

### Accounts

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/api/account/register` | Register (if enabled) |
| POST | `/api/account/login` | Log in |
| POST | `/api/account/logout` | Log out |
| GET | `/api/account/me` | Current user's basic profile |

Built-in Identity endpoints are also an option; compare them before writing custom account endpoints.

### Projects and memberships

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/projects` | List projects the current user belongs to |
| POST | `/api/projects` | Create a project and creator Admin membership |
| GET | `/api/projects/{projectId}` | Get project details |
| PUT | `/api/projects/{projectId}` | Edit project |
| DELETE | `/api/projects/{projectId}` | Delete project (Admin only) |
| GET | `/api/projects/{projectId}/members` | List members |
| POST | `/api/projects/{projectId}/members` | Add an existing registered user by username |
| PUT | `/api/projects/{projectId}/members/{userId}` | Change a member's role |
| DELETE | `/api/projects/{projectId}/members/{userId}` | Remove member and unassign their issues |

### Issues

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/projects/{projectId}/issues` | List/search/filter issues |
| POST | `/api/projects/{projectId}/issues` | Create issue |
| GET | `/api/issues/{issueId}` | Issue details |
| PUT | `/api/issues/{issueId}` | Edit issue, status, priority, or assignee |
| DELETE | `/api/issues/{issueId}` | Delete issue (Admin only) |

For list queries, support only useful filters initially: status, priority, assignee, and a simple title search. Sort by creation date by default. Pagination may be added once needed.

## 6. UI pages

- Register / Login
- My Projects (projects where I am a member)
- Create / Edit Project
- Project Overview and Member Management (Admin controls)
- Project Issues List (filter/search)
- Create / Edit / View Issue
- Delete confirmation where applicable

Use Angular routing, `HttpClient`, and a basic form approach such as reactive forms. Show useful loading, empty, validation-error, and API-error states. A functional UI matters more than elaborate styling.

## 7. Suggested repository structure

```text
IssueTracker/
├── backend/
│   ├── IssueTracker.Api/
│   │   ├── Controllers/
│   │   ├── Contracts/
│   │   ├── Data/
│   │   ├── Models/
│   │   └── Program.cs
│   └── IssueTracker.Tests/
├── frontend/
│   └── issue-tracker/
├── docs/
│   └── PLAN.md
└── README.md
```

Organize more deeply only when the code needs it. Simple controller endpoints and focused application logic are fine for the MVP.

## 8. Milestones

Each milestone should be demonstrable and mergeable. Tests are added as features arrive, not postponed until a dedicated testing phase.

### Milestone 1 — Setup and first API call

- [ ] Stormchiller creates the repository and invites JoyfulReaper.
- [ ] Create the ASP.NET Core Web API and Angular app.
- [ ] Document local prerequisites and startup commands.
- [ ] Return a basic API response and display it in Angular.
- [ ] Configure local API URL/proxy and development CORS as needed.
- [ ] Open and review an initial pull request.

**Done:** Both contributors can clone, run, and see Angular communicate with ASP.NET Core.

### Milestone 2 — Authentication foundation

- [ ] Add Identity and database migrations.
- [ ] Decide and document browser authentication/CSRF behavior.
- [ ] Implement registration (if enabled), login, logout, and current-user display.
- [ ] Protect a test API route and handle authenticated/unauthenticated UI states.
- [ ] Test successful login, bad credentials, logout, and protected access.

**Done:** A real account can sign in and access a protected endpoint; unauthenticated requests are rejected.

### Milestone 3 — Project CRUD and ownership

- [ ] Add Project and ProjectMembership models/migration.
- [ ] Implement project create/list/details/edit/delete.
- [ ] Automatically create Admin membership for the project creator.
- [ ] Only show projects the current user belongs to.
- [ ] Implement Angular project pages/forms.
- [ ] Test that another registered nonmember cannot read or change a private project.

**Done:** Authenticated users can manage their own projects without accessing others' projects.

### Milestone 4 — Memberships and project RBAC

- [ ] Add an existing user to a project by username.
- [ ] List members and change project roles.
- [ ] Remove a member; unassign their project issues once issues exist.
- [ ] Enforce the Admin/Member/Viewer permission matrix in the API.
- [ ] Prevent the last Admin from being removed or demoted.
- [ ] Test project-specific access, including one user having different roles across projects.

**Done:** Project Admins control membership, and role checks cannot be bypassed through direct API requests.

### Milestone 5 — Issue CRUD and assignments

- [ ] Add Issue model and migration.
- [ ] Implement create/list/details/edit/delete endpoints.
- [ ] Add status and priority controls.
- [ ] Add optional individual assignee selection from eligible project members.
- [ ] Implement Angular issue pages.
- [ ] Test invalid project IDs, unauthorized access, invalid assignees, and member removal behavior.

**Done:** Two people can collaborate on one project and track issues assigned to individual users.

### Milestone 6 — Usability and filtering

- [ ] Filter by status, priority, and assignee.
- [ ] Add basic title search and sorting.
- [ ] Improve form validation, empty states, and errors.
- [ ] Add a few focused frontend service/component tests.

**Done:** Users can find the tickets they need without browsing every issue.

### Milestone 7 — Verification and CI

- [ ] Add API integration tests using an isolated test database.
- [ ] Cover the permission matrix and cross-project access boundaries.
- [ ] Verify that direct API requests cannot bypass UI restrictions.
- [ ] Add GitHub Actions for backend and frontend build/tests.
- [ ] Update README with setup, architecture, and test commands.

**Done:** A fresh checkout builds and tests successfully; key access-control scenarios are covered.

### Milestone 8 — Deployment

- [ ] Choose an appropriately isolated hosting environment.
- [ ] Serve the frontend/API over HTTPS with a clear origin/cookie strategy.
- [ ] Configure production secrets and database storage outside source control.
- [ ] Set up backup and recovery for the database.
- [ ] Apply migrations safely and verify health/logging.
- [ ] Test authentication and authorization in the deployed environment before granting outside access.

**Done:** A small deployed demo works securely and can be reproduced from documented steps.

## 9. Tests worth writing early

- Project names and issue titles are required.
- Project creation gives the creator Admin membership.
- A nonmember cannot list/read/update another project's tickets.
- A Viewer cannot create or edit issues.
- A Member can create/edit issues but cannot manage memberships or delete projects.
- An Admin can manage their own project but not another project's membership.
- Duplicate membership is rejected.
- An issue cannot be assigned to a user outside its project.
- Removing an assigned member unassigns their issues without deleting the issues.
- The last Admin cannot be removed/demoted.
- API updates do not accept unauthorized changes to project or creator fields.

## 10. Deliberately out of MVP scope

- Team- or role-assigned tickets (we use **one individual assignee** or none)
- Organizations, department hierarchies, custom roles/permission editors
- Email invitations, password reset emails, external OAuth providers
- Comments, attachments, activity timelines, notifications
- Kanban drag-and-drop board, analytics dashboard, real-time updates
- Microservices, event buses, complex abstractions, production orchestration

These are possible follow-up features after the core app is finished.

## 11. Collaboration and AI guidelines

- Stormchiller owns the repository initially; both contributors make technical decisions and review pull requests.
- Stormchiller leads Angular work; Kyle is interested in writing C# backend code. Share knowledge and occasionally swap areas rather than permanently siloing work.
- Prefer small GitHub issues and pull requests. Write down acceptance criteria for each feature.
- Review how and why code works; do not merge solely because it compiles.
- Aim for a lightweight weekly progress update. No rigid deadlines; consistency is the point.
- AI may assist with planning, documentation, explanations, and review. **Agree explicitly before using AI to generate implementation code.** Both people should understand what they merge.
- Use Reddit initially and optionally Matrix for ongoing communication; no specific messaging platform is required to contribute.

## 12. Kickoff checklist

- [ ] Confirm both collaborators agree with the stack and MVP.
- [ ] Choose the repository/project name.
- [ ] Confirm private-project visibility and the proposed permission matrix.
- [ ] Agree on initial registration and authentication/hosting approach.
- [ ] Decide project-deletion behavior.
- [ ] Agree on AI use and review expectations.
- [ ] Start Milestone 1.

**Keep the first release finishable.** A modest app with real authorization, tests, and a reproducible deployment is a much better learning and portfolio outcome than a half-finished Jira clone.
