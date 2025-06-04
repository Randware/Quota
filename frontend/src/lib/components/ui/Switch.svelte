<script lang="ts">
	let { toggled = $bindable() } = $props<{ toggled: boolean }>();
</script>

<button
	onclick={() => (toggled = !toggled)}
	class="border-light/50 bg-dark flex aspect-video h-8 items-center rounded-full border-2
         p-1 transition-colors duration-300 hover:cursor-pointer
         {toggled ? 'bg-primary' : 'bg-dark'}"
>
	<div
		class="bg-light aspect-square h-full rounded-full
             transition-transform duration-300
             {toggled ? 'translate-x-6' : 'translate-x-0'}"
	/>
</button>
