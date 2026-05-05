import type { Guild, Session } from '$lib/server/types';
import { BACKEND_HOST } from '$env/static/private';

export interface RemoveBotResult {
  success: boolean;
  botLeft?: boolean;
  reason?: string;
  error?: string;
  code?: string;
}

export async function removeBotFromGuild(guild: Guild, session: Session): Promise<RemoveBotResult> {
  const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/bot`, {
    method: 'DELETE',
    headers: {
      Authorization: `Bearer ${session.jwt}`
    }
  });

  if (!res.ok) {
    let payload: RemoveBotResult = { success: false };
    try {
      payload = await res.json();
    } catch {
      payload = { success: false, error: 'Failed to remove bot' };
    }
    console.error(`[removeBotFromGuild] ${res.status} for guild ${guild.id}:`, JSON.stringify(payload));
    return { ...payload, success: false };
  }

  return await res.json();
}
