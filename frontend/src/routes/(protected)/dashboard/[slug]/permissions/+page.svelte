<script lang="ts">
	import SettingsItem from '$lib/components/settings/SettingsItem.svelte';
	import type {
		DiscordRole,
		DiscordMember,
		PermissionEntry,
		PermissionType
	} from '$lib/server/types';
	import { page } from '$app/state';
	import { enhance } from '$app/forms';
	import { Shield, Users, Crown, Search, Plus, Trash2 } from 'lucide-svelte';
	import { fly } from 'svelte/transition';
	import ApplyPanel from '$lib/components/settings/ApplyPanel.svelte';
	import { emojify } from '$lib/actions/emojify';
	import type { SubmitFunction } from '@sveltejs/kit';

	let roles: DiscordRole[] = $derived(page.data.roles);
	let members: DiscordMember[] = $derived(page.data.members);
	let permissions: PermissionEntry[] = $state([]);
	let activeTab: 'roles' | 'members' = $state('roles');
	let searchQuery: string = $state('');
	let saving: boolean = $state(false);
	let isModified: boolean = $state(false);

	const handleApply: SubmitFunction = async ({ formData }) => {
		formData.set('payload', JSON.stringify(permissions));
		saving = true;

		return async ({ update }) => {
			saving = false;
			isModified = false;
			await update();
		};
	};

	function handleRevert() {
		if (page.data.permissions) {
			permissions = structuredClone(page.data.permissions);
		} else {
			permissions = [];
		}
		isModified = false;
	}

	const PERMISSION_TYPES: PermissionType[] = [
		'DASHBOARD',
		'ADMIN',
		'MANAGE_QUOTES',
		'CREATE_QUOTES',
		'READ_QUOTES'
	];

	const PERMISSION_LABELS: Record<PermissionType, string> = {
		DASHBOARD: 'Dashboard',
		ADMIN: 'Admin',
		MANAGE_QUOTES: 'Manage',
		CREATE_QUOTES: 'Create',
		READ_QUOTES: 'Read'
	};

	const PERMISSION_COLORS: Record<PermissionType, string> = {
		DASHBOARD: 'bg-blue-500/20 text-blue-400 border-blue-500/30',
		ADMIN: 'bg-red-500/20 text-red-400 border-red-500/30',
		MANAGE_QUOTES: 'bg-amber-500/20 text-amber-400 border-amber-500/30',
		CREATE_QUOTES: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30',
		READ_QUOTES: 'bg-violet-500/20 text-violet-400 border-violet-500/30'
	};

	// Initialize permissions from server data
	$effect(() => {
		if (page.data.permissions) {
			permissions = structuredClone(page.data.permissions);
		}
	});

	function hasPermission(
		targetId: string,
		type: 'role' | 'user',
		permType: PermissionType
	): boolean {
		return permissions.some((p) =>
			type === 'role'
				? p.roleID === targetId && p.permissionType === permType
				: p.userID === targetId && p.permissionType === permType
		);
	}

	function togglePermission(targetId: string, type: 'role' | 'user', permType: PermissionType) {
		const exists = permissions.findIndex((p) =>
			type === 'role'
				? p.roleID === targetId && p.permissionType === permType
				: p.userID === targetId && p.permissionType === permType
		);

		if (exists >= 0) {
			permissions = permissions.filter((_, i) => i !== exists);
		} else {
			permissions = [
				...permissions,
				{
					userID: type === 'user' ? targetId : null,
					roleID: type === 'role' ? targetId : null,
					permissionType: permType
				}
			];
		}
		isModified = true;
	}

	let filteredRoles: DiscordRole[] = $derived(
		roles
			?.filter(
				(r) => r.name !== '@everyone' && r.name.toLowerCase().includes(searchQuery.toLowerCase())
			)
			.sort((a, b) => b.position - a.position) ?? []
	);

	let filteredMembers: DiscordMember[] = $derived(
		members
			?.filter((m) => {
				const name = m.nick || m.user.global_name || m.user.username;
				return name.toLowerCase().includes(searchQuery.toLowerCase());
			})
			.sort((a, b) => {
				const aName = a.nick || a.user.global_name || a.user.username;
				const bName = b.nick || b.user.global_name || b.user.username;
				return aName.localeCompare(bName);
			}) ?? []
	);

	function roleColor(color: number): string {
		if (color === 0) return '#99aab5';
		return '#' + color.toString(16).padStart(6, '0');
	}

	function memberAvatar(member: DiscordMember): string {
		if (member.user.avatar) {
			return `https://cdn.discordapp.com/avatars/${member.user.id}/${member.user.avatar}.png?size=32`;
		}
		const index = (BigInt(member.user.id) >> 22n) % 6n;
		return `https://cdn.discordapp.com/embed/avatars/${index}.png`;
	}
</script>

<div class="flex flex-col gap-8 p-8">
	<div class="text-light text-2xl font-semibold">Permissions</div>

	<!-- Main Card -->
	<div class="bg-dark flex flex-col gap-6 rounded-xl p-6">
		<!-- Toolbar -->
		<div class="flex flex-col justify-between gap-4 md:flex-row md:items-center">
			<!-- Tab Switcher -->
			<div class="bg-darker flex gap-1 rounded-lg p-1">
				<button
					class="flex items-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-all {activeTab ===
					'roles'
						? 'bg-primary text-white shadow-sm'
						: 'text-light/50 hover:text-light/80'}"
					onclick={() => (activeTab = 'roles')}
				>
					<span use:emojify={{}} class="h-4 w-4">👑</span>
					Roles ({filteredRoles.length})
				</button>
				<button
					class="flex items-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-all {activeTab ===
					'members'
						? 'bg-primary text-white shadow-sm'
						: 'text-light/50 hover:text-light/80'}"
					onclick={() => (activeTab = 'members')}
				>
					<span use:emojify={{}} class="h-4 w-4">👥</span>
					Members ({filteredMembers.length})
				</button>
			</div>

			<!-- Search -->
			<div class="relative w-full md:w-64">
				<Search class="text-light/30 absolute top-1/2 left-3 -translate-y-1/2" size={16} />
				<input
					type="text"
					placeholder="Search {activeTab}..."
					class="bg-darker text-light placeholder:text-light/30 focus:border-primary/50 focus:ring-primary/20 w-full rounded-lg border border-white/5 py-2.5 pr-4 pl-10 text-sm transition-all outline-none focus:ring-2"
					bind:value={searchQuery}
				/>
			</div>
		</div>

		<!-- Permission Type Legend -->
		<div class="flex flex-wrap gap-2">
			{#each PERMISSION_TYPES as permType}
				<div
					class="flex items-center gap-1.5 rounded-md border px-2.5 py-1 text-xs font-medium {PERMISSION_COLORS[
						permType
					]}"
				>
					{PERMISSION_LABELS[permType]}
				</div>
			{/each}
		</div>

		<!-- Permissions Grid -->
		<div class="bg-darker flex flex-col rounded-xl border border-white/5">
			<!-- Header -->
			<div class="border-b border-white/5 px-4 py-3">
				<div class="grid grid-cols-[1fr_repeat(5,_60px)] items-center gap-2">
					<div class="text-light/50 text-xs font-medium tracking-wider uppercase">
						{activeTab === 'roles' ? 'Role' : 'Member'}
					</div>
					{#each PERMISSION_TYPES as permType}
						<div
							class="text-center text-xs font-medium {PERMISSION_COLORS[permType].split(' ')[1]}"
						>
							{PERMISSION_LABELS[permType]}
						</div>
					{/each}
				</div>
			</div>

			<!-- Rows -->
			<div class="flex max-h-[500px] flex-col overflow-y-auto">
				{#if activeTab === 'roles'}
					{#each filteredRoles as role (role.id)}
						<div
							class="grid grid-cols-[1fr_repeat(5,_60px)] items-center gap-2 border-b border-white/5 px-4 py-3 transition-colors last:border-0 hover:bg-white/[0.02]"
							transition:fly={{ y: 5, duration: 200 }}
						>
							<div class="flex items-center gap-2.5">
								<div
									class="h-3 w-3 rounded-full"
									style="background-color: {roleColor(role.color)}"
								></div>
								<span class="text-light text-sm font-medium">{role.name}</span>
							</div>
							{#each PERMISSION_TYPES as permType}
								<div class="flex justify-center">
									<button
										class="h-5 w-5 rounded border transition-all {hasPermission(
											role.id,
											'role',
											permType
										)
											? PERMISSION_COLORS[permType] + ' border-current'
											: 'border-white/10 bg-white/5 hover:border-white/20'}"
										onclick={() => togglePermission(role.id, 'role', permType)}
									>
										{#if hasPermission(role.id, 'role', permType)}
											<svg
												class="h-full w-full p-0.5"
												viewBox="0 0 24 24"
												fill="none"
												stroke="currentColor"
												stroke-width="3"
											>
												<polyline points="20 6 9 17 4 12"></polyline>
											</svg>
										{/if}
									</button>
								</div>
							{/each}
						</div>
					{/each}
				{:else}
					{#each filteredMembers as member (member.user.id)}
						<div
							class="grid grid-cols-[1fr_repeat(5,_60px)] items-center gap-2 border-b border-white/5 px-4 py-3 transition-colors last:border-0 hover:bg-white/[0.02]"
							transition:fly={{ y: 5, duration: 200 }}
						>
							<div class="flex items-center gap-2.5">
								<img
									src={memberAvatar(member)}
									alt={member.user.username}
									class="h-6 w-6 rounded-full"
								/>
								<div class="flex flex-col">
									<span class="text-light text-sm font-medium">
										{member.nick || member.user.global_name || member.user.username}
									</span>
									{#if member.nick || member.user.global_name}
										<span class="text-light/30 text-xs">{member.user.username}</span>
									{/if}
								</div>
							</div>
							{#each PERMISSION_TYPES as permType}
								<div class="flex justify-center">
									<button
										class="h-5 w-5 rounded border transition-all {hasPermission(
											member.user.id,
											'user',
											permType
										)
											? PERMISSION_COLORS[permType] + ' border-current'
											: 'border-white/10 bg-white/5 hover:border-white/20'}"
										onclick={() => togglePermission(member.user.id, 'user', permType)}
									>
										{#if hasPermission(member.user.id, 'user', permType)}
											<svg
												class="h-full w-full p-0.5"
												viewBox="0 0 24 24"
												fill="none"
												stroke="currentColor"
												stroke-width="3"
											>
												<polyline points="20 6 9 17 4 12"></polyline>
											</svg>
										{/if}
									</button>
								</div>
							{/each}
						</div>
					{/each}
				{/if}

				{#if (activeTab === 'roles' && filteredRoles.length === 0) || (activeTab === 'members' && filteredMembers.length === 0)}
					<div class="text-light/30 flex flex-col items-center gap-2 py-12 text-sm">
						<Search size={24} />
						<span>No {activeTab} found</span>
					</div>
				{/if}
			</div>
		</div>
	</div>

	<!-- Save Bar -->
	{#if isModified}
		<div class="sticky bottom-8 mx-8" transition:fly={{ y: 10, duration: 300 }}>
			<ApplyPanel {handleApply} {handleRevert} action="?/updatePermissions" />
		</div>
	{/if}
</div>
