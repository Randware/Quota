import type { Session } from '$lib/server/auth';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = ({ locals }) => {
  let session: Session | undefined = locals.session;

  return {
    session
  };
};
