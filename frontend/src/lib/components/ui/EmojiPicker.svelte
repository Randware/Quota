<script lang="ts">
	//  TODO: Make responsive
	//        (maybe) Adjust border-radius

	import { Picker } from 'emoji-mart';
	import data from '@emoji-mart/data';
	import { onMount } from 'svelte';
	import type { CustomEmojiCollection, ImageEmoji, TextEmoji } from '$lib/server/types';
	let { onSelect, custom = [] } = $props<{
		onSelect: (emoji: TextEmoji | ImageEmoji) => void;
		custom: CustomEmojiCollection[];
	}>();

	let container: HTMLDivElement;
	let primaryColor: string = $state('');
	let darkColor: string = $state('');
	let font: string = $state('');

	function convertColor(cssColor: string): string {
		const match = cssColor.match(/rgba?\(\s*([^)]+)\s*\)/i);

		if (!match) {
			throw new Error(`invalid color: "${cssColor}"`);
		}

		return match[1]
			.split(',')
			.map((s) => s.trim())
			.join(',');
	}

	onMount(() => {
		primaryColor = convertColor(getComputedStyle(container).getPropertyValue('--color-primary'));
		darkColor = convertColor(getComputedStyle(container).getPropertyValue('--color-dark'));
		font = getComputedStyle(container).getPropertyValue('--font-emoji');

		const mapped = custom.map((c: CustomEmojiCollection) => ({
			id: c.id,
			name: c.name,
			emojis: c.emojis.map((e: ImageEmoji) => {
				return { id: e.id, name: e.name, keywords: [e.name], skins: [{ src: e.src }] };
			})
		}));

		new Picker({
			parent: container,
			data,
			custom: mapped,
			autoFocus: true,
			onEmojiSelect: (emoji: any) => {
				if (emoji.src) {
					onSelect({ type: 'image', id: emoji.id, name: emoji.name, src: emoji.src });
				} else {
					onSelect({ type: 'text', id: emoji.id, name: emoji.name, native: emoji.native });
				}
			}
		});
	});
</script>

<div
	bind:this={container}
	style="
    --rgb-accent: {primaryColor};
    --rgb-background: {darkColor};
    --rgb-input: {darkColor};
    --font-family: {font};
  "
></div>
