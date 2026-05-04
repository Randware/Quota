import { jwtDecode } from "jwt-decode";
import type { Guild, Session } from "./types";
import { BACKEND_HOST } from "$env/static/private";

const REFRESH_BUFFER: number = 10_000;

// Authorizes a new user
export async function createSession(code: string, redirectURI: string): Promise<Session> {
  let tokensRes = await fetch(`${BACKEND_HOST}/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ code, redirectUri: redirectURI })
  })

  if (!tokensRes.ok) {
    throw new Error(`Login failed: ${tokensRes.status} ${await tokensRes.text()}`);
  }

  let { jwt, refreshToken }: { jwt: string, refreshToken: string } = await tokensRes.json();

  let userID = jwtDecode(jwt).sub;

  let userRes = await fetch(`${BACKEND_HOST}/user/${userID}/info`, {
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${jwt}`
    },
  });

  if (!userRes.ok) {
    throw new Error(`Failed to fetch user info: ${userRes.status}`);
  }

  let { username, avatar } = await userRes.json();
  const avatarUrl = avatar ? `https://cdn.discordapp.com/avatars/${userID}/${avatar}.png?size=256` : "";

  return { jwt, token: refreshToken, username, avatar: avatarUrl };
}

export async function getUserGuilds(session: Session): Promise<Guild[]> {
  const userID = jwtDecode(session.jwt).sub;
  if (!userID) return [];

  // Fetch user's Discord guilds
  const guildsRes = await fetch(`${BACKEND_HOST}/user/${userID}/guilds`, {
    headers: {
      "Authorization": `Bearer ${session.jwt}`
    }
  });

  if (!guildsRes.ok) return [];

  const discordGuilds: { id: string; name: string; icon: string | null; permissions: number }[] = await guildsRes.json();

  // Show all guilds where user has MANAGE_GUILD (0x20) or ADMINISTRATOR (0x8)
  const MANAGE_GUILD = 0x20;
  const ADMINISTRATOR = 0x8;

  const userAdminGuilds = discordGuilds.filter(
    (g) => (g.permissions & MANAGE_GUILD) !== 0 || (g.permissions & ADMINISTRATOR) !== 0
  );

  // Check which of these guilds the bot is in
  const checkRes = await fetch(`${BACKEND_HOST}/user/${userID}/check-guilds`, {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${session.jwt}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify(userAdminGuilds.map((g) => g.id))
  });

  const botGuildIds: string[] = checkRes.ok ? await checkRes.json() : [];

  const guilds: Guild[] = userAdminGuilds
    .map((g) => ({
      id: g.id,
      name: g.name,
      icon: g.icon ? `https://cdn.discordapp.com/icons/${g.id}/${g.icon}.png?size=256` : "",
      bot: botGuildIds.includes(g.id),
    }))
    .sort((a, b) => {
      // Bot guilds first, then alphabetical
      if (a.bot !== b.bot) return a.bot ? -1 : 1;
      return a.name.localeCompare(b.name);
    });

  return guilds;
}

export async function getBotPermissions(): Promise<number> {
  const res = await fetch(`${BACKEND_HOST}/bot/invite`);

  if (!res.ok) return 0;

  const { inviteUrl }: { inviteUrl: string } = await res.json();

  try {
    const url = new URL(inviteUrl);
    const permissions = url.searchParams.get("permissions");
    return permissions ? parseInt(permissions, 10) : 0;
  } catch {
    return 0;
  }
}

export async function isJWTExpired(jwt: string) {
  const token = jwtDecode(jwt);

  let exp: number = token.exp || 0;

  // JWT exp is in seconds, Date.now() is in milliseconds
  return (exp * 1000) <= Date.now() + REFRESH_BUFFER;
}

export async function refreshSession(session: Session, force: boolean = false): Promise<Session> {
  if (force || await isJWTExpired(session.jwt)) {
    let refreshRes = await fetch(`${BACKEND_HOST}/auth/refresh`, {
      body: JSON.stringify({ refreshToken: session.token }),
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
    });

    if (!refreshRes.ok) {
      throw new Error("Failed to refresh session");
    }

    let { jwt, refreshToken }: { jwt: string, refreshToken: string } = await refreshRes.json();

    let userID = jwtDecode(jwt).sub;

    let userRes = await fetch(`${BACKEND_HOST}/user/${userID}/info`, {
      headers: {
        "Authorization": `Bearer ${jwt}`
      }
    });

    if (!userRes.ok) {
      throw new Error("Failed to fetch user info");
    }

    let { username, avatar }: { username: string, avatar: string } = await userRes.json();
    const avatarUrl = avatar ? `https://cdn.discordapp.com/avatars/${userID}/${avatar}.png?size=256` : "";

    return { jwt, token: refreshToken, username, avatar: avatarUrl };
  }

  return session;
}

export async function revokeSession(session: Session): Promise<void> {
  await fetch(`${BACKEND_HOST}/auth/revoke`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ refreshToken: session.token })
  });
}
