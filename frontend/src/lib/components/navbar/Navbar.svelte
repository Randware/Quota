<script lang="ts">
	import CubeContainer from '$lib/components/ui/CubeContainer.svelte';
	import QuotaLogo from '$lib/assets/QuotaLogo.svelte';
	import LoginButton from './LoginButton.svelte';
	import UserMenu from './UserMenu.svelte';

	let { user } = $props<{ user: { name: string; picture: string } | null }>();
</script>

<header class="bg-dark flex w-full items-center">
	<CubeContainer>
		<a href="/" class="text-primary p-5">
			<QuotaLogo size={'100%'} />
		</a>
	</CubeContainer>

	<CubeContainer>
		<div class="text-primary text-lg font-semibold sm:text-xl">Quota</div>
	</CubeContainer>

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
