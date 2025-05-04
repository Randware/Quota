<script lang="ts">
	import Sidebar from '$lib/components/Sidebar.svelte';

	let { children } = $props();
</script>

<div class="flex flex-1">
	<Sidebar />
	{@render children()}
</div>
