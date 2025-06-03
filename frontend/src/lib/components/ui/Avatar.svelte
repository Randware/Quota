<script lang="ts">
	let { image, text } = $props<{ image: string; text: string }>();
</script>

<div class="border-primary aspect-square h-full rounded-full border-3">
	<div class="flex h-full w-full items-center justify-center border-3 border-transparent">
		<img src={image} alt={text.charAt(0)} class="text-light rounded-full font-semibold uppercase" />
	</div>
</div>
