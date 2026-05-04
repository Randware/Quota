import { BACKEND_HOST } from '$env/static/private';
import type { Guild, Session, DiscordRole, DiscordMember, PermissionEntry } from './types';

export async function getGuildRoles(guild: Guild, session: Session): Promise<DiscordRole[]> {
    const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/roles`, {
        headers: { Authorization: `Bearer ${session.jwt}` }
    });
    if (!res.ok) return [];
    return res.json();
}

export async function getGuildMembers(guild: Guild, session: Session): Promise<DiscordMember[]> {
    const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/members`, {
        headers: { Authorization: `Bearer ${session.jwt}` }
    });
    if (!res.ok) return [];
    return res.json();
}

export async function getGuildPermissions(guild: Guild, session: Session): Promise<PermissionEntry[]> {
    const res = await fetch(`${BACKEND_HOST}/server/${guild.id}/permissions`, {
        headers: { Authorization: `Bearer ${session.jwt}` }
    });
    if (!res.ok) return [];
    return res.json();
}

export async function updateGuildPermissions(guild: Guild, permissions: PermissionEntry[], session: Session): Promise<void> {
    await fetch(`${BACKEND_HOST}/server/${guild.id}/permissions`, {
        method: 'POST',
        headers: {
            Authorization: `Bearer ${session.jwt}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(permissions)
    });
}
