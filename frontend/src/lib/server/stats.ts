import type { Guild, Session, Stats } from '$lib/server/types';
import { BACKEND_HOST } from '$env/static/private';

export async function getGuildStats(guild: Guild, session: Session): Promise<Stats> {
  try {
    const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/stats`, {
      headers: {
        "Authorization": `Bearer ${session.jwt}`
      }
    });

    if (!res.ok) {
      return { totalQuotes: 0, totalUpvotes: 0, totalDownvotes: 0, quotesByMonth: [], topQuotees: [], topQuotes: [] };
    }

    return await res.json();
  } catch {
    return { totalQuotes: 0, totalUpvotes: 0, totalDownvotes: 0, quotesByMonth: [], topQuotees: [], topQuotes: [] };
  }
}
