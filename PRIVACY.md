# Privacy Policy

Last updated: May 14, 2026

IssueForge is a self-hosted issue and work tracking demo project. This policy explains what data the application is designed to process when it is run locally, used as a demo or deployed by a team.

## Data the Application Stores

Depending on how the application is configured and used, IssueForge can store:

- Account name and email address
- Profile avatar image data or avatar URL
- Team names, invite codes and team membership records
- Team member roles, permissions and issue limits
- Project records
- Issues, descriptions, statuses, priorities and assignments
- Comments and comment timestamps
- File attachment metadata and uploaded file content
- Activity log entries for workspace actions

## Authentication Data

The local authentication flow stores account records in the configured application database. Passwords should always be hashed by the backend and must never be stored as plain text.

If Google OAuth or another external identity provider is enabled, authentication is handled by that provider. IssueForge may store basic profile information returned by the provider, such as name, email and avatar URL.

## Cookies

IssueForge uses authentication cookies to keep signed-in users connected to their workspace. Production deployments should use HTTPS and secure cookie settings.

## File Attachments

IssueForge can accept screenshots, media, documents and text files as issue attachments. Teams should avoid uploading sensitive personal, financial, medical or confidential third-party data unless their deployment and storage policies allow it.

## Demo and Self-Hosted Deployments

This repository is provided as a demo project. If you deploy IssueForge, you are responsible for:

- Choosing where the database and uploaded files are stored
- Protecting server, database and storage credentials
- Configuring HTTPS, backups and access controls
- Complying with privacy laws that apply to your users and location
- Replacing demo seed data before production use

## Data Retention

IssueForge keeps data until a user or authorized team member deletes it, or until the deployment owner removes the database/storage volume. Account deletion and team deletion can remove related workspace data depending on ownership and team membership rules.

## Third-Party Services

The base project can be configured to use:

- Google OAuth for sign-in
- Hosting providers for frontend/backend deployment
- External databases or object storage if adapted for production

Those services have their own privacy policies.

## Contact

For questions about this demo repository, contact the project maintainer through the GitHub repository.

This policy is a project document and should be reviewed before using IssueForge in a public hosted environment.
