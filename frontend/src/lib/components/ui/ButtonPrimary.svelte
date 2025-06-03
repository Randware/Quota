<script lang="ts">
	let { onclick, children } = $props<{ onclick: () => void }>();
</script>

<button
	class="bg-primary hover:bg-primary/80 flex items-center justify-center rounded-xl transition-colors duration-300 hover:cursor-pointer"
	{onclick}
>
	{@render children()}
</button>
