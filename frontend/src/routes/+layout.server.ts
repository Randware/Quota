import type { Session } from '$lib/server/auth';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = ({ locals }) => {
  let session: Session | null = locals.session;

  return {
    user: session
      ? { name: session.username, picture: session.userPicture }
      : null
  };
};
