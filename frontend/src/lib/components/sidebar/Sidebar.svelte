<script lang="ts">
	import { Settings, type Icon, ChartNoAxesColumn, LayoutGrid } from 'lucide-svelte';
	import SidebarItem from './SidebarItem.svelte';
	import { page } from '$app/state';
	import type { Guild } from '$lib/server/types';

	interface Page {
		name: string;
		icon: typeof Icon;
		path: string;
	}

	let guild: Guild = $derived(page.data.guild);

	const pages: Page[] = [
		{ name: 'Overview', icon: LayoutGrid, path: `/dashboard/${guild.id}/overview` },
		{ name: 'Settings', icon: Settings, path: `/dashboard/${guild.id}/settings` },
		{ name: 'Stats', icon: ChartNoAxesColumn, path: `/dashboard/${guild.id}/stats` }
	];
</script>

<aside class="bg-dark flex h-full max-w-18 min-w-18 flex-col gap-2 p-2 sm:max-w-20 sm:min-w-20">
	{#each pages as page}
		<SidebarItem name={page.name} icon={page.icon} path={page.path} />
	{/each}
</aside>
