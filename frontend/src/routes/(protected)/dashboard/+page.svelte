<script lang="ts">
	import GuildItem from '$lib/components/dashboard/GuildItem.svelte';
	import type { Guild } from '$lib/server/types';
	import { page } from '$app/state';
	import SkeletonSquare from '$lib/components/ui/SkeletonSquare.svelte';

	let userGuilds: Promise<Guild[]> = $derived(page.data.userGuilds);
</script>

<div class="flex h-full w-full flex-col gap-8 overflow-y-auto p-8 md:p-12">
	<div class="text-light text-center text-3xl font-bold">Select a Server</div>

	{#await userGuilds}
		<div class="flex flex-wrap justify-center gap-4">
			<SkeletonSquare width={320} height={250} />
			<SkeletonSquare width={320} height={250} />
			<SkeletonSquare width={320} height={250} />
			<SkeletonSquare width={320} height={250} />
		</div>
	{:then guilds}
		{@const botGuilds = guilds.filter((g) => g.bot)}
		{@const nonBotGuilds = guilds.filter((g) => !g.bot)}

		{#if botGuilds.length > 0}
			<div class="flex flex-col gap-4">
				<div class="flex items-center gap-3">
					<div
						class="bg-primary/20 text-primary flex h-8 w-8 items-center justify-center rounded-lg text-sm font-bold"
					>
						{botGuilds.length}
					</div>
					<h2 class="text-light/80 text-lg font-semibold">Your Servers</h2>
				</div>
				<div class="flex flex-wrap gap-4">
					{#each botGuilds as guild (guild.id)}
						<GuildItem {guild} />
					{/each}
				</div>
			</div>
		{/if}

		{#if nonBotGuilds.length > 0}
			<div class="flex flex-col gap-4">
				<div class="flex items-center gap-3">
					<div
						class="bg-light/10 text-light/50 flex h-8 w-8 items-center justify-center rounded-lg text-sm font-bold"
					>
						{nonBotGuilds.length}
					</div>
					<h2 class="text-light/50 text-lg font-semibold">Add Quota</h2>
				</div>
				<div class="flex flex-wrap gap-4">
					{#each nonBotGuilds as guild (guild.id)}
						<GuildItem {guild} />
					{/each}
				</div>
			</div>
		{/if}

		{#if guilds.length === 0}
			<div class="text-light/50 text-center text-xl font-medium">
				No servers found where you have admin permissions.
			</div>
		{/if}
	{/await}
</div>
