<script lang="ts">
	import Sidebar from '$lib/components/sidebar/Sidebar.svelte';

	let { children } = $props();
</script>

<div class="bg-dark flex flex-1 overflow-hidden">
	<Sidebar />
	<div class="bg-darker h-full w-full overflow-y-auto rounded-tl-xl">
		{@render children()}
	</div>
</div>
