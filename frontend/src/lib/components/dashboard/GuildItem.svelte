<script lang="ts">
	import type { Guild } from '$lib/server/types';
	import Avatar from '../ui/Avatar.svelte';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';
	import ButtonDark from '../ui/ButtonDark.svelte';
	import { goto } from '$app/navigation';

	let { guild }: { guild: Guild } = $props<{ guild: Guild }>();

	let displayBackground = $state(true);
</script>

<div class="w-full max-w-80 overflow-hidden rounded-xl">
	<div class="bg-highlight relative aspect-video overflow-hidden">
		{#if displayBackground}
			<img
				src={guild.icon}
				alt=""
				class="absolute inset-0 h-full w-full object-cover blur-md brightness-75"
				onerror={() => (displayBackground = false)}
			/>
		{/if}

		<div class="relative z-10 flex h-full w-full items-center justify-center">
			<div class="h-full max-h-16">
				<Avatar image={guild.icon} text={guild.name} />
			</div>
		</div>
	</div>

	<div class="bg-dark flex items-center justify-between gap-4 p-4">
		<div class="text-md text-light flex-grow truncate font-semibold">
			{guild.name}
		</div>

		{#if guild.bot}
			<ButtonDark onclick={() => goto(`/dashboard/${guild.id}`)}>
				<div class="text-light px-4 py-2 font-semibold">Go</div>
			</ButtonDark>
		{:else}
			<ButtonPrimary onclick={() => goto('/auth/invite')}>
				<div class="text-light px-4 py-2 font-semibold">Add</div>
			</ButtonPrimary>
		{/if}
	</div>
</div>
