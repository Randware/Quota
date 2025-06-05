import { dev } from "$app/environment";
import { DISCORD_REDIRECT_URI } from "$env/static/private";
import { createSession } from "$lib/server/auth";
import type { Session } from "$lib/server/types";
import { redirect, type RequestHandler } from "@sveltejs/kit";

export const GET: RequestHandler = async ({ url, cookies }) => {
  const guildID = url.searchParams.get('guild_id');

  // If we have a guild ID, this is a bot invite callback
  if (guildID) {
    throw redirect(302, `/dashboard/${guildID}`);
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

  const session: Session = await createSession(code, DISCORD_REDIRECT_URI);

  cookies.set('session', JSON.stringify(session), {
    path: '/',
    httpOnly: true,
    sameSite: 'lax',
    secure: !dev,
  });

  throw redirect(302, '/dashboard');
}

