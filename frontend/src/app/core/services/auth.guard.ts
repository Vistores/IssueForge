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
          if (!teams.some(team => team.id === activeTeamId)) {
            api.setActiveTeamId(teams[0].id);
          }

          return true;
        })
      );
    }),
    catchError(() => of(router.createUrlTree(['/auth'])))
  );
};
