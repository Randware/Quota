import type { LayoutServerLoad } from './$types';
import { getUserGuilds } from '$lib/server/auth';

export const load: LayoutServerLoad = async ({ locals }) => {
  const userGuilds = await getUserGuilds(locals.session);

  return { userGuilds };
};
