<script lang="ts">
	import { Plus } from 'lucide-svelte';
	import ButtonDark from '../ui/ButtonDark.svelte';
	import type { Channel } from '$lib/server/types';

	let { channel, add } = $props<{ channel: Channel; add: (channel: Channel) => void }>();
</script>

<div class="bg-darker text-light flex items-center gap-3 rounded-xl p-3">
	<div class="text-md flex-1 truncate text-center font-semibold">#{channel.name}</div>
	<ButtonDark onclick={() => add(channel)}>
		<div class="p-2">
			<Plus />
		</div>
	</ButtonDark>
</div>
