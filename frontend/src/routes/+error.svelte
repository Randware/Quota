<script lang="ts">
	import { page } from '$app/state';
</script>

<div class="text-light flex h-full items-center justify-center">
	<div class="flex flex-col items-center">
		<div class="mb-4 text-5xl font-bold">
			{page.status}
		</div>

		{#if page.error}
			<div class="text-3xl font-semibold">
				{page.error.message}
			</div>
		{/if}
	</div>
</div>
