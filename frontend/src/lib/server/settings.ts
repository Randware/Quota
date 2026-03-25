import type { Channel, Guild, ImageEmoji, Session, Settings, TextEmoji } from "./types";
import { BACKEND_HOST } from "$env/static/private";

const DEFAULT_SETTINGS: Settings = {
  allowedChannels: [],
  lockAllowedChannels: false,
  upvoteEmoji: { id: 'thumbsup', name: 'Thumbs Up', native: '👍' } as TextEmoji,
  downvoteEmoji: { id: 'thumbsdown', name: 'Thumbs Down', native: '👎' } as TextEmoji,
  allowVoting: true,
  comments: false
};

/**
 * Converts a frontend emoji object to the format the backend expects.
 * - TextEmoji (native unicode) → plain string: "👍"
 * - ImageEmoji (custom Discord) → object: { type, id, name, animated }
 */
function emojiToBackend(emoji: TextEmoji | ImageEmoji): string | object {
  if ('native' in emoji) {
    return emoji.native;
  } else {
    return {
      type: "custom",
      id: emoji.id,
      name: emoji.name,
      animated: false
    };
  }
}

/**
 * Converts a backend emoji response to a frontend emoji object.
 */
function emojiFromBackend(data: unknown, fallbackNative: string): TextEmoji | ImageEmoji {
  if (typeof data === 'string') {
    return { id: data, name: data, native: data } as TextEmoji;
  } else if (data && typeof data === 'object') {
    const obj = data as Record<string, unknown>;
    if (obj.url || obj.type === 'custom') {
      return {
        id: (obj.id as string) ?? '',
        name: (obj.name as string) ?? '',
        src: (obj.url as string) ?? `https://cdn.discordapp.com/emojis/${obj.id}.${obj.animated ? 'gif' : 'png'}`
      } as ImageEmoji;
    }
    return { id: (obj.name as string) ?? fallbackNative, name: (obj.name as string) ?? fallbackNative, native: (obj.name as string) ?? fallbackNative } as TextEmoji;
  }
  return { id: fallbackNative, name: fallbackNative, native: fallbackNative } as TextEmoji;
}

export async function updateGuildSettings(guild: Guild, newSettings: Settings, session: Session): Promise<void> {
  const body: Record<string, unknown> = {
    allowVoting: newSettings.allowVoting,
    lockAllowedChannels: newSettings.lockAllowedChannels,
    allowComments: newSettings.comments,
    upvoteEmoji: emojiToBackend(newSettings.upvoteEmoji),
    downvoteEmoji: emojiToBackend(newSettings.downvoteEmoji),
    allowedChannels: newSettings.allowedChannels.map(ch => ch.id),
  };

  await fetch(`${BACKEND_HOST}/server/${guild.id}/config`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${session.jwt}`
    },
    body: JSON.stringify(body)
  });
}

export async function getGuildSettings(guild: Guild, session: Session): Promise<Settings> {
  const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/config`, {
    headers: {
      "Authorization": `Bearer ${session.jwt}`
    }
  });

  if (!res.ok) {
    return DEFAULT_SETTINGS;
  }

  const data = await res.json();

  // Fetch all channels so we can resolve names for allowed channel IDs
  const allChannels = await getGuildChannels(guild, session);
  const channelMap = new Map(allChannels.map(ch => [ch.id, ch.name]));

  const allowedChannels: Channel[] = (data.allowedChannels ?? []).map((id: string) => ({
    id,
    name: channelMap.get(id) ?? id
  }));

  return {
    allowedChannels,
    lockAllowedChannels: data.lockAllowedChannels ?? false,
    upvoteEmoji: emojiFromBackend(data.upvoteEmoji, "👍"),
    downvoteEmoji: emojiFromBackend(data.downvoteEmoji, "👎"),
    allowVoting: data.allowVoting ?? true,
    comments: data.allowComments ?? false
  };
}

export async function getGuildChannels(guild: Guild, session: Session): Promise<Channel[]> {
  const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/channels`, {
    headers: {
      "Authorization": `Bearer ${session.jwt}`
    }
  });

  if (!res.ok) {
    return [];
  }

  const discordChannels: { id: string; name: string; type: number }[] = await res.json();

  // Only return text channels (type 0) and announcement channels (type 5)
  return discordChannels
    .filter(ch => ch.type === 0 || ch.type === 5)
    .map(ch => ({
      id: ch.id,
      name: ch.name
    }));
}
