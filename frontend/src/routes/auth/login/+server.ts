import { dev } from "$app/environment";
import { env } from "$env/dynamic/private";
import { redirect, type RequestHandler } from "@sveltejs/kit";
export const GET: RequestHandler = async ({ cookies }) => {
  const state: string = crypto.randomUUID();

  cookies.set('state', state, {
    path: '/',
    httpOnly: true,
    sameSite: 'lax',
    secure: !dev,
  });

  //  TODO: Maybe fetch this from backend
  const params = new URLSearchParams({
    client_id: env.DISCORD_CLIENT_ID ?? '',
    redirect_uri: env.DISCORD_REDIRECT_URI ?? '',
    response_type: "code",
    scope: "identify guilds",
    state
  });

  throw redirect(302, `https://discord.com/oauth2/authorize?${params}`, { external: ['https://discord.com'] });
}
