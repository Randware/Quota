<script lang="ts">
	import GuildItem from '$lib/components/dashboard/GuildItem.svelte';
	import type { Guild } from '$lib/server/types';
	import { page } from '$app/state';
	import SkeletonSquare from '$lib/components/ui/SkeletonSquare.svelte';

	let userGuilds: Promise<Guild[]> = $derived(page.data.userGuilds);
</script>

<div class="flex h-full w-full flex-col gap-8 overflow-y-auto p-8">
	<div class="text-light text-center text-2xl font-semibold">Select a server</div>

	<div class="flex flex-wrap justify-center gap-4">
		{#await userGuilds}
			<SkeletonSquare width={320} height={250} />
			<SkeletonSquare width={320} height={250} />
			<SkeletonSquare width={320} height={250} />
			<SkeletonSquare width={320} height={250} />
		{:then userGuilds}
			{#if userGuilds.length > 0}
				{#each userGuilds as guild}
					<GuildItem {guild} />
				{/each}
			{:else}
				<div class="text-light text-xl font-medium">There are no servers you can manage :(</div>
			{/if}
		{/await}
	</div>
</div>
