<script lang="ts">
	import SkeletonCircle from './SkeletonCircle.svelte';

	let { image, text } = $props<{ image: string; text: string }>();

	let loading = $state(true);
</script>

<div class="border-primary aspect-square h-full overflow-hidden rounded-full border-3">
	<div class="relative flex h-full w-full items-center justify-center border-3 border-transparent">
		{#if loading}
			<div class="absolute h-full w-full">
				<SkeletonCircle />
			</div>
		{/if}

		<img
			src={image}
			loading="lazy"
			alt={text.charAt(0)}
			onload={() => (loading = false)}
			onerror={() => (loading = false)}
			class="text-light absolute rounded-full font-semibold uppercase"
		/>
	</div>
</div>
