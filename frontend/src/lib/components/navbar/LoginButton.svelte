<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';

	function runAuthFlow() {
		window.location.href = '/auth/login';
	}
</script>

<Button onclick={runAuthFlow}>
	<div class="text-light text-md font-bold sm:text-lg">Log in with Discord</div>
</Button>
