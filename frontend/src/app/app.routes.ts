import { Routes } from '@angular/router';
import { Dashboard } from './pages/dashboard/dashboard';
import { IssueDetails } from './pages/issue-details/issue-details';
import { IssueForm } from './pages/issue-form/issue-form';
import { IssueList } from './pages/issue-list/issue-list';
import { ProjectForm } from './pages/project-form/project-form';
import { ProjectList } from './pages/project-list/project-list';

export const routes: Routes = [
  { path: '', component: Dashboard, title: 'Dashboard | GameIssueTracker' },
  { path: 'projects', component: ProjectList, title: 'Projects | GameIssueTracker' },
  { path: 'projects/new', component: ProjectForm, title: 'New Project | GameIssueTracker' },
  { path: 'projects/:id/edit', component: ProjectForm, title: 'Edit Project | GameIssueTracker' },
  { path: 'issues', component: IssueList, title: 'Issues | GameIssueTracker' },
  { path: 'issues/new', component: IssueForm, title: 'New Issue | GameIssueTracker' },
  { path: 'issues/:id', component: IssueDetails, title: 'Issue Details | GameIssueTracker' },
  { path: 'issues/:id/edit', component: IssueForm, title: 'Edit Issue | GameIssueTracker' },
  { path: '**', redirectTo: '' }
];
