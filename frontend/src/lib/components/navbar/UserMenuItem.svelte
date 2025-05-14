<script lang="ts">
	import { Icon } from 'lucide-svelte';

	let { icon, text, href } = $props<{ icon: typeof Icon; text: string; href: string }>();
</script>

<a
	{href}
	class="hover:bg-highlight text-light flex items-center justify-center gap-2 rounded-xl px-4 py-2 transition-colors duration-300"
>
	{#if true}
		{@const Icon = icon}
		<Icon />
	{/if}
	<div class="text-md mx-auto px-2 font-medium sm:text-lg">{text}</div>
</a>
