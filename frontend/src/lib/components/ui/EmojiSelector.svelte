<script lang="ts">
	import type { CustomEmojiCollection, ImageEmoji, TextEmoji } from '$lib/server/types';
	import { fly } from 'svelte/transition';
	import EmojiPicker from './EmojiPicker.svelte';
	import { emojify } from '$lib/actions/emojify';

	let { defaultEmoji, custom = [] } = $props<{
		defaultEmoji: TextEmoji | ImageEmoji;
		custom: CustomEmojiCollection[];
	}>();

	let container: HTMLDivElement;

	let selectedEmoji: TextEmoji | ImageEmoji = $state(defaultEmoji);

	let showSelector: boolean = $state(false);

	function onSelect(emoji: TextEmoji | ImageEmoji) {
		selectedEmoji = emoji;
	}
</script>

{#if showSelector}
	<div class="fixed inset-0 z-40 h-screen w-screen" onclick={() => (showSelector = false)}></div>
{/if}

<div class="relative" bind:this={container}>
	<button
		class=" bg-dark border-highlight hover:bg-highlight z-50 flex aspect-square w-12 items-center justify-center rounded-xl border-2 p-2 transition-colors duration-300 {showSelector
			? 'bg-highlight'
			: ''}"
		onclick={() => (showSelector = !showSelector)}
	>
		<div class="h-full w-full">
			{#if 'native' in selectedEmoji}
				{#key selectedEmoji}
					<div use:emojify={{}}>
						{selectedEmoji.native}
					</div>
				{/key}
			{:else}
				<img src={selectedEmoji.src} alt="" />
			{/if}
		</div>
	</button>

	{#if showSelector}
		<div class="absolute top-14 z-50" transition:fly={{ y: -10, duration: 300 }}>
			<EmojiPicker {onSelect} {custom} />
		</div>
	{/if}
</div>
