export interface Session {
  auth_token: string;
  refresh_token: string;
  userName: string;
  userAvatar: string;
}

export interface Channel {
  id: string;
  name: string;
}

export interface GuildConfig {
  allowedChannels: Channel[];
  lockAllowedChannels: boolean;
}
