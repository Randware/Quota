<script lang="ts">
	import { page } from '$app/state';
	import type { Guild } from '$lib/server/types';
	import Avatar from '../ui/Avatar.svelte';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';
	import { goto } from '$app/navigation';

	let guild: Guild = $derived(page.data.guild);
</script>

<div class="bg-dark flex flex-col justify-between gap-6 rounded-xl p-4">
	<div class="text-light text-2xl font-semibold">
		<span class="text-primary">Selected</span> guild
	</div>

	<div class=" flex h-16 items-center gap-4">
		<Avatar image={guild.icon} text={guild.name} />

		<div class="text-light text-xl font-semibold">{guild.name}</div>
	</div>

	<ButtonPrimary onclick={() => goto('/dashboard')}>
		<div class="text-light px-4 py-2 font-semibold">Switch</div>
	</ButtonPrimary>
</div>
