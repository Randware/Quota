import { getGuildSettings } from "$lib/server/settings";
import type { Guild } from "$lib/server/types";
import type { PageServerLoad } from "./$types";

export const load: PageServerLoad = async ({ parent }) => {
  let { guild }: { guild: Guild } = await parent();

  const settings = await getGuildSettings(guild);

  return { settings };
};
