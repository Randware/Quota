import { DISCORD_CLIENT_ID, DISCORD_REDIRECT_URI } from "$env/static/private";
import { getBotPermissions } from "$lib/server/auth";
import { redirect, type RequestHandler } from "@sveltejs/kit";

export const GET: RequestHandler = async ({ url }) => {
  const permissions = await getBotPermissions();
  const guild_id = url.searchParams.get('guild_id');

  const scopes: string[] = ["bot", "applications.commands"];

  const params = new URLSearchParams({
    client_id: DISCORD_CLIENT_ID,
    permissions: permissions.toString(),
    scope: scopes.join(' '),
    redirect_uri: DISCORD_REDIRECT_URI,
    response_type: 'code'
  });

  if (guild_id) {
    params.set('guild_id', guild_id);
    params.set('disable_guild_select', 'true');
  }

  throw redirect(302, `https://discord.com/oauth2/authorize?${params}`);
}
