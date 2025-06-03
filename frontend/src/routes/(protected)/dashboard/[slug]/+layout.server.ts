import { redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';
import type { Guild } from '$lib/server/types';
import { getGuildStats } from '$lib/server/stats';
import { getGuildSettings } from '$lib/server/settings';

export const load: LayoutServerLoad = async ({ params, parent }) => {
  const guildId: string = params.slug;
  const { userGuilds }: { userGuilds: Guild[] } = await parent();
  const guild: Guild | undefined = userGuilds.find(guild => guild.id === guildId);

  if (!guild) {
    throw redirect(303, '/dashboard');
  }

  const stats = await getGuildStats(guild);

  const settings = await getGuildSettings(guild);

  return { guild, stats, settings };
};
