<script lang="ts">
	let { width = '100%', height = '100%' } = $props<{
		width: string | number;
		height: string | number;
	}>();

	// Convert numeric values to pixel strings
	const widthStyle = typeof width === 'number' ? `${width}px` : width;
	const heightStyle = typeof height === 'number' ? `${height}px` : height;
</script>

<div
	class="text-highlight bg-light/20 flex animate-pulse items-center justify-center rounded-xl"
	style="width: {widthStyle}; height: {heightStyle};"
></div>
