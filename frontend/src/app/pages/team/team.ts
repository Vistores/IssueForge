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
  pendingTeamDelete?: Team;
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

  isCurrentUserOwner(team = this.selectedTeam): boolean {
    return !!team?.members.some(member => this.isCurrentUser(member) && member.role === 'Owner');
  }

  requestMemberUpdate(member: TeamMember): void {
    if (this.isCurrentUser(member)) {
      this.toast.error('You cannot edit your own permissions here.');
      return;
    }

    this.updateMember(member);
  }

  requestOwnerTransfer(member: TeamMember): void {
    if (this.isCurrentUser(member)) {
      this.toast.error('You already own this team.');
      return;
    }

    this.pendingOwnerTransfer = member;
  }

  confirmOwnerTransfer(): void {
    if (!this.pendingOwnerTransfer || !this.selectedTeam) {
      return;
    }

    this.api.transferTeamOwner(this.selectedTeam.id, { newOwnerMemberId: this.pendingOwnerTransfer.id }).subscribe({
      next: () => {
        this.toast.success('Team ownership transferred.');
        this.pendingOwnerTransfer = undefined;
        this.closeTeamDetails();
        this.loadTeams();
      },
      error: () => this.toast.error('Could not transfer ownership.')
    });
  }

  requestDeleteTeam(team: Team): void {
    if (!this.isCurrentUserOwner(team)) {
      this.toast.error('Only the owner can delete this team.');
      return;
    }

    this.pendingTeamDelete = team;
  }

  confirmDeleteTeam(): void {
    if (!this.pendingTeamDelete) {
      return;
    }

    this.api.deleteTeam(this.pendingTeamDelete.id).subscribe({
      next: () => {
        this.toast.success('Team deleted.');
        if (this.activeTeamId === this.pendingTeamDelete?.id) {
          this.api.setActiveTeamId(null);
        }
        this.pendingTeamDelete = undefined;
        this.closeTeamDetails();
        this.loadTeams();
      },
      error: () => this.toast.error('Could not delete team.')
    });
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
          if (this.selectedTeam) {
            this.selectedTeam = {
              ...this.selectedTeam,
              members: this.selectedTeam.members.map(item => (item.id === member.id ? { ...member } : item))
            };
          }
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
