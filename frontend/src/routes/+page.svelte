<script lang="ts">
	import { goto } from '$app/navigation';
	import QuotaLogo from '$lib/assets/QuotaLogo.svelte';
	import ButtonPrimary from '$lib/components/ui/ButtonPrimary.svelte';
	import { ArrowBigRightDash } from 'lucide-svelte';
	import { fly } from 'svelte/transition';
	import { page } from '$app/state';
	import { type Session } from '$lib/server/types';
	import { onMount } from 'svelte';

	let words: string[] = [
		'reimagined',
		'safer',
		'easier',
		'better',
		'ranked',
		'more fun',
		'maintainable'
	];

	let randomWord: string = $state(words[Math.floor(Math.random() * words.length)]);
	let session: Session | undefined = $derived(page.data.session);

	onMount(() => {
		setInterval(() => {
			randomWord = words[Math.floor(Math.random() * words.length)];
		}, 5000);
	});
</script>

<div class="flex h-full w-full flex-col items-center overflow-y-auto p-8 md:p-12 lg:p-24">
	<div class="my-auto flex w-full max-w-4xl flex-col items-center gap-12 md:gap-16 lg:gap-20">
		<div class="flex flex-col items-center gap-6 text-center md:flex-row md:gap-8 md:text-left">
			<div class="text-primary">
				<QuotaLogo size={80} />
			</div>
			<div class="flex flex-col gap-2">
				<h1 class="text-primary text-4xl font-bold tracking-tight md:text-5xl lg:text-6xl">
					Quota
				</h1>
				<p class="text-light text-lg font-medium md:text-xl">Quote Management Bot</p>
			</div>
		</div>

		<div class="flex flex-wrap items-center justify-center gap-2 text-center sm:gap-3">
			<h2 class="text-light text-3xl font-semibold md:text-4xl lg:text-5xl">Your quotes,</h2>
			{#key randomWord}
				<h2
					class="text-primary text-3xl font-semibold md:text-4xl lg:text-5xl"
					in:fly={{ y: -10, duration: 500 }}
				>
					{randomWord}
				</h2>
			{/key}
		</div>

		<div class="flex flex-col items-center gap-6">
			<ButtonPrimary onclick={() => (session ? goto('/dashboard') : goto('/auth/login'))}>
				<div class="text-light flex items-center gap-3 px-6 py-3 md:px-8 md:py-4">
					<span class="text-light text-lg font-semibold md:text-xl"> Get started </span>
					<ArrowBigRightDash size={28} class="md:h-8 md:w-8" />
				</div>
			</ButtonPrimary>
		</div>

		<div class="mt-8 grid w-full max-w-5xl grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
			<div class="border-highlight bg-dark rounded-xl border p-6 text-center backdrop-blur-sm">
				<h3 class="text-primary mb-2 text-lg font-semibold">Secure Storage</h3>
				<p class="text-light text-md font-medium">Your quotes are safely stored and never lost</p>
			</div>

			<div class="border-highlight bg-dark rounded-xl border p-6 text-center backdrop-blur-sm">
				<h3 class="text-primary mb-2 text-lg font-semibold">Community Features</h3>
				<p class="text-light text-md font-medium">Vote, comment, and rank quotes together</p>
			</div>

			<div
				class="border-highlight bg-dark rounded-xl border p-6 text-center backdrop-blur-sm sm:col-span-2 lg:col-span-1"
			>
				<h3 class="text-primary mb-2 text-lg font-semibold">Easy Management</h3>
				<p class="text-light text-md font-medium">
					Organize and search through quotes effortlessly
				</p>
			</div>
		</div>
	</div>
</div>
