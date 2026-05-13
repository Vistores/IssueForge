import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthStatus, Team, TeamMember } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-team',
  imports: [FormsModule],
  templateUrl: './team.html'
})
export class TeamPage implements OnInit {
  teams: Team[] = [];
  auth?: AuthStatus;
  activeTeamId: number | null = null;
  selectedTeam?: Team;
  pendingOwnerTransfer?: TeamMember;
  isTeamCreateModalOpen = false;
  teamMode: 'create' | 'join' = 'create';
  teamName = 'New QA Guild';
  inviteCode = '';
  isLoading = true;
  error = '';

  constructor(
    private readonly api: Api,
    private readonly toast: Toast
  ) {}

  ngOnInit(): void {
    this.api.getAuthStatus().subscribe({ next: auth => (this.auth = auth) });
    this.loadTeams();
  }

  loadTeams(): void {
    this.isLoading = true;
    this.activeTeamId = this.api.getActiveTeamId();
    this.api.getTeams().subscribe({
      next: teams => {
        this.teams = teams;
        this.activeTeamId = this.resolveActiveTeam(teams);
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load teams.';
        this.isLoading = false;
      }
    });
  }

  createTeam(): void {
    this.api.createTeam(this.teamName).subscribe({
      next: team => {
        this.teams = [...this.teams, team];
        this.selectTeam(team);
        this.closeTeamCreateModal();
        this.toast.success('Team created.');
      },
      error: () => this.toast.error('Could not create team.')
    });
  }

  joinTeam(): void {
    this.api.joinTeam(this.inviteCode).subscribe({
      next: joinedTeam => {
        this.teams = this.teams.map(team => (team.id === joinedTeam.id ? joinedTeam : team));
        if (!this.teams.some(team => team.id === joinedTeam.id)) {
          this.teams = [...this.teams, joinedTeam];
        }
        this.selectTeam(joinedTeam);
        this.closeTeamCreateModal();
        this.toast.success('Joined team.');
      },
      error: () => this.toast.error('Invite code was not found.')
    });
  }

  selectTeam(team: Team): void {
    this.activeTeamId = team.id;
    this.api.setActiveTeamId(team.id);
  }

  openTeamDetails(team: Team): void {
    this.selectTeam(team);
    this.selectedTeam = team;
  }

  closeTeamDetails(): void {
    this.selectedTeam = undefined;
  }

  openTeamCreateModal(mode: 'create' | 'join' = 'create'): void {
    this.teamMode = mode;
    this.isTeamCreateModalOpen = true;
  }

  closeTeamCreateModal(): void {
    this.isTeamCreateModalOpen = false;
  }

  isCurrentUser(member: TeamMember): boolean {
    return member.userId === this.auth?.userId;
  }

  requestMemberUpdate(member: TeamMember): void {
    if (this.isCurrentUser(member)) {
      this.toast.error('You cannot edit your own permissions here.');
      return;
    }

    if (member.role === 'Owner') {
      this.pendingOwnerTransfer = member;
      return;
    }

    this.updateMember(member);
  }

  confirmOwnerTransfer(): void {
    if (!this.pendingOwnerTransfer) {
      return;
    }

    this.updateMember(this.pendingOwnerTransfer);
    this.pendingOwnerTransfer = undefined;
  }

  private updateMember(member: TeamMember): void {
    const teamId = this.activeTeamId;
    if (!teamId) {
      return;
    }

    this.api
      .updateTeamMember(teamId, member.id, {
        role: member.role,
        canEditIssues: member.canEditIssues,
        canAssignIssues: member.canAssignIssues,
        issueLimit: Number(member.issueLimit)
      })
      .subscribe({
        next: () => {
          this.toast.success('Member permissions updated.');
          this.loadTeams();
        },
        error: () => this.toast.error('Could not update member.')
      });
  }

  copyInviteCode(team: Team): void {
    navigator.clipboard
      .writeText(team.inviteCode)
      .then(() => this.toast.success('Invite code copied.'))
      .catch(() => this.toast.error('Could not copy invite code.'));
  }

  copyInviteLink(team: Team): void {
    const link = `${window.location.origin}/auth?inviteCode=${encodeURIComponent(team.inviteCode)}`;
    navigator.clipboard
      .writeText(link)
      .then(() => this.toast.success('Invite link copied.'))
      .catch(() => this.toast.error('Could not copy invite link.'));
  }

  private resolveActiveTeam(teams: Team[]): number | null {
    if (!teams.length) {
      this.api.setActiveTeamId(null);
      return null;
    }

    const existing = this.api.getActiveTeamId();
    const active = teams.some(team => team.id === existing) ? existing : teams[0].id;
    this.api.setActiveTeamId(active);
    return active;
  }
}
