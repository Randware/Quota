<script lang="ts">
	import { Plus } from 'lucide-svelte';
	import Button from '../ui/Button.svelte';
	import ChannelItem from './ChannelItem.svelte';
	import SettingsItem from './SettingsItem.svelte';
	import Switch from '../ui/Switch.svelte';
	import { type Channel } from '$lib/server/types';

	let channels: Channel[] = [
		{
			id: '1',
			name: '📜-quotes'
		},
		{
			id: '2',
			name: '💬-general'
		}
	];

	function removeChannel(id: string) {
		//  TODO: Query backend here
		channels = channels.filter((c) => c.id !== id);
	}
</script>

<SettingsItem
	heading={'Allowed channels'}
	description={'Configure in which channels the bot is allowed to operate'}
>
	<div class="flex flex-col gap-4">
		<div class="mx-auto flex items-center gap-2">
			<div class="text-light text-center font-semibold">Lock channels</div>
			<Switch />
		</div>
		<Button onclick={() => console.log('Add channel')}>
			<div class="text-light flex w-full items-center">
				<Plus />
				<div class="flex-1 text-center font-semibold">Add Channel</div>
			</div>
		</Button>

		<div class="flex flex-col gap-2">
			{#if channels.length > 0}
				{#each channels as channel}
					<ChannelItem id={channel.id} name={channel.name} remove={removeChannel} />
				{/each}
			{:else}
				<div
					class="border-highlight text-light rounded-xl border-2 border-dashed p-4 text-center font-semibold"
				>
					No channels
				</div>
			{/if}
		</div>
	</div>
</SettingsItem>
