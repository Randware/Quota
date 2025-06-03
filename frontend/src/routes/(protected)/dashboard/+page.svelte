<script lang="ts">
	import { page } from '$app/state';
	import GuildItem from '$lib/components/dashboard/GuildItem.svelte';
	import type { Guild } from '$lib/server/types';

	let userGuilds: Guild[] = page.data.userGuilds;
</script>

<div class="h-full w-full overflow-y-auto">
	<div class="flex flex-wrap justify-center gap-4 p-4">
		{#each userGuilds as guild}
			<GuildItem {guild} />
		{/each}
	</div>
</div>
