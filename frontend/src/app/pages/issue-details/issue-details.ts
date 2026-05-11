import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Comment, Issue } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-issue-details',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './issue-details.html'
})
export class IssueDetails implements OnInit {
  private readonly fb = inject(FormBuilder);

  issue?: Issue;
  comments: Comment[] = [];
  issueId = 0;
  isLoading = true;
  error = '';

  commentForm = this.fb.group({
    author: ['QA Tester', Validators.maxLength(80)],
    text: ['', [Validators.required, Validators.maxLength(1000)]]
  });

  constructor(
    private readonly api: Api,
    private readonly toast: Toast,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.issueId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadIssue();
    this.loadComments();
  }

  loadIssue(): void {
    this.api.getIssue(this.issueId).subscribe({
      next: issue => {
        this.issue = issue;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load issue.';
        this.isLoading = false;
      }
    });
  }

  loadComments(): void {
    this.api.getComments(this.issueId).subscribe({
      next: comments => (this.comments = comments),
      error: () => (this.error = 'Could not load comments.')
    });
  }

  addComment(): void {
    if (this.commentForm.invalid) {
      this.commentForm.markAllAsTouched();
      return;
    }

    this.api
      .addComment(this.issueId, {
        author: this.commentForm.value.author || 'QA Tester',
        text: this.commentForm.value.text || ''
      })
      .subscribe({
        next: () => {
          this.commentForm.patchValue({ text: '' });
          this.loadComments();
          this.loadIssue();
        },
        error: () => (this.error = 'Could not add comment.')
      });
  }

  deleteComment(comment: Comment): void {
    this.api.deleteComment(this.issueId, comment.id).subscribe({
      next: () => this.loadComments(),
      error: () => (this.error = 'Could not delete comment.')
    });
  }

  deleteIssue(): void {
    if (!this.issue || !confirm(`Delete issue "${this.issue.title}"?`)) {
      return;
    }

    this.api.deleteIssue(this.issue.id).subscribe({
      next: () => this.router.navigateByUrl('/issues'),
      error: () => (this.error = 'Could not delete issue.')
    });
  }

  copyText(value: string, label = 'Text'): void {
    navigator.clipboard
      .writeText(value)
      .then(() => this.toast.success(`${label} copied.`))
      .catch(() => this.toast.error(`Could not copy ${label.toLowerCase()}.`));
  }
}
