<script lang="ts">
	import { Settings, type Icon, ChartNoAxesColumn, LayoutGrid } from 'lucide-svelte';
	import CubeContainer from '$lib/components/ui/CubeContainer.svelte';
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

<aside class="bg-dark flex h-full flex-col">
	{#each pages as page}
		<CubeContainer>
			<SidebarItem name={page.name} icon={page.icon} path={page.path} />
		</CubeContainer>
	{/each}
</aside>
