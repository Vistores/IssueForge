# Release Notes

## v0.1.0

Initial public template release for IssueForge.

### Included

- Angular frontend with routed pages and custom professional dark UI
- ASP.NET Core Web API backend
- SQLite persistence through Entity Framework Core
- Local account registration and sign-in
- Optional Google OAuth configuration entry point
- Private team workspaces
- Team invite codes and invite links
- Team ownership transfer and team deletion flows
- Team roles: Owner, Manager, Member, Commenter and Viewer
- Project CRUD
- Issue CRUD
- Board and table issue views
- Filters by team, status, priority, project and assignee
- Issue preview modal with comments, assignments and copy actions
- Issue attachment upload support
- Comment creation and deletion from the issue preview modal
- Dashboard metrics, recently updated list and critical watchlist
- Member statistics and team activity log pages
- Account avatar upload, paste/drop support and crop controls
- Swagger/OpenAPI endpoint
- Docker Compose setup
- GitHub Actions backend/frontend build workflow
- Privacy Policy and Terms of Use template documents

### Demo Account

- Email: `alex.morgan@issueforge.local`
- Password: `Demo123!`

### Recommended Testing Flow

1. Sign in with the demo account.
2. Open the dashboard and review workspace metrics.
3. Create or edit a project.
4. Create an issue with assignees and attachments.
5. Switch between board and table views.
6. Drag an issue between statuses.
7. Open the issue preview modal and add a comment.
8. Open team settings and review roles/permissions.
9. Check member stats and activity logs.

### Notes

This release is suitable as a source-code template and local demo. A public click-through demo requires a deployed frontend, backend and database environment.
