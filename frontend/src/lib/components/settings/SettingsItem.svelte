<script lang="ts">
	let { heading, children } = $props<{
		heading: string;
	}>();
</script>

<div class="bg-dark flex flex-col rounded-xl p-4">
	<div class="text-light relative flex justify-center pb-4">
		<div class="text-xl font-semibold">
			{heading}
		</div>
	</div>

	{@render children()}
</div>
