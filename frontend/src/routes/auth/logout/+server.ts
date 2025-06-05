import { redirect, type RequestHandler } from "@sveltejs/kit";
export const GET: RequestHandler = async ({ cookies }) => {
  cookies.delete('session', { path: '/' });

  throw redirect(302, "/");
}
