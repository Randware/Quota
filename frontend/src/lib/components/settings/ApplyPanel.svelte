<script lang="ts">
	import { enhance } from '$app/forms';
	import type { SubmitFunction } from '@sveltejs/kit';
	import ButtonDark from '../ui/ButtonDark.svelte';
	import ButtonPrimary from '../ui/ButtonPrimary.svelte';

	let { handleApply, handleRevert } = $props<{
		handleApply: SubmitFunction;
		handleRevert: () => void;
	}>();
</script>

<div class="bg-dark flex flex-col items-center gap-4 rounded-xl p-4 drop-shadow-lg sm:flex-row">
	<div class="text-light flex-1 font-semibold">Unsaved changes</div>

	<form
		class="flex flex-wrap items-center gap-4"
		method="POST"
		action="?/applySettings"
		use:enhance={handleApply}
	>
		<ButtonDark onclick={handleRevert}>
			<div class="text-light px-4 py-2 font-semibold">Revert</div>
		</ButtonDark>

		<ButtonPrimary type="submit">
			<div class="text-light px-4 py-2 font-semibold">Apply</div>
		</ButtonPrimary>
	</form>
</div>
