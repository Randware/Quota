import type { PageServerLoad, Actions } from './$types';
import type { Guild } from '$lib/server/types';
import { getGuildQuotes, editQuote, deleteQuote } from '$lib/server/quotes';

export const load: PageServerLoad = async ({ parent, locals, url }) => {
    const { guild }: { guild: Guild } = await parent();
    const session = locals.session!;
    const page = parseInt(url.searchParams.get('page') ?? '1');
    const search = url.searchParams.get('search') ?? '';

    const quotesData = await getGuildQuotes(guild, session, page, 20, search);

    return { quotesData, guild, search };
};

export const actions: Actions = {
    editQuote: async ({ request, locals, params }) => {
        const session = locals.session;
        if (!session) return { success: false, error: 'Not authenticated' };

        const form = await request.formData();
        const quoteId = form.get('quoteId') as string;
        const content = form.get('content') as string;
        const quoteesRaw = form.get('quotees') as string;

        if (!quoteId || !content) {
            return { success: false, error: 'Missing quoteId or content' };
        }

        const quotees = quoteesRaw ? quoteesRaw.split(',').map(q => q.trim()).filter(Boolean) : [];

        const guild = { id: params.slug } as any;
        const result = await editQuote(guild, session, quoteId, content, quotees);

        return { ...result, action: 'edit' };
    },

    deleteQuote: async ({ request, locals, params }) => {
        const session = locals.session;
        if (!session) return { success: false, error: 'Not authenticated' };

        const form = await request.formData();
        const quoteId = form.get('quoteId') as string;

        if (!quoteId) {
            return { success: false, error: 'Missing quoteId' };
        }

        const guild = { id: params.slug } as any;
        const result = await deleteQuote(guild, session, quoteId);

        return { ...result, action: 'delete' };
    },
};
