import { Routes } from '@angular/router';
import { Dashboard } from './pages/dashboard/dashboard';
import { IssueDetails } from './pages/issue-details/issue-details';
import { IssueForm } from './pages/issue-form/issue-form';
import { IssueList } from './pages/issue-list/issue-list';
import { ProjectForm } from './pages/project-form/project-form';
import { ProjectList } from './pages/project-list/project-list';
import { TeamPage } from './pages/team/team';
import { AuthPage } from './pages/auth/auth';
import { AccountPage } from './pages/account/account';
import { authGuard } from './core/services/auth.guard';

export const routes: Routes = [
  { path: '', component: Dashboard, title: 'Dashboard | GameIssueTracker', canActivate: [authGuard] },
  { path: 'projects', component: ProjectList, title: 'Projects | GameIssueTracker', canActivate: [authGuard] },
  { path: 'projects/new', component: ProjectForm, title: 'New Project | GameIssueTracker', canActivate: [authGuard] },
  { path: 'projects/:id/edit', component: ProjectForm, title: 'Edit Project | GameIssueTracker', canActivate: [authGuard] },
  { path: 'issues', component: IssueList, title: 'Issues | GameIssueTracker', canActivate: [authGuard] },
  { path: 'issues/new', component: IssueForm, title: 'New Issue | GameIssueTracker', canActivate: [authGuard] },
  { path: 'issues/:id', component: IssueDetails, title: 'Issue Details | GameIssueTracker', canActivate: [authGuard] },
  { path: 'issues/:id/edit', component: IssueForm, title: 'Edit Issue | GameIssueTracker', canActivate: [authGuard] },
  { path: 'teams', component: TeamPage, title: 'Teams | GameIssueTracker', canActivate: [authGuard] },
  { path: 'account', component: AccountPage, title: 'Account | GameIssueTracker', canActivate: [authGuard] },
  { path: 'auth', component: AuthPage, title: 'Sign in | GameIssueTracker' },
  { path: 'team', redirectTo: 'teams' },
  { path: '**', redirectTo: '' }
];
