import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityLog, MemberStats, Team, TeamMember } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-team',
  imports: [DatePipe, FormsModule],
  templateUrl: './team.html'
})
export class TeamPage implements OnInit {
  teams: Team[] = [];
  stats: MemberStats[] = [];
  activity: ActivityLog[] = [];
  activeTeamId: number | null = null;
  teamName = 'New QA Guild';
  inviteCode = '';
  isLoading = true;
  error = '';

  constructor(
    private readonly api: Api,
    private readonly toast: Toast
  ) {}

  ngOnInit(): void {
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
        this.loadTeamInsights();
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
        this.toast.success('Joined team.');
      },
      error: () => this.toast.error('Invite code was not found.')
    });
  }

  selectTeam(team: Team): void {
    this.activeTeamId = team.id;
    this.api.setActiveTeamId(team.id);
    this.loadTeamInsights();
  }

  updateMember(member: TeamMember): void {
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
          this.loadTeamInsights();
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

  private loadTeamInsights(): void {
    if (!this.activeTeamId) {
      this.stats = [];
      this.activity = [];
      return;
    }

    this.api.getTeamStats(this.activeTeamId).subscribe({ next: stats => (this.stats = stats) });
    this.api.getTeamActivity(this.activeTeamId).subscribe({ next: activity => (this.activity = activity) });
  }
}
