import { getGuildChannels, getGuildSettings, updateGuildSettings } from '$lib/server/settings';
import type { Channel, Guild, Session, Settings } from '$lib/server/types';
import type { PageServerLoad, Actions } from './$types';

export const load: PageServerLoad = async ({ parent, locals }) => {
  const { guild }: { guild: Guild } = await parent();
  const session = locals.session!;

  const settings: Promise<Settings> = getGuildSettings(guild, session);
  const allChannels: Promise<Channel[]> = getGuildChannels(guild, session);

  return { settings, allChannels };
};

export const actions: Actions = {
  applySettings: async ({ request, locals }) => {
    const session = locals.session;
    if (!session) {
      return { success: false, error: 'Not authenticated' };
    }

    const form = await request.formData();
    const rawPayload = form.get('payload');
    const rawGuild = form.get('guild');

    if (typeof rawPayload !== 'string') {
      return { success: false, error: 'Missing or invalid payload' };
    }

    if (typeof rawGuild !== 'string') {
      return { success: false, error: 'Missing or invalid guild' };
    }

    let newSettings: Settings;
    let guild: Guild;

    try {
      newSettings = JSON.parse(rawPayload) as Settings;
    } catch (e) {
      return { success: false, error: 'Cannot parse JSON payload' };
    }

    try {
      guild = JSON.parse(rawGuild) as Guild;
    } catch (e) {
      return { success: false, error: 'Cannot parse JSON guild' };
    }

    await updateGuildSettings(guild, newSettings, session);

    return { success: true };
  }
};
