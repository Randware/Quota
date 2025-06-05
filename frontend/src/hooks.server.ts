import { dev } from '$app/environment';
import { refreshSession } from '$lib/server/auth';
import type { Session } from '$lib/server/types';
import { redirect, type Handle } from '@sveltejs/kit';

export const handle: Handle = async ({ event, resolve }) => {
  const sessionRaw: string | undefined = event.cookies.get("session");

  if (sessionRaw) {
    try {
      let sessionCache: Session = JSON.parse(sessionRaw) as Session;

      let session: Session = await refreshSession(sessionCache);

      event.locals.session = session;

      event.cookies.set("session", JSON.stringify(session), {
        path: '/',
        httpOnly: true,
        sameSite: 'lax',
        secure: !dev,
      });

    } catch (e) { console.log(e); }
  }


  if (!event.locals.session && event.route?.id?.startsWith('/(protected)/')) {
    throw redirect(302, '/');
  }

  return await resolve(event);
};
