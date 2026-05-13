# IssueForge

IssueForge is a full-stack issue and work tracking application for small teams. It helps organize projects, work items, comments, assignments, team roles, activity logs and member statistics in private workspaces.

It combines a compact issue board, team workspaces, permissions, comments, assignments, attachments and activity tracking in a clean Angular + ASP.NET Core monorepo.

## Tech Stack

- Frontend: Angular, Angular routing, Angular forms, Angular CDK drag-and-drop
- Backend: ASP.NET Core Web API
- Database: SQLite with Entity Framework Core
- API documentation: Swagger / OpenAPI
- Authentication: local email/password auth, optional Google OAuth configuration
- Styling: custom responsive CSS, no heavy UI framework
- Version control: Git with a structured commit history

## Features

- Account registration and sign-in
- Account deletion with shared-team ownership checks
- Optional Google OAuth entry point when credentials are configured
- Private team workspaces
- Create teams or join by invite code/link
- Delete a team when you are the owner
- Transfer team ownership to another member with confirmation
- Team-scoped projects, issues, comments, members and activity
- Project CRUD: create, edit, delete and list projects
- Issue CRUD: create, edit, delete and list issues
- Issue preview modal with description, status, priority, assignees and comments
- Issue attachments for screenshots, media, documents and text files
- Add and delete comments directly from the issue preview modal
- Kanban-style board with drag-and-drop status updates
- Table view with inline status and priority editing
- Filters by team, status, priority, project and assignee
- Assignment controls with member avatars and quick "assign me" actions
- Team member roles: Owner, Manager, Member, Commenter and Viewer
- Team permissions for editing and assigning issues
- Commenter role for users who can only leave task comments
- Member issue limits
- Member statistics page
- Team activity log for issue and permission changes
- Account profile page with avatar upload, drag/drop, paste and circular crop controls
- Dashboard with total, open, fixed and critical issue counts
- Recently updated issue list with update time and actor
- Critical watchlist
- Toast notifications, loading states and empty states
- Medieval-inspired issue board theme with parchment panels and a custom background
- DTO-based API responses instead of exposing raw EF entities
- Optional seed data for local development
- CORS configuration for the Angular dev server
- Docker Compose setup for local container runs
- GitHub Actions workflow for backend and frontend build checks

## Screenshots

Add screenshots or GIFs here:

- Sign-in and registration screen
- Dashboard overview
- Kanban issue board with drag-and-drop
- Issue preview modal with comments and assignees
- Issue table with filters and inline editing
- Team cards and team settings modal
- Member statistics page
- Activity log page
- Account avatar upload and crop modal
- Swagger API page

## Project Structure

```text
IssueForge/
  backend/     ASP.NET Core Web API, EF Core models, DTOs, controllers and services
  frontend/    Angular app with routed pages, API services and custom CSS
  README.md
  .gitignore
```

## Run the Backend

```bash
cd backend
dotnet restore
dotnet run --launch-profile http
```

The backend runs at:

- API: `http://localhost:5008/api`
- Swagger: `http://localhost:5008/swagger`

SQLite database file:

- `backend/issueforge.db`

The database is created automatically on startup. Development seed data is enabled through configuration.

Local development seed account:

- Email: `demo@game.local`
- Password: `Demo123!`

## Run the Frontend

```bash
cd frontend
npm install
npm start
```

The Angular app runs at:

- `http://localhost:4200`

Make sure the backend is running before using the frontend.

## Run with Docker Compose

```bash
docker compose up --build
```

Container URLs:

- Frontend: `http://localhost:4200`
- Backend API: `http://localhost:5008/api`
- Swagger: `http://localhost:5008/swagger`

The Docker Compose setup stores SQLite data in the `issueforge-data` volume.

## API Overview

### Auth

- `GET /api/auth/status`
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `PUT /api/auth/account`
- `DELETE /api/auth/account`
- `GET /api/auth/google`
- `GET /api/auth/google/callback`

### Teams

- `GET /api/teams`
- `POST /api/teams`
- `POST /api/teams/join`
- `PUT /api/teams/{teamId}/members/{memberId}`
- `POST /api/teams/{teamId}/transfer-owner`
- `DELETE /api/teams/{teamId}`
- `GET /api/teams/{teamId}/stats`
- `GET /api/teams/{teamId}/activity`

### Projects

- `GET /api/projects`
- `GET /api/projects/{id}`
- `POST /api/projects`
- `PUT /api/projects/{id}`
- `DELETE /api/projects/{id}`

### Issues

- `GET /api/issues`
- `GET /api/issues?status=Open&priority=High&projectId=1&assigneeId=2`
- `GET /api/issues/{id}`
- `POST /api/issues`
- `PUT /api/issues/{id}`
- `DELETE /api/issues/{id}`

### Comments

- `GET /api/issues/{issueId}/comments`
- `POST /api/issues/{issueId}/comments`
- `DELETE /api/issues/{issueId}/comments/{commentId}`

### Dashboard

- `GET /api/dashboard`

## Database Information

The backend uses Entity Framework Core with SQLite. The main entities are:

- `AppUser`
- `Team`
- `TeamMember`
- `Project`
- `Issue`
- `IssueAssignment`
- `Comment`
- `ActivityLog`

Issue statuses:

- `Open`
- `InProgress`
- `Fixed`
- `Rejected`

Issue priorities:

- `Low`
- `Medium`
- `High`
- `Critical`

## What This Project Demonstrates

- Angular standalone components and routing
- Angular forms and typed API models
- Angular CDK drag-and-drop
- Reusable frontend API service layer
- Modal-based CRUD flows
- Loading, error and empty UI states
- ASP.NET Core Web API controllers
- EF Core relationships and SQLite persistence
- DTOs for API boundaries
- Team-scoped data access
- Cookie-based authentication with production cookie hardening
- Optional external OAuth integration point
- Role-aware authorization checks for team actions, issue editing and comments
- CRUD operations with proper HTTP status codes
- Basic validation and error handling
- Swagger/OpenAPI documentation
- Clean monorepo organization
- Practical Git workflow and commit history
- Clear README and deployment-oriented project presentation
- Docker Compose foundations
- GitHub Actions build workflow

## Deployment Notes

- Build the backend with `dotnet publish -c Release`.
- Build the frontend with `npm run build`.
- Configure the frontend API base URL for the deployed backend before hosting.
- Set a production SQLite path or replace SQLite with a managed relational database.
- Use HTTPS and a production identity provider before exposing the app publicly.
- Store Google OAuth credentials and connection strings as environment secrets.
- Run the GitHub Actions workflow before merging changes.

## Future Improvements

- Production-grade JWT or external identity provider setup
- Refresh tokens and stricter role-based authorization policies
- Pagination, search and server-side sorting
- Attachment previews with external object storage support
- Rich text comments
- Notifications for assignment and status changes
- Unit and integration tests
- API-level tests for team permissions and ownership transfer
- Hosted production deployment
