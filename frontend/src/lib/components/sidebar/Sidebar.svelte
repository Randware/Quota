<script lang="ts">
	import { Settings, type Icon, ChartNoAxesColumn, LayoutGrid } from 'lucide-svelte';
	import SidebarItem from './SidebarItem.svelte';

	interface Page {
		name: string;
		icon: typeof Icon;
		path: string;
	}

	const pages: Page[] = [
		{ name: 'Overview', icon: LayoutGrid, path: '/dashboard/overview' },
		{ name: 'Settings', icon: Settings, path: '/dashboard/settings' },
		{ name: 'Stats', icon: ChartNoAxesColumn, path: '/dashboard/stats' }
	];
</script>

<aside class="bg-dark flex h-full w-18 flex-col gap-2 p-2 sm:w-20">
	{#each pages as page}
		<SidebarItem name={page.name} icon={page.icon} path={page.path} />
	{/each}
</aside>
