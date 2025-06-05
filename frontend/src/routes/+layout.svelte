<script lang="ts">
	import Navbar from '$lib/components/navbar/Navbar.svelte';
	import '../app.css';
	import { page } from '$app/state';
	import type { Session } from '$lib/server/types';

	let { children } = $props();

	let session: Session | undefined = $derived(page.data.session);
</script>

<div class="bg-darker flex h-screen w-screen flex-col">
	<Navbar {session} />

	{@render children()}
</div>
