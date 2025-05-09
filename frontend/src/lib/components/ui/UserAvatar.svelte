<script lang="ts">
	import { Avatar } from 'bits-ui';
	import pic from '$lib/assets/logo.png';

	let { name, picture } = $props<{ name: string; picture: string }>();
</script>

<Avatar.Root class="text-light border-light aspect-square h-full rounded-full border-2 font-medium">
	<div
		class="m-auto flex h-full w-full items-center justify-center overflow-hidden rounded-full border-2 border-transparent"
	>
		<Avatar.Image src={pic} alt="{name} Discord profile picture" class="h-full w-full" />
		<Avatar.Fallback class="font-semibold uppercase">{name.charAt(0)}</Avatar.Fallback>
	</div>
</Avatar.Root>
