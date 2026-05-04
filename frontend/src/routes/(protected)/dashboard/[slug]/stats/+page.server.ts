import type { PageServerLoad } from "./$types";

export const load: PageServerLoad = async ({ parent }) => {
    const { stats, guild, settings } = await parent();

    return { stats, guild, settings };
};
