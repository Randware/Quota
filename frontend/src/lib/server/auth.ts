import { dev } from "$app/environment";

export interface Session {
  auth_token: string;
  refresh_token: string;
  userName: string;
  userAvatar: string;
}

const MOCK_TOKEN: string = "MOCK_TOKEN";
const MOCK_SESSION: Session = { auth_token: "1234567890", refresh_token: "1234567890", userName: "mockuser", userAvatar: "" }

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
