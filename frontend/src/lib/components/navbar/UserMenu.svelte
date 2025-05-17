<script lang="ts">
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import { slide } from 'svelte/transition';
	import UserMenuItem from './UserMenuItem.svelte';
	import { Server } from 'lucide-svelte';
	import LayoutGrid from 'lucide-svelte/icons/layout-grid';
	import LogOut from 'lucide-svelte/icons/log-out';
	import Settings from 'lucide-svelte/icons/settings';
	import { afterNavigate } from '$app/navigation';

	let { user } = $props<{ user: { name: string; picture: string } }>();

	let open = $state(false);

	afterNavigate(() => {
		open = false;
	});
</script>

{#if open}
	<div class="fixed inset-0 z-40 h-screen w-screen" onclick={() => (open = false)}></div>
{/if}

<div class="relative z-50 inline-block h-full">
	<button
		onclick={() => {
			open = !open;
		}}
		class="relative flex h-full items-center rounded-xl p-2 {open
			? 'bg-highlight'
			: 'hover:bg-highlight'} transition-colors duration-300"
	>
		<UserAvatar {user} />

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
			class="bg-dark absolute right-0 mt-4 flex w-fit flex-col gap-2 rounded-xl p-4 shadow-black drop-shadow-lg"
			transition:slide={{ duration: 500, axis: 'y' }}
		>
			<UserMenuItem icon={LayoutGrid} text={'Dashboard'} href="/dashboard" />
			<UserMenuItem icon={Server} text={'Servers'} href="/" />
			<UserMenuItem icon={Settings} text={'Settings'} href="/" />
			<UserMenuItem icon={LogOut} text={'Logout'} href="/" />
		</div>
	{/if}
</div>
