<script lang="ts">
	import Navbar from '$lib/components/navbar/Navbar.svelte';
	import '../app.css';
	import { page } from '$app/state';

	let { children } = $props();

	let user: { name: string; picture: string } | null = $derived(page.data.user);
</script>

<div class="bg-darker flex h-screen w-screen flex-col">
	<Navbar {user} />

	{@render children()}
</div>
