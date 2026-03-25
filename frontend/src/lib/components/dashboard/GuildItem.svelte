<script lang="ts">
	import type { Guild } from '$lib/server/types';
	import Avatar from '../ui/Avatar.svelte';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';
	import ButtonDark from '../ui/ButtonDark.svelte';
	import { goto } from '$app/navigation';

	let { guild }: { guild: Guild } = $props<{ guild: Guild }>();

	let displayBackground = $state(true);
	let isNavigating = $state(false);

	// Discord CDN icon URL or fallback
	let iconUrl = $derived(guild.icon ? guild.icon : '');

	async function handleManage() {
		isNavigating = true;
		await goto(`/dashboard/${guild.id}`);
		isNavigating = false;
	}

	async function handleAdd() {
		isNavigating = true;
		await goto(`/auth/invite?guild_id=${guild.id}`);
		isNavigating = false;
	}
</script>

<div
	class="group w-full max-w-80 overflow-hidden rounded-xl transition-all duration-300 {guild.bot
		? 'hover:ring-primary/30 hover:ring-2'
		: 'opacity-70 hover:opacity-100'}"
>
	<div class="bg-highlight relative aspect-video overflow-hidden">
		{#if displayBackground && iconUrl}
			<img
				src={iconUrl}
				loading="lazy"
				alt=""
				class="absolute inset-0 h-full w-full scale-110 object-cover blur-xl brightness-50"
				onerror={() => (displayBackground = false)}
			/>
		{/if}

		<div class="relative z-10 flex h-full w-full items-center justify-center">
			<div class="h-full max-h-16">
				<Avatar image={iconUrl} text={guild.name} />
			</div>
		</div>

		{#if !guild.bot}
			<div
				class="bg-dark/60 text-light/40 absolute top-2 right-2 z-20 rounded-md px-2 py-0.5 text-xs font-medium backdrop-blur-sm"
			>
				Bot not added
			</div>
		{/if}
	</div>

	<div class="bg-dark flex items-center justify-between gap-4 p-4">
		<div class="text-md text-light flex-grow truncate font-semibold">
			{guild.name}
		</div>

		{#if guild.bot}
			<ButtonDark onclick={handleManage} disabled={isNavigating}>
				<div class="text-light flex items-center gap-2 px-4 py-2 font-semibold">
					{#if isNavigating}
						<svg class="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
							<circle
								class="opacity-25"
								cx="12"
								cy="12"
								r="10"
								stroke="currentColor"
								stroke-width="4"
							></circle>
							<path
								class="opacity-75"
								fill="currentColor"
								d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
							></path>
						</svg>
						Loading…
					{:else}
						Manage
					{/if}
				</div>
			</ButtonDark>
		{:else}
			<ButtonPrimary onclick={handleAdd} disabled={isNavigating}>
				<div class="text-light flex items-center gap-2 px-4 py-2 font-semibold">
					{#if isNavigating}
						<svg class="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
							<circle
								class="opacity-25"
								cx="12"
								cy="12"
								r="10"
								stroke="currentColor"
								stroke-width="4"
							></circle>
							<path
								class="opacity-75"
								fill="currentColor"
								d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
							></path>
						</svg>
						Loading…
					{:else}
						Add
					{/if}
				</div>
			</ButtonPrimary>
		{/if}
	</div>
</div>
