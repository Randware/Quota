import { dev } from "$app/environment";
import type { Guild, Settings } from "./types";

const MOCK_SETTINGS: Settings = {
  allowedChannels: [{ id: "123545151", name: "memes" }, { id: "591923213", name: "quotes-📜" }, { id: "104915051", name: "general" }, { id: "1589238924", name: "long-channel-name-that-does-not-fit" }],
  lockAllowedChannels: true,
  upvoteEmoji: ":thumbsup:",
  downvoteEmoji: ":thumbsdown:",
  allowVoting: true,
  comments: false
};

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
