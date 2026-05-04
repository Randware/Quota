import { getGuildRoles, getGuildMembers, getGuildPermissions, updateGuildPermissions } from '$lib/server/permissions';
import type { Guild, PermissionEntry } from '$lib/server/types';
import type { PageServerLoad, Actions } from './$types';

export const load: PageServerLoad = async ({ parent, locals }) => {
    const { guild }: { guild: Guild } = await parent();
    const session = locals.session!;

    const [roles, members, permissions] = await Promise.all([
        getGuildRoles(guild, session),
        getGuildMembers(guild, session),
        getGuildPermissions(guild, session)
    ]);

    return { roles, members, permissions };
};

export const actions: Actions = {
    updatePermissions: async ({ request, locals, params }) => {
        const session = locals.session;
        if (!session) return { success: false, error: 'Not authenticated' };

        const form = await request.formData();
        const rawPayload = form.get('payload');
        if (typeof rawPayload !== 'string') {
            return { success: false, error: 'Missing payload' };
        }

        try {
            const permissions: PermissionEntry[] = JSON.parse(rawPayload);
            const guild = { id: params.slug } as Guild;
            await updateGuildPermissions(guild, permissions, session);
            return { success: true };
        } catch (e: any) {
            return { success: false, error: e.message || 'Failed to update permissions' };
        }
    }
};
