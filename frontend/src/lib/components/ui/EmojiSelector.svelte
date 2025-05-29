<script lang="ts">
	import type { CustomEmojiCollection, ImageEmoji, TextEmoji } from '$lib/server/types';
	import { fly } from 'svelte/transition';
	import EmojiPicker from './EmojiPicker.svelte';
	import { emojify } from '$lib/actions/emojify';
	import { createFloatingActions } from 'svelte-floating-ui';
	import { offset, flip, shift } from 'svelte-floating-ui/dom';

	let { defaultEmoji, custom = [] } = $props<{
		defaultEmoji: TextEmoji | ImageEmoji;
		custom: CustomEmojiCollection[];
	}>();

	let selectedEmoji: TextEmoji | ImageEmoji = $state(defaultEmoji);
	let showSelector: boolean = $state(false);

	const [referenceAction, floatingAction] = createFloatingActions({
		placement: 'bottom',
		middleware: [offset(5), flip(), shift({ padding: 8 })],
		autoUpdate: true
	});

	function onSelect(emoji: TextEmoji | ImageEmoji) {
		selectedEmoji = emoji;
		showSelector = false;
	}
</script>

{#if showSelector}
	<div class="fixed inset-0 z-40 h-screen w-screen" onclick={() => (showSelector = false)}></div>
{/if}

<button
	use:referenceAction
	class="bg-dark border-highlight hover:bg-highlight flex aspect-square w-12 items-center justify-center rounded-xl border-2 p-2 transition-colors duration-300 {showSelector
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
	<div
		use:floatingAction
		class="z-50 rounded-lg shadow-lg"
		transition:fly={{ y: -10, duration: 300 }}
	>
		<EmojiPicker {onSelect} {custom} />
	</div>
{/if}
