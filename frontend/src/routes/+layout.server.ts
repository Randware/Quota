import type { Session } from '$lib/server/types';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = ({ locals }) => {
  let session: Session | undefined = locals.session;

  return {
    session
  };
};
