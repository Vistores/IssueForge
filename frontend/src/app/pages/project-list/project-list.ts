import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Project } from '../../core/models/api.models';
import { Api } from '../../core/services/api';

@Component({
  selector: 'app-project-list',
  imports: [RouterLink],
  templateUrl: './project-list.html'
})
export class ProjectList implements OnInit {
  projects: Project[] = [];
  isLoading = true;
  error = '';

  constructor(private readonly api: Api) {}

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.isLoading = true;
    this.api.getProjects().subscribe({
      next: projects => {
        this.projects = projects;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load projects.';
        this.isLoading = false;
      }
    });
  }

  deleteProject(project: Project): void {
    if (!confirm(`Delete project "${project.name}" and its issues?`)) {
      return;
    }

    this.api.deleteProject(project.id).subscribe({
      next: () => this.loadProjects(),
      error: () => (this.error = 'Could not delete project.')
    });
  }
}
