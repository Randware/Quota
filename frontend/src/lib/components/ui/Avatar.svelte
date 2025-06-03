<script lang="ts">
	let { image, text } = $props<{ image: string; text: string }>();

	let displayFallback = $state(false);
</script>

<div class="border-primary aspect-square h-full rounded-full border-3">
	<div class="flex h-full w-full items-center justify-center border-3 border-transparent">
		<img src={image} alt="" onerror={() => (displayFallback = true)} class="rounded-full" />

		{#if displayFallback}
			<div class="text-light text-md font-semibold uppercase sm:text-lg">
				{text.charAt(0)}
			</div>
		{/if}
	</div>
</div>
