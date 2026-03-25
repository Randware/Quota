import type { Guild, Session } from '$lib/server/types';
import { BACKEND_HOST } from '$env/static/private';

export interface QuoteItem {
    id: string;
    content: string;
    mediaUrls: string;
    upvotes: number;
    downvotes: number;
    score: number;
    createdAt: string | null;
    messageId: string;
    channelId: string;
    quotees: string[];
}

export interface QuotesResponse {
    quotes: QuoteItem[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

export async function getGuildQuotes(
    guild: Guild,
    session: Session,
    page = 1,
    pageSize = 20,
    search = ''
): Promise<QuotesResponse> {
    const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
    });
    if (search) params.set('search', search);

    const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/quotes?${params}`, {
        headers: {
            Authorization: `Bearer ${session.jwt}`,
        },
    });

    if (!res.ok) {
        return { quotes: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
    }

    return await res.json();
}

export async function editQuote(
    guild: Guild,
    session: Session,
    quoteId: string,
    content: string,
    quotees: string[]
): Promise<{ success: boolean; discordUpdated: boolean }> {
    const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/quote/${quoteId}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${session.jwt}`,
        },
        body: JSON.stringify({ content, quotees }),
    });

    if (!res.ok) {
        return { success: false, discordUpdated: false };
    }

    return await res.json();
}

export async function deleteQuote(
    guild: Guild,
    session: Session,
    quoteId: string
): Promise<{ success: boolean; discordDeleted: boolean }> {
    const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/quote/${quoteId}`, {
        method: 'DELETE',
        headers: {
            Authorization: `Bearer ${session.jwt}`,
        },
    });

    if (!res.ok) {
        return { success: false, discordDeleted: false };
    }

    return await res.json();
}
