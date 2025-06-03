import { dev } from '$app/environment';
import type { Guild, Stats } from '$lib/server/types';
import { createCache } from 'cache-manager';

const CACHE_TTL = 30_000;

const cache = createCache({ ttl: CACHE_TTL, });

const MOCK_STATS: Stats = { totalQuotes: 50 };

async function fetchStatsFromBackend(guild: Guild): Promise<Stats> {
  if (dev) {
    return MOCK_STATS
  }

  //  TODO: Query backend here
  const stats: Stats = { totalQuotes: 0 };

  return stats;
}

// Function to get stats from cache or fetch and update the cache
export async function getGuildStats(guild: Guild): Promise<Stats> {
  const cacheKey = `guild-stats:${guild.id}`;

  return cache.wrap(cacheKey, async () => {
    const stats = await fetchStatsFromBackend(guild);
    return stats;
  }, { ttl: CACHE_TTL });
}
