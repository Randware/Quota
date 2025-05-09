import type { Session } from '$lib/server/auth';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = ({ locals }) => {
  let session: Session | null = locals.session;
  let user: { name: string; picture: string } | null = session
    ? { name: session.userName, picture: session.userAvatar }
    : null;

  return {
    user
  };
};
