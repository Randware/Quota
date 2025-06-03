<script lang="ts">
	let { onclick, children } = $props<{ onclick: () => void }>();
</script>

<button
	class=" bg-dark hover:bg-highlight ring-highlight border-ins rounded-xl ring-2 transition-colors duration-300 hover:cursor-pointer"
	{onclick}
>
	{@render children()}
</button>
