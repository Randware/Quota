<script lang="ts">
	import Avatar from '$lib/components/ui/Avatar.svelte';
	import { slide } from 'svelte/transition';
	import UserMenuItem from './UserMenuItem.svelte';
	import LayoutGrid from 'lucide-svelte/icons/layout-grid';
	import LogOut from 'lucide-svelte/icons/log-out';
	import Settings from 'lucide-svelte/icons/settings';
	import { afterNavigate } from '$app/navigation';
	import { createFloatingActions } from 'svelte-floating-ui';
	import { offset } from 'svelte-floating-ui/dom';

	let { user } = $props<{ user: { name: string; picture: string } }>();

	let open = $state(false);

	const [referenceAction, floatingAction] = createFloatingActions({
		placement: 'bottom-end',
		middleware: [offset(15)],
		autoUpdate: true
	});

	afterNavigate(() => {
		open = false;
	});
</script>

{#if open}
	<div class="fixed inset-0 z-40 h-screen w-screen" onclick={() => (open = false)}></div>
{/if}

<div class="z-50 h-full">
	<button
		use:referenceAction
		onclick={() => {
			open = !open;
		}}
		class="flex h-full items-center rounded-xl p-2 {open
			? 'bg-highlight'
			: 'hover:bg-highlight'} transition-colors duration-300"
	>
		<Avatar image={user.picture} text={user.name} />

		{#if open}
			<div
				class="text-light text-md mx-2 font-semibold sm:text-lg"
				transition:slide={{ duration: 500, axis: 'x' }}
			>
				@{user.name}
			</div>
		{/if}
	</button>

	{#if open}
		<div
			use:floatingAction
			class="bg-dark right-0 flex w-fit flex-col gap-2 rounded-xl p-4 shadow-black drop-shadow-lg"
			transition:slide={{ duration: 500, axis: 'y' }}
		>
			<UserMenuItem icon={LayoutGrid} text={'Dashboard'} href="/dashboard" />
			<UserMenuItem icon={Settings} text={'Settings'} href="/" />
			<UserMenuItem icon={LogOut} text={'Logout'} href="/" />
		</div>
	{/if}
</div>
