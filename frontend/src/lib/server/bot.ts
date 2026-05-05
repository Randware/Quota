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
  let res: Response;
  try {
    res = await fetch(`${BACKEND_HOST}/server/${guild.id}/bot`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${session.jwt}`
      }
    });
  } catch (e) {
    console.error(`[removeBotFromGuild] fetch threw for guild ${guild.id}:`, e);
    return { success: false, error: 'Could not reach backend.' };
  }

  if (!res.ok) {
    const raw = await res.text();
    console.error(`[removeBotFromGuild] ${res.status} for guild ${guild.id}: ${raw}`);
    let payload: RemoveBotResult = { success: false, error: 'Failed to remove bot' };
    try {
      payload = JSON.parse(raw);
    } catch {
      // raw body was not JSON
    }
    return { ...payload, success: false };
  }

  return await res.json();
}
