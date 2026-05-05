<script lang="ts">
	import { page } from '$app/state';
	import { invalidateAll } from '$app/navigation';
	import { enhance } from '$app/forms';
	import type { Guild } from '$lib/server/types';
	import Avatar from '../ui/Avatar.svelte';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';
	import ButtonDark from '../ui/ButtonDark.svelte';
	import { goto } from '$app/navigation';
	import type { SubmitFunction } from '@sveltejs/kit';

	let guild: Guild = $derived(page.data.guild);
	let errorMessage = $state('');
	let isRemoving = $state(false);

	const handleRemove: SubmitFunction = () => {
		isRemoving = true;
		return async ({ result }) => {
			isRemoving = false;
			if (result.type === 'success' && result.data?.success) {
				errorMessage = '';
				await invalidateAll();
				return;
			}
			const error = result.data?.error ?? 'Failed to remove bot from server.';
			errorMessage = error;
			await invalidateAll();
		};
	};
</script>

<div class="bg-dark flex flex-col justify-between gap-6 rounded-xl p-4">
	<div class="text-light text-2xl font-semibold">
		<span class="text-primary">Selected</span> guild
	</div>

	<div class=" flex h-16 items-center gap-4">
		<Avatar image={guild.icon} text={guild.name} />

		<div class="text-light text-xl font-semibold">{guild.name}</div>
	</div>

	{#if errorMessage}
		<div class="rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-400">
			{errorMessage}
		</div>
	{/if}

	<div class="flex flex-wrap items-center gap-3">
		<ButtonPrimary onclick={() => goto('/dashboard')}>
			<div class="text-light px-4 py-2 font-semibold">Switch</div>
		</ButtonPrimary>

		<form method="POST" action="?/removeBot" use:enhance={handleRemove}>
			<ButtonDark onclick={() => undefined} disabled={isRemoving}>
				<div class="text-light px-4 py-2 font-semibold">
					{isRemoving ? 'Removing…' : 'Remove Quota'}
				</div>
			</ButtonDark>
		</form>
	</div>
</div>
