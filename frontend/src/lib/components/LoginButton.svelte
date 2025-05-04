<script lang="ts">
	function runAuthFlow() {
		window.location.href = '/auth/login';
	}
</script>

<button
	class="bg-primary hover:bg-primary/80 flex items-center justify-center rounded-xl px-4 py-2 transition-colors duration-300"
	onclick={runAuthFlow}
>
	<div class="text-light text-lg font-bold">Log in with Discord</div>
</button>
