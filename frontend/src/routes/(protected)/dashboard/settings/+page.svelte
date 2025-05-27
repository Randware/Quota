<script lang="ts">
	import AllowedChannels from '$lib/components/settings/AllowedChannels.svelte';
	import img from '$lib/assets/logo.png';
	import EmojiSelector from '$lib/components/ui/EmojiSelector.svelte';
</script>

<div class="p-8">
	<AllowedChannels />
	<EmojiSelector
		defaultEmoji={{
			id: 'test_id',
			name: 'Test Name',
			src: img
		}}
		custom={[
			{
				id: 'discord',
				name: 'Discord',
				emojis: [{ id: 'test_id', name: 'Test Name', src: img }]
			}
		]}
	/>
</div>
