import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Api } from '../../core/services/api';

@Component({
  selector: 'app-project-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './project-form.html'
})
export class ProjectForm implements OnInit {
  private readonly fb = inject(FormBuilder);

  projectId?: number;
  isSaving = false;
  error = '';

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    description: ['', Validators.maxLength(500)]
  });

  constructor(
    private readonly api: Api,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      return;
    }

    this.projectId = id;
    this.api.getProject(id).subscribe({
      next: project => this.form.patchValue(project),
      error: () => (this.error = 'Could not load project.')
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const payload = {
      name: this.form.value.name ?? '',
      description: this.form.value.description ?? ''
    };

    if (this.projectId) {
      this.api.updateProject(this.projectId, payload).subscribe({
        next: () => this.router.navigateByUrl('/projects'),
        error: () => {
          this.error = 'Could not save project.';
          this.isSaving = false;
        }
      });
      return;
    }

    this.api.createProject(payload).subscribe({
      next: () => this.router.navigateByUrl('/projects'),
      error: () => {
        this.error = 'Could not save project.';
        this.isSaving = false;
      }
    });
  }
}
