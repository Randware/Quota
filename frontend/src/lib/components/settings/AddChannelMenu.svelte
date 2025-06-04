<script lang="ts">
	import type { Channel } from '$lib/server/types';
	import { Trash2 } from 'lucide-svelte';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';
	import ChannelAddItem from './ChannelAddItem.svelte';
	import NoChannels from './NoChannels.svelte';

	let {
		switchView,
		addChannel,
		channels = $bindable()
	} = $props<{
		switchView: () => void;
		addChannel: (channel: Channel) => void;
		channels: Channel[];
	}>();
</script>

<div class="flex flex-col gap-4">
	<ButtonPrimary onclick={switchView}>
		<div class="text-light flex w-full items-center px-4 py-2">
			<Trash2 />
			<div class="flex-1 text-center font-semibold">Remove channels</div>
		</div>
	</ButtonPrimary>

	<div class="relative flex flex-col gap-2">
		{#if channels.length > 0}
			{#each channels as channel}
				<ChannelAddItem {channel} add={addChannel} />
			{/each}
		{:else}
			<NoChannels />
		{/if}
	</div>
</div>
