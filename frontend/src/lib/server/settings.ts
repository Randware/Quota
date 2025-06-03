import { dev } from "$app/environment";
import type { Guild, Settings } from "./types";

const MOCK_SETTINGS: Settings = {
  allowedChannels: ["#memes", "#quotes-📜", "#general", "#long-channel-name-that-does-not-fit"],
  lockAllowedChannels: true,
  upvoteEmoji: ":thumbsup:",
  downvoteEmoji: ":thumbsdown:",
  allowVoting: true,
  comments: false
};

export async function getGuildSettings(guild: Guild): Promise<Settings> {
  if (dev) {
    return MOCK_SETTINGS;
  }

  //  TODO: Query backend
  const settings = MOCK_SETTINGS;

  return settings;
}
