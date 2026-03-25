<script lang="ts">
	import type { CustomEmojiCollection, ImageEmoji, TextEmoji } from '$lib/server/types';
	import { fly } from 'svelte/transition';
	import EmojiPicker from './EmojiPicker.svelte';
	import { emojify } from '$lib/actions/emojify';
	import { createFloatingActions } from 'svelte-floating-ui';
	import { offset, flip, shift } from 'svelte-floating-ui/dom';

	let {
		defaultEmoji,
		custom = [],
		selectedEmoji = $bindable()
	} = $props<{
		defaultEmoji?: TextEmoji | ImageEmoji;
		custom?: CustomEmojiCollection[];
		selectedEmoji?: TextEmoji | ImageEmoji;
	}>();

	// Initialize selectedEmoji with defaultEmoji if it's not provided
	$effect(() => {
		if (selectedEmoji === undefined && defaultEmoji !== undefined) {
			selectedEmoji = defaultEmoji;
		}
	});

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

<!--  TODO: Use ButtonDark component here -->
<button
	use:referenceAction
	class="bg-dark border-highlight hover:bg-highlight flex aspect-square w-12 items-center justify-center rounded-xl border-2 p-2 transition-colors duration-300 {showSelector
		? 'bg-highlight'
		: ''}"
	onclick={() => (showSelector = !showSelector)}
>
	<div class="h-full w-full">
		{#if selectedEmoji}
			{#if 'native' in selectedEmoji}
				{#key selectedEmoji}
					<div use:emojify={{}}>
						{selectedEmoji.native}
					</div>
				{/key}
			{:else if 'src' in selectedEmoji}
				<img src={selectedEmoji.src} alt="" />
			{/if}
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
