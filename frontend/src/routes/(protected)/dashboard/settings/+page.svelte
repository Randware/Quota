<script lang="ts">
	import AllowedChannels from '$lib/components/settings/AllowedChannels.svelte';
	import EmojiPicker from '$lib/components/ui/EmojiPicker.svelte';
	import img from '$lib/assets/logo.png';
	import type { ImageEmoji, TextEmoji } from '$lib/server/types';

	function handleSelect(emoji: TextEmoji | ImageEmoji) {
		if ('native' in emoji) {
			console.log(emoji.native);
		} else {
			console.log(emoji.src);
		}
	}
</script>

<div class="p-8">
	<AllowedChannels />
	<EmojiPicker
		onSelect={handleSelect}
		custom={[
			{
				id: 'discord',
				name: 'Discord',
				emojis: [{ type: 'image', id: 'test_id', name: 'Test Name', src: img }]
			}
		]}
	/>
</div>
