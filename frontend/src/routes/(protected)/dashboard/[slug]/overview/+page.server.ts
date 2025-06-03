import { getGuildSettings } from "$lib/server/settings";
import type { Guild, Settings } from "$lib/server/types";
import type { PageServerLoad } from "./$types";

export const load: PageServerLoad = async ({ parent }) => {
  let { guild }: { guild: Guild } = await parent();

  const settings: Promise<Settings> = getGuildSettings(guild);

  return { settings };
};
