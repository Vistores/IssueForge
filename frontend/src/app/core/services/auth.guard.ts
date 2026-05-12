import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of, switchMap } from 'rxjs';
import { Api } from './api';

export const authGuard: CanActivateFn = () => {
  const api = inject(Api);
  const router = inject(Router);

  return api.getAuthStatus().pipe(
    switchMap(status => {
      if (!status.isAuthenticated) {
        return of(router.createUrlTree(['/team']));
      }

      return api.getTeams().pipe(
        map(teams => {
          if (!teams.length) {
            api.setActiveTeamId(null);
            return router.createUrlTree(['/team']);
          }

          const activeTeamId = api.getActiveTeamId();
          if (!teams.some(team => team.id === activeTeamId)) {
            api.setActiveTeamId(teams[0].id);
          }

          return true;
        })
      );
    }),
    catchError(() => of(router.createUrlTree(['/team'])))
  );
};
