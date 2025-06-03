<script lang="ts">
	import Sidebar from '$lib/components/sidebar/Sidebar.svelte';
	import { page } from '$app/state';
	import type { Guild } from '$lib/server/types';

	let { children } = $props();

	let guild: Guild = $derived(page.data.guild);
</script>

<div class="bg-dark flex flex-1 overflow-hidden">
	<Sidebar />
	<div class="bg-darker h-full w-full overflow-y-auto rounded-tl-xl">
		{@render children()}
	</div>
</div>
