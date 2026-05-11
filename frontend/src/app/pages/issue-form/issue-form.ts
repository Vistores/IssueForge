import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { IssuePriority, IssueStatus, Project } from '../../core/models/api.models';
import { Api } from '../../core/services/api';

@Component({
  selector: 'app-issue-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './issue-form.html'
})
export class IssueForm implements OnInit {
  private readonly fb = inject(FormBuilder);

  readonly statuses: IssueStatus[] = ['Open', 'InProgress', 'Fixed', 'Rejected'];
  readonly priorities: IssuePriority[] = ['Low', 'Medium', 'High', 'Critical'];

  issueId?: number;
  projects: Project[] = [];
  isSaving = false;
  error = '';

  form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(160)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    projectId: [0, [Validators.required, Validators.min(1)]],
    status: ['Open' as IssueStatus, Validators.required],
    priority: ['Medium' as IssuePriority, Validators.required]
  });

  constructor(
    private readonly api: Api,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.api.getProjects().subscribe({
      next: projects => {
        this.projects = projects;
        if (!this.issueId && projects.length) {
          this.form.patchValue({ projectId: projects[0].id });
        }
      },
      error: () => (this.error = 'Could not load projects.')
    });

    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      return;
    }

    this.issueId = id;
    this.api.getIssue(id).subscribe({
      next: issue => this.form.patchValue(issue),
      error: () => (this.error = 'Could not load issue.')
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const payload = {
      title: this.form.value.title ?? '',
      description: this.form.value.description ?? '',
      projectId: Number(this.form.value.projectId),
      status: this.form.value.status ?? 'Open',
      priority: this.form.value.priority ?? 'Medium'
    };

    if (this.issueId) {
      this.api.updateIssue(this.issueId, payload).subscribe({
        next: () => this.router.navigateByUrl(`/issues/${this.issueId}`),
        error: () => {
          this.error = 'Could not save issue.';
          this.isSaving = false;
        }
      });
      return;
    }

    this.api.createIssue(payload).subscribe({
      next: issue => this.router.navigateByUrl(`/issues/${issue.id}`),
      error: () => {
        this.error = 'Could not save issue.';
        this.isSaving = false;
      }
    });
  }
}
