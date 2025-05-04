<script lang="ts">
	import ContainerCube from './ContainerCube.svelte';
	import LoginButton from './LoginButton.svelte';
	import QuotaLogo from '$lib/assets/QuotaLogo.svelte';

	let { user }: { user: { name: string; picture: string } | null } = $props();
</script>

<header class="bg-dark flex w-full">
	<ContainerCube>
		<a href="/" class="text-primary p-5">
			<QuotaLogo size={'100%'} />
		</a>
	</ContainerCube>

	<ContainerCube>
		<div class="text-primary text-lg font-semibold sm:text-xl">Quota</div>
	</ContainerCube>

	<div class="flex w-full items-center p-4">
		{#if !user}
			<div class="ms-auto">
				<LoginButton />
			</div>
		{:else}
			<div class="">{user.name}</div>
		{/if}
	</div>
</header>
