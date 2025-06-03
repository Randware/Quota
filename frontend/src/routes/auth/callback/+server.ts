import { dev } from "$app/environment";
import { DISCORD_CLIENT_ID, DISCORD_CLIENT_SECRET, DISCORD_REDIRECT_URI } from "$env/static/private";
import { createSession } from "$lib/server/auth";
import { redirect, type RequestHandler } from "@sveltejs/kit";

export const GET: RequestHandler = async ({ url, fetch, cookies }) => {
  const guildID = url.searchParams.get('guild_id');

  // If we have a guild ID, this is a bot invite callback
  if (guildID) {
    return new Response(null, {
      status: 302,
      headers: {
        Location: `/dashboard/${guildID}`
      }
    });
  }

  // Otherwise it is an user authorization callback

  const urlState: string | null = url.searchParams.get("state");
  const storedState: string | undefined = cookies.get('state');

  //  TODO: Show error here
  if (!urlState || !storedState || urlState !== storedState) {
    throw redirect(302, '/');
  }

  const code: string | null = url.searchParams.get("code");

  if (!code) {
    throw redirect(302, "/")
  }

  cookies.delete('state', { path: '/' });

  const data = new URLSearchParams({
    client_id: DISCORD_CLIENT_ID,
    client_secret: DISCORD_CLIENT_SECRET,
    redirect_uri: DISCORD_REDIRECT_URI,
    grant_type: 'authorization_code',
    code,
  });

  const tokenRes = await fetch('https://discord.com/api/oauth2/token', {
    method: 'POST',
    body: data,
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
  });

  const { token_type, access_token, refresh_token, expires_in } = await tokenRes.json();

  const userRes = await fetch('https://discord.com/api/users/@me', {
    headers: { Authorization: `${token_type} ${access_token}` }
  });

  const { id } = await userRes.json();

  const token = await createSession({
    discordID: id,
    accessToken: access_token,
    refreshToken: refresh_token,
    expiresIn: expires_in
  })

  cookies.set('session-token', token, {
    path: '/',
    httpOnly: true,
    sameSite: 'lax',
    secure: !dev,
    maxAge: expires_in //  TODO: Use the actual database expiration data
  });

  throw redirect(302, '/dashboard');
}

