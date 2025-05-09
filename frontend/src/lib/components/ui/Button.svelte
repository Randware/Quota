<script lang="ts">
	let { onclick, children } = $props<{ onclick: () => void }>();
</script>

<button
	class="bg-primary hover:bg-primary/90 flex items-center justify-center rounded-xl px-4 py-2 transition-colors duration-300"
	{onclick}
>
	{@render children()}
</button>
