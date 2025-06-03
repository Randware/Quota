<script lang="ts">
	import { Icon } from 'lucide-svelte';
	import ButtonDark from '../ui/ButtonDark.svelte';
	import { goto } from '$app/navigation';

	let { icon, text, href } = $props<{ icon: typeof Icon; text: string; href: string }>();
</script>

<ButtonDark onclick={() => goto(href)}>
	<div class="text-light flex items-center justify-center gap-2 px-4 py-2">
		{#if true}
			{@const Icon = icon}
			<Icon />
		{/if}
		<div class="text-md mx-auto px-2 font-medium sm:text-lg">{text}</div>
	</div>
</ButtonDark>
