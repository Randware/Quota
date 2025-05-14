<script lang="ts">
	import { Trash2 } from 'lucide-svelte';

	let { id, name, remove } = $props<{ id: string; name: string; remove: (id: string) => void }>();
</script>

<div class="bg-darker text-light flex items-center rounded-xl p-3">
	<div class="text-md flex-1 truncate text-center font-semibold">#{name}</div>
	<button
		onclick={() => remove(id)}
		class="hover:bg-highlight rounded-xl p-2 transition-colors duration-300"
	>
		<Trash2 />
	</button>
</div>
