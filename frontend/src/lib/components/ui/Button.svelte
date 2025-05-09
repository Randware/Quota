<script lang="ts">
	import { Button } from 'bits-ui';

	let { onclick, children } = $props<{ onclick: () => void }>();
</script>

<Button.Root
	class="bg-primary hover:bg-primary/90 flex items-center justify-center rounded-xl px-4 py-2 transition-colors duration-300"
	{onclick}
>
	{@render children()}
</Button.Root>
