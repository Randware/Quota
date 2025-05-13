<script lang="ts">
	let { heading, children } = $props<{
		heading: string;
		description: string | null;
	}>();
</script>

<div class="bg-dark flex flex-col rounded-xl p-4">
	<div class="text-light flex items-center">
		<div class="flex-1 pb-4 text-center text-lg font-semibold">
			{heading}
		</div>
	</div>

	{@render children()}
</div>
