<script lang="ts">
	import { Trash2 } from 'lucide-svelte';
	import ButtonDark from '../ui/ButtonDark.svelte';
	import type { Channel } from '$lib/server/types';

	let { channel, remove } = $props<{ channel: Channel; remove: (channel: Channel) => void }>();
</script>

<div class="bg-darker text-light flex items-center gap-3 rounded-xl p-3">
	<div class="text-md flex-1 truncate text-center font-semibold">#{channel.name}</div>
	<ButtonDark onclick={() => remove(channel)}>
		<div class="p-2">
			<Trash2 />
		</div>
	</ButtonDark>
</div>
