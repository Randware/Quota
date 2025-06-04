export interface Settings {
  allowedChannels: Channel[];
  lockAllowedChannels: boolean;
  upvoteEmoji: string;
  downvoteEmoji: string;
  allowVoting: boolean;
  comments: boolean;
}

export interface Stats {
  totalQuotes: number;
}

export interface Session {
  auth_token: string;
  refresh_token: string;
  userName: string;
  userAvatar: string;
}

export interface Guild {
  id: string;
  name: string;
  icon: string;
  bot: boolean;
}

export interface TextEmoji {
  id: string;
  name: string;
  native: string;
}

export interface ImageEmoji {
  id: string;
  name: string;
  src: string;
}

export interface CustomEmojiCollection {
  id: string;
  name: string;
  emojis: ImageEmoji[];
}

export interface Channel {
  id: string;
  name: string;
}

export interface GuildConfig {
  allowedChannels: Channel[];
  lockAllowedChannels: boolean;
}
