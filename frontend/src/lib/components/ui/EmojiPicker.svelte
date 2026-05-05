<script lang="ts">
	//  TODO: Make responsive
	//        (maybe) Adjust border-radius

	import { Picker } from 'emoji-mart';
	import data from '@emoji-mart/data/sets/15/twitter.json';
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
		cssColor = cssColor.trim();

		// Already rgb/rgba — extract the inner values
		const rgbMatch = cssColor.match(/rgba?\(\s*([^)]+)\s*\)/i);
		if (rgbMatch) {
			return rgbMatch[1]
				.split(',')
				.map((s) => s.trim())
				.join(',');
		}

		// Hex color (#rrggbb or #rgb) — convert to r,g,b
		const hexMatch = cssColor.match(/^#([0-9a-f]{3,8})$/i);
		if (hexMatch) {
			let hex = hexMatch[1];
			if (hex.length === 3) hex = hex.split('').map((c) => c + c).join('');
			const r = parseInt(hex.slice(0, 2), 16);
			const g = parseInt(hex.slice(2, 4), 16);
			const b = parseInt(hex.slice(4, 6), 16);
			return `${r},${g},${b}`;
		}

		throw new Error(`invalid color: "${cssColor}"`);
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
			set: 'twitter',
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
