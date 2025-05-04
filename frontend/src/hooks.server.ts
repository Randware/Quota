import { getSession } from '$lib/server/auth';
import { redirect, type Handle } from '@sveltejs/kit';

export const handle: Handle = async ({ event, resolve }) => {
  const token = event.cookies.get('session-token');

  if (token) {
    event.locals.session = await getSession(token);
  }

  if (!event.locals.session && event.route?.id?.startsWith('/(protected)/')) {
    throw redirect(302, '/');
  }

  return await resolve(event);
};
