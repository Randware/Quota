import { dev } from "$app/environment";
import { jwtDecode } from "jwt-decode";
import type { Guild, Session } from "./types";
import { BACKEND_HOST } from "$env/static/private";

const REFRESH_BUFFER: number = 10_000;

const MOCK_SESSION: Session = { jwt: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", token: "ref", username: "mockuser", avatar: "" }
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

// Authorizes a new user
export async function createSession(code: string, redirectURI: string): Promise<Session> {
  if (dev) {
    return MOCK_SESSION;
  }

  let tokensRes = await fetch(`${BACKEND_HOST}/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ code, redirectUri: redirectURI })
  })

  let { jwt, refreshToken }: { jwt: string, refreshToken: string } = await tokensRes.json();

  let userID = jwtDecode(jwt).sub;

  let userRes = await fetch(`${BACKEND_HOST}/user/${userID}/info`, {
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${jwt}`
    },
  });

  let { username, avatar } = await userRes.json();

  return { jwt, token: refreshToken, username, avatar };
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

export async function isJWTExpired(jwt: string) {
  const token = jwtDecode(jwt);

  let exp: number = token.exp || Date.now();

  return exp <= Date.now() + REFRESH_BUFFER;
}

export async function refreshSession(session: Session): Promise<Session> {
  if (dev) {
    return MOCK_SESSION;
  }

  if (await isJWTExpired(session.jwt)) {
    let refreshRes = await fetch(`${BACKEND_HOST}/auth/refresh`, {
      body: JSON.stringify({ refreshToken: session.token }),
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
    });

    let { jwt, refreshToken }: { jwt: string, refreshToken: string } = await refreshRes.json();

    let userID = jwtDecode(jwt).sub;

    let userRes = await fetch(`${BACKEND_HOST}/user/${userID}/info`);

    let { username, avatar }: { username: string, avatar: string } = await userRes.json();

    return { jwt, token: refreshToken, username, avatar };
  }

  return session;
}
