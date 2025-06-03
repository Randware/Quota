<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import type { Guild, Stats } from '$lib/server/types';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';

	let guild: Guild = $derived(page.data.guild);
	let stats: Stats = $derived(page.data.stats);
</script>

<div class="bg-dark flex flex-col justify-between gap-6 rounded-xl p-4">
	<div class="text-light text-2xl font-semibold">
		<span class="text-primary">Total</span> quotes
	</div>

	<div class="text-light text-4xl font-semibold">{stats.totalQuotes}</div>

	<ButtonPrimary onclick={() => goto(`/dashboard/${guild.id}/stats`)}>
		<div class="text-light px-4 py-2 font-semibold">Details</div>
	</ButtonPrimary>
</div>
