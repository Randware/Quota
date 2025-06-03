import { DISCORD_CLIENT_ID, DISCORD_REDIRECT_URI } from "$env/static/private";
import { getBotPermissions } from "$lib/server/auth";
import { redirect, type RequestHandler } from "@sveltejs/kit";

export const GET: RequestHandler = async ({ }) => {
  const permissions = await getBotPermissions();

  const scopes: string[] = ["bot", "applications.commands"];

  const params = new URLSearchParams({
    client_id: DISCORD_CLIENT_ID,
    permissions: permissions.toString(),
    scope: scopes.join(' '),
    redirect_uri: DISCORD_REDIRECT_URI,
    response_type: 'code'
  });

  throw redirect(302, `https://discord.com/oauth2/authorize?${params}`);
}
