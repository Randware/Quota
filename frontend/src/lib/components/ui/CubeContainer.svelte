<script lang="ts">
	let { children } = $props();
</script>

<div
	class="flex max-h-18 min-h-18 max-w-18 min-w-18 items-center justify-center sm:max-h-20 sm:min-h-20 sm:max-w-20 sm:min-w-20"
>
	{@render children()}
</div>
