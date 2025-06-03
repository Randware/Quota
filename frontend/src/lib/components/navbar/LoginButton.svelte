<script lang="ts">
	import ButtonPrimary from '$lib/components/ui/ButtonPrimary.svelte';

	function runAuthFlow() {
		window.location.href = '/auth/login';
	}
</script>

<ButtonPrimary onclick={runAuthFlow}>
	<div class="text-light text-md px-4 py-2 font-bold sm:text-lg">Log in with Discord</div>
</ButtonPrimary>
