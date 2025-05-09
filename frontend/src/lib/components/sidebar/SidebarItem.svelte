<script lang="ts">
	import { page } from '$app/state';
	import type { Icon } from 'lucide-svelte';
	import { scale } from 'svelte/transition';

	let { name, icon, path } = $props<{
		name: string;
		icon: typeof Icon;
		path: string;
	}>();

	let active: boolean = $derived(page.url.pathname === path);
</script>

<div class="relative m-2 inline-flex items-center justify-center">
	<a
		href={path}
		aria-label="Go to {name.toLowerCase()} dashboard page"
		class="text-light rounded-xl
           {active ? 'bg-darker' : ''}
           hover:bg-darker p-4 transition-colors duration-300"
	>
		{#if true}
			{@const IconComp = icon}
			<IconComp size="100%" />
		{/if}
	</a>

	{#if active}
		<div
			in:scale={{ duration: 300 }}
			class="bg-primary absolute right-0 h-[50%] w-1.5 translate-x-1/2 rounded-full"
		></div>
	{/if}
</div>
