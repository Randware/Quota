import { dev } from "$app/environment";
import { jwtDecode } from "jwt-decode";
import type { Guild, Session } from "./types";
import { BACKEND_HOST } from "$env/static/private";

const REFRESH_BUFFER: number = 10_000;

const MOCK_SESSION: Session = { jwt: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", token: "ref", username: "lukaschi", avatar: "https://cdn.discordapp.com/avatars/708699937629536283/08a81c1e2892438b65b2aceea3175854.webp" }
const MOCK_GUILDS: Guild[] = [
  {
    id: "123456789012345678",
    name: "Randware Testcord",
    icon: "https://cdn.discordapp.com/icons/1331222419469500499/64728f405bfc922dca201cfd01c92cca.png?quality=lossless",
    bot: true,
  },
  {
    id: "128458178294513563",
    name: "Hall of Benkö",
    icon: "https://cdn.discordapp.com/icons/639150417527439389/09471eaea82150c2f18d9816a5360e12.png?quality=lossless",
    bot: true,
  },
  {
    id: "312351256342724557234",
    name: "?????",
    icon: "https://cdn.discordapp.com/icons/482149505735589889/ba54db006c13f479f6dd9c2bb1936f22.png?quality=lossless",
    bot: false,
  },
  {
    id: "9125125132526344637",
    name: "Bot Test",
    icon: "https://cdn.discordapp.com/icons/1102520437231067136/8e9ea8dabfd9dcde0e04efd92e08dac0.png?quality=lossless",
    bot: false,
  },
  {
    id: "846423468203985",
    name: "SpengerBenger",
    icon: "https://cdn.discordapp.com/icons/359219112154497026/5b4f5f3cc833817a7fdeac9de658faa0.png?quality=lossless",
    bot: false,
  },
  {
    id: "553458461286369633",
    name: "A discord server",
    icon: "https://cdn.discordapp.com/icons/667838772343341059/9a1a2b208c2dd4360c6890d3d839f2a3.png?quality=lossless",
    bot: false,
  },
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
