import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of, switchMap } from 'rxjs';
import { Api } from './api';

export const authGuard: CanActivateFn = (_route, state) => {
  const api = inject(Api);
  const router = inject(Router);

  return api.getAuthStatus().pipe(
    switchMap(status => {
      if (!status.isAuthenticated) {
        return of(router.createUrlTree(['/auth']));
      }

      return api.getTeams().pipe(
        map(teams => {
          if (!teams.length) {
            api.setActiveTeamId(null);
            return state.url.startsWith('/teams') || state.url.startsWith('/account')
              ? true
              : router.createUrlTree(['/teams']);
          }

          const activeTeamId = api.getActiveTeamId();
          const activeTeam = teams.find(team => team.id === activeTeamId);
          const preferredTeam = teams.find(team => team.projectCount > 0) ?? teams[0];

          if (!activeTeam || (activeTeam.projectCount === 0 && preferredTeam.projectCount > 0)) {
            api.setActiveTeamId(preferredTeam.id);
          }

          return true;
        })
      );
    }),
    catchError(() => of(router.createUrlTree(['/auth'])))
  );
};
