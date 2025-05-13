<script lang="ts">
	import { Plus } from 'lucide-svelte';
	import Button from '../ui/Button.svelte';
	import ChannelItem, { type Channel } from './ChannelItem.svelte';
	import SettingsItem from './SettingsItem.svelte';

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
					class="border-light text-light rounded-xl border-2 border-dashed p-4 text-center font-semibold"
				>
					No channels
				</div>
			{/if}
		</div>
	</div>
</SettingsItem>
