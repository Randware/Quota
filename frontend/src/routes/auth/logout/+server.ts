import { dev } from "$app/environment";
import { revokeSession } from "$lib/server/auth";
import { redirect, type RequestHandler } from "@sveltejs/kit";

export const GET: RequestHandler = async ({ cookies, locals }) => {
    if (locals.session) {
        try {
            await revokeSession(locals.session);
        } catch {
            // Best-effort revocation — proceed to clear cookie regardless
        }
    }

    cookies.delete('session', {
        path: '/',
        httpOnly: true,
        sameSite: 'lax',
        secure: !dev,
    });

    throw redirect(302, '/');
}
