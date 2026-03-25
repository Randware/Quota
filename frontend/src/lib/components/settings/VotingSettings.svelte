<script lang="ts">
	import type { Settings } from '$lib/server/types';
	import EmojiSelector from '../ui/EmojiSelector.svelte';
	import Switch from '../ui/Switch.svelte';
	import SettingsItem from './SettingsItem.svelte';
	import SettingsItemSection from './SettingsItemSection.svelte';

	let { settings = $bindable() } = $props<{ settings: Settings }>();
</script>

<SettingsItem heading={'Voting'}>
	<div class="flex flex-col gap-4 px-2">
		<SettingsItemSection heading={'Voting settings'}>
			<div class="flex items-center">
				<div class="text-light flex-1 font-semibold">Enable voting</div>

				<Switch bind:toggled={settings.allowVoting} />
			</div>
		</SettingsItemSection>

		<SettingsItemSection heading={'Emojis'}>
			<div class="flex flex-col gap-2">
				<div class="flex items-center">
					<div class="text-light flex-1 font-semibold">Upvote Emoji</div>

					<EmojiSelector
						defaultEmoji={settings.upvoteEmoji}
						bind:selectedEmoji={settings.upvoteEmoji}
					/>
				</div>

				<div class="flex items-center">
					<div class="text-light flex-1 font-semibold">Downvote Emoji</div>

					<EmojiSelector
						defaultEmoji={settings.downvoteEmoji}
						bind:selectedEmoji={settings.downvoteEmoji}
					/>
				</div>
			</div>
		</SettingsItemSection>
	</div>
</SettingsItem>
