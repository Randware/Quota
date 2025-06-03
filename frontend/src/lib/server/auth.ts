import { dev } from "$app/environment";
import type { Guild, Session } from "./types";

const MOCK_TOKEN: string = "MOCK_TOKEN";
const MOCK_SESSION: Session = { auth_token: "1234567890", refresh_token: "1234567890", userName: "mockuser", userAvatar: "" }
const MOCK_GUILDS: Guild[] = [
  {
    id: "123456789012345678",
    name: "TestServer",
    icon: "wrvQFCCpJF9uUguDArXnBvJZ67oZqoyu",
    bot: true,
  },
  {
    id: "553458461286369633",
    name: "CoolServer",
    icon: "wrvQFCCpJF9uUguDArXnBvJZ67oZqoyu",
    bot: false,
  }
];
const MOCK_PERMISSIONS: number = 8;

// Authorizes a new user and returns a session token
export async function createSession(session: { discordID: string, accessToken: string, refreshToken: string, expiresIn: number }): Promise<string> {
  if (dev) {
    return MOCK_TOKEN;
  }

  //  TODO: Query backend here
  const token: string = "";

  return token;
}

// Get information related to a session from a token
export async function getSession(token: string): Promise<Session | null> {
  if (dev && token === MOCK_TOKEN) {
    return MOCK_SESSION;
  }

  //  TODO: Query backend here
  const session: Session | null = null;

  return session;
}

export async function getUserGuilds(session: Session): Promise<Guild[]> {
  if (dev) {
    return MOCK_GUILDS;
  }

  //  TODO: Query backend here
  const guilds: Guild[] = [];

  return guilds;
}

export async function getBotPermissions(): Promise<number> {
  if (dev) {
    return MOCK_PERMISSIONS;
  }

  //  TODO: Query backend here
  const scopes: number = 0;

  return scopes;
}
