import { getGuildSettings } from "$lib/server/settings";
import type { Guild, Settings } from "$lib/server/types";
import { removeBotFromGuild } from "$lib/server/bot";
import type { PageServerLoad } from "./$types";
import type { Actions } from "./$types";

export const load: PageServerLoad = async ({ parent, locals }) => {
  let { guild }: { guild: Guild } = await parent();

  const settings: Promise<Settings> = getGuildSettings(guild, locals.session!);

  return { settings };
};

export const actions: Actions = {
  removeBot: async ({ locals, params }) => {
    const session = locals.session;
    if (!session) {
      return { success: false, error: 'Not authenticated' };
    }

    const guild = { id: params.slug } as Guild;
    const result = await removeBotFromGuild(guild, session);

    return { ...result };
  }
};
