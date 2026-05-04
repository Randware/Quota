import type { LayoutServerLoad } from './$types';
import { getUserGuilds } from '$lib/server/auth';
import type { Guild } from '$lib/server/types';

export const load: LayoutServerLoad = async ({ locals }) => {
  const userGuilds: Promise<Guild[]> = getUserGuilds(locals.session!);

  return { userGuilds };
};
