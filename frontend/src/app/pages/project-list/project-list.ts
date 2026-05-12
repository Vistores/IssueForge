import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Project } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-project-list',
  imports: [FormsModule],
  templateUrl: './project-list.html'
})
export class ProjectList implements OnInit {
  projects: Project[] = [];
  modalProject?: Project | null;
  formModel = { name: '', description: '' };
  isSaving = false;
  isLoading = true;
  error = '';

  constructor(
    private readonly api: Api,
    private readonly toast: Toast
  ) {}

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

  openCreateModal(): void {
    this.modalProject = null;
    this.formModel = { name: '', description: '' };
  }

  openEditModal(project: Project): void {
    this.modalProject = project;
    this.formModel = { name: project.name, description: project.description ?? '' };
  }

  closeModal(): void {
    this.modalProject = undefined;
    this.isSaving = false;
  }

  saveProject(): void {
    if (!this.formModel.name.trim()) {
      this.toast.error('Project name is required.');
      return;
    }

    this.isSaving = true;
    const payload = {
      name: this.formModel.name.trim(),
      description: this.formModel.description.trim()
    };

    if (this.modalProject) {
      this.api.updateProject(this.modalProject.id, payload).subscribe({
        next: () => {
          this.toast.success('Project updated.');
          this.closeModal();
          this.loadProjects();
        },
        error: () => {
          this.toast.error('Could not save project.');
          this.isSaving = false;
        }
      });
      return;
    }

    this.api.createProject(payload).subscribe({
      next: () => {
        this.toast.success('Project created.');
        this.closeModal();
        this.loadProjects();
      },
      error: () => {
        this.toast.error('Could not create project.');
        this.isSaving = false;
      }
    });
  }

  deleteProject(project: Project): void {
    if (!confirm(`Delete project "${project.name}" and its issues?`)) {
      return;
    }

    this.api.deleteProject(project.id).subscribe({
      next: () => {
        this.toast.success('Project deleted.');
        this.loadProjects();
      },
      error: () => (this.error = 'Could not delete project.')
    });
  }
}
