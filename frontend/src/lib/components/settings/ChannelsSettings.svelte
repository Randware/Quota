<script lang="ts">
	import { Plus } from 'lucide-svelte';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';
	import ChannelItem from './ChannelItem.svelte';
	import SettingsItem from './SettingsItem.svelte';
	import Switch from '../ui/Switch.svelte';
	import SettingsItemSection from './SettingsItemSection.svelte';
	import type { Channel, Settings } from '$lib/server/types';

	let { settings = $bindable() } = $props<{ settings: Settings }>();

	async function removeChannel(id: string) {
		//  TODO: Query backend here
		settings.allowedChannels = settings.allowedChannels.filter((c: Channel) => c.id !== id);
	}
</script>

<SettingsItem heading={'Channels'}>
	<div class="flex flex-col gap-4 px-2">
		<SettingsItemSection heading={'Channel settings'}>
			<div class="flex items-center gap-4">
				<div class="text-light flex-1 font-semibold">Lock channels</div>

				<Switch bind:toggled={settings.lockAllowedChannels} />
			</div>
		</SettingsItemSection>

		<SettingsItemSection heading={'Channels'}>
			<div class="flex flex-col gap-4">
				<ButtonPrimary onclick={() => console.log('Add channel')}>
					<div class="text-light flex w-full items-center px-4 py-2">
						<Plus />
						<div class="flex-1 text-center font-semibold">Add channel</div>
					</div>
				</ButtonPrimary>

				<div class="flex flex-col gap-2">
					{#if settings.allowedChannels.length > 0}
						{#each settings.allowedChannels as channel}
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
		</SettingsItemSection>
	</div>
</SettingsItem>
