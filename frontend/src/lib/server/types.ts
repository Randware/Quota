export interface Settings {
  allowedChannels: Channel[];
  lockAllowedChannels: boolean;
  upvoteEmoji: TextEmoji | ImageEmoji;
  downvoteEmoji: TextEmoji | ImageEmoji;
  allowVoting: boolean;
  comments: boolean;
}

export interface Stats {
  totalQuotes: number;
  totalUpvotes: number;
  totalDownvotes: number;
  quotesByMonth: MonthlyQuotes[];
  topQuotees: TopQuotee[];
  topQuotes: TopQuote[];
}

export interface MonthlyQuotes {
  year: number;
  month: number;
  count: number;
}

export interface TopQuotee {
  name: string;
  count: number;
}

export interface TopQuote {
  content: string;
  upvotes: number;
  downvotes: number;
  score: number;
  createdAt: string | null;
}

export interface Session {
  jwt: string;
  token: string;
  username: string;
  avatar: string;
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
