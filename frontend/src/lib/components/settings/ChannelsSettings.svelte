<script lang="ts">
	import SettingsItem from './SettingsItem.svelte';
	import Switch from '../ui/Switch.svelte';
	import SettingsItemSection from './SettingsItemSection.svelte';
	import type { Channel, Settings } from '$lib/server/types';
	import DeleteChannelMenu from './DeleteChannelMenu.svelte';
	import AddChannelMenu from './AddChannelMenu.svelte';
	import { fly } from 'svelte/transition';

	let allChannels: Channel[] = [
		{ id: 'testid', name: 'john-channel' },
		{ id: '123123412451224', name: 'am-a-channel' }
	];

	let addChannels: Channel[] = $derived(
		allChannels.filter(
			(c: Channel) => !settings.allowedChannels.find((a: Channel) => a.id === c.id)
		)
	);

	let { settings = $bindable() }: { settings: Settings } = $props<{ settings: Settings }>();

	function removeChannel(channel: Channel) {
		settings.allowedChannels = settings.allowedChannels.filter((c: Channel) => c.id !== channel.id);
	}

	function addChannel(channel: Channel) {
		settings.allowedChannels = [...settings.allowedChannels, channel];
	}

	let displayRemove: boolean = $state(true);
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
			{#if displayRemove}
				<div class="" in:fly={{ x: -10, duration: 500 }}>
					<DeleteChannelMenu
						bind:channels={settings.allowedChannels}
						{removeChannel}
						switchView={() => (displayRemove = false)}
					/>
				</div>
			{:else}
				<div class="" in:fly={{ x: 10, duration: 500 }}>
					<AddChannelMenu
						bind:channels={addChannels}
						{addChannel}
						switchView={() => (displayRemove = true)}
					/>
				</div>
			{/if}
		</SettingsItemSection>
	</div>
</SettingsItem>
