<script lang="ts">
	let { heading, children } = $props<{
		heading: string;
	}>();
</script>

<div class="flex flex-col">
	<div class="flex flex-col gap-2">
		<div class="text-light flex-1 px-1 text-lg font-semibold">{heading}</div>
		<hr class="border-highlight rounded-full border-1" />
	</div>

	<div class="px-2 py-4">
		{@render children()}
	</div>
</div>
