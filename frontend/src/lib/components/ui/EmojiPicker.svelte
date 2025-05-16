<script lang="ts">
	//  TODO: Make responsive
	//        (maybe) Adjust border-radius
	//        Create typing for onSelect and custom emojis

	import { Picker } from 'emoji-mart';
	import data from '@emoji-mart/data';
	import { onMount } from 'svelte';

	let { onSelect, custom = [] } = $props<{
		onSelect: (id: string, name: string, native: string) => void;
		custom: [
			{ id: string; name: string; emojis: [{ id: string; name: string; skins: [{ src: string }] }] }
		];
	}>();

	let container: HTMLDivElement;
	let primaryColor: string = $state('');
	let darkColor: string = $state('');

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

		new Picker({
			parent: container,
			data,
			custom,
			onEmojiSelect: onSelect,
			skinTonePosition: 'none'
		});
	});
</script>

<div
	bind:this={container}
	class="
    [--rgb-accent:{primaryColor}]
    [--rgb-background:{darkColor}]
    [--rgb-input:var(--color-dark)]
  "
></div>

<style>
</style>
