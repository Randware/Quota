<script lang="ts">
	import type { Channel } from '$lib/server/types';
	import { Plus } from 'lucide-svelte';
	import ChannelRemoveItem from './ChannelRemoveItem.svelte';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';
	import NoChannels from './NoChannels.svelte';

	let {
		switchView,
		removeChannel,
		channels = $bindable()
	} = $props<{
		switchView: () => void;
		removeChannel: (channel: Channel) => void;
		channels: Channel[];
	}>();
</script>

<div class="flex flex-col gap-4">
	<ButtonPrimary onclick={switchView}>
		<div class="text-light flex w-full items-center px-4 py-2">
			<Plus />
			<div class="flex-1 text-center font-semibold">Add channels</div>
		</div>
	</ButtonPrimary>

	<div class="relative flex flex-col gap-2">
		{#if channels.length > 0}
			{#each channels as channel}
				<ChannelRemoveItem {channel} remove={removeChannel} />
			{/each}
		{:else}
			<NoChannels />
		{/if}
	</div>
</div>
