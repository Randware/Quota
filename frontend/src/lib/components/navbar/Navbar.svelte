<script lang="ts">
	import QuotaLogo from '$lib/assets/QuotaLogo.svelte';
	import LoginButton from './LoginButton.svelte';
	import UserMenu from './UserMenu.svelte';

	let { user } = $props<{ user: { name: string; picture: string } | null }>();
</script>

<header class="bg-dark flex h-20 w-full items-center gap-2 p-2">
	<a href="/" class="text-primary aspect-square h-full p-3">
		<QuotaLogo size={'100%'} />
	</a>

	<div
		class="text-primary flex aspect-square h-full items-center justify-center text-lg font-semibold sm:text-xl"
	>
		Quota
	</div>

	<div class="flex h-full w-full items-center p-4">
		<div class="ms-auto flex h-full items-center">
			{#if user}
				<UserMenu {user} />
			{:else}
				<LoginButton />
			{/if}
		</div>
	</div>
</header>
