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
import { StatsPage } from './pages/stats/stats';
import { ActivityPage } from './pages/activity/activity';
import { authGuard } from './core/services/auth.guard';

export const routes: Routes = [
  { path: '', component: Dashboard, title: 'Dashboard | IssueForge', canActivate: [authGuard] },
  { path: 'projects', component: ProjectList, title: 'Projects | IssueForge', canActivate: [authGuard] },
  { path: 'projects/new', component: ProjectForm, title: 'New Project | IssueForge', canActivate: [authGuard] },
  { path: 'projects/:id/edit', component: ProjectForm, title: 'Edit Project | IssueForge', canActivate: [authGuard] },
  { path: 'issues', component: IssueList, title: 'Issues | IssueForge', canActivate: [authGuard] },
  { path: 'issues/new', component: IssueForm, title: 'New Issue | IssueForge', canActivate: [authGuard] },
  { path: 'issues/:id', component: IssueDetails, title: 'Issue Details | IssueForge', canActivate: [authGuard] },
  { path: 'issues/:id/edit', component: IssueForm, title: 'Edit Issue | IssueForge', canActivate: [authGuard] },
  { path: 'teams', component: TeamPage, title: 'Teams | IssueForge', canActivate: [authGuard] },
  { path: 'stats', component: StatsPage, title: 'Stats | IssueForge', canActivate: [authGuard] },
  { path: 'activity', component: ActivityPage, title: 'Activity | IssueForge', canActivate: [authGuard] },
  { path: 'account', component: AccountPage, title: 'Account | IssueForge', canActivate: [authGuard] },
  { path: 'auth', component: AuthPage, title: 'Sign in | IssueForge' },
  { path: 'team', redirectTo: 'teams' },
  { path: '**', redirectTo: '' }
];
