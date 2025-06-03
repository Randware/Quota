<script lang="ts">
	import { Trash2 } from 'lucide-svelte';
	import ButtonDark from '../ui/ButtonDark.svelte';

	let { id, name, remove } = $props<{ id: string; name: string; remove: (id: string) => void }>();
</script>

<div class="bg-darker text-light flex items-center rounded-xl p-3">
	<div class="text-md flex-1 truncate text-center font-semibold">#{name}</div>
	<ButtonDark onclick={() => remove(id)}>
		<div class="p-2">
			<Trash2 />
		</div>
	</ButtonDark>
</div>
