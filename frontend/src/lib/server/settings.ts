import { dev } from "$app/environment";
import type { Channel, Guild, Settings } from "./types";

const MOCK_SETTINGS: Settings = {
  allowedChannels: [{ id: "123545151", name: "memes" }, { id: "591923213", name: "quotes-📜" }, { id: "104915051", name: "general" }, { id: "1589238924", name: "long-channel-name-that-does-not-fit" }],
  lockAllowedChannels: true,
  upvoteEmoji: ":thumbsup:",
  downvoteEmoji: ":thumbsdown:",
  allowVoting: true,
  comments: false
};

const MOCK_CHANNELS: Channel[] = [
  { id: 'testid', name: 'john-channel' },
  { id: '123123412451224', name: 'am-a-channel' }
];

export async function updateGuildSettings(guild: Guild, newSettings: Settings): Promise<void> {
  //  TODO: Query backend here
}

export async function getGuildSettings(guild: Guild): Promise<Settings> {
  if (dev) {
    return MOCK_SETTINGS;
  }

  //  TODO: Query backend
  const settings = MOCK_SETTINGS;

  return settings;
}

export async function getGuildChannels(guild: Guild): Promise<Channel[]> {
  if (dev) {
    return MOCK_CHANNELS;
  }

  //  TODO: Query backend
  const channels = MOCK_CHANNELS;

  return channels;
}
