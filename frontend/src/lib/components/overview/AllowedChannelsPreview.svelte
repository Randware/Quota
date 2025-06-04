<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import type { Guild, Settings } from '$lib/server/types';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';
	import SkeletonSquare from '../ui/SkeletonSquare.svelte';

	let guild: Guild = $derived(page.data.guild);
	let settings: Promise<Settings> = $derived(page.data.settings);
</script>

<div class="bg-dark flex flex-col gap-6 rounded-xl p-4">
	<div class="text-light text-2xl font-semibold">
		<span class="text-primary">Allowed</span> channels
	</div>

	<div class="mb-auto flex flex-wrap gap-2">
		{#await settings}
			<SkeletonSquare width={90} height={40} />
			<SkeletonSquare width={140} height={40} />
			<SkeletonSquare width={70} height={40} />
			<SkeletonSquare width={110} height={40} />
		{:then settings}
			{#each settings.allowedChannels as channel}
				<div class="bg-darker text-light text-md rounded-xl p-2 font-medium">#{channel.name}</div>
			{/each}
		{/await}
	</div>

	<ButtonPrimary onclick={() => goto(`/dashboard/${guild.id}/settings`)}>
		<div class="text-light px-4 py-2 font-semibold">Configure</div>
	</ButtonPrimary>
</div>
