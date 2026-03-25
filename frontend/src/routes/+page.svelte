<script lang="ts">
	import QuotaLogo from '$lib/assets/QuotaLogo.svelte';
	import ButtonPrimary from '$lib/components/ui/ButtonPrimary.svelte';
	import {
		MessageSquare,
		ThumbsUp,
		Hash,
		Shield,
		Zap,
		BarChart3,
		ArrowBigRightDash
	} from 'lucide-svelte';
	import { fly, fade, scale } from 'svelte/transition';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import type { Session } from '$lib/server/types';
	import { onMount } from 'svelte';

	let session: Session | undefined = $derived(page.data.session);

	const words: string[] = [
		'reimagined',
		'safer',
		'easier',
		'better',
		'ranked',
		'more fun',
		'maintainable'
	];

	let currentIndex = $state(Math.floor(Math.random() * words.length));
	let cyclingWord: string = $derived(words[currentIndex]);

	let visible = $state(false);

	onMount(() => {
		visible = true;
		const interval = setInterval(() => {
			currentIndex = (currentIndex + 1) % words.length;
		}, 3000);
		return () => clearInterval(interval);
	});

	const features = [
		{
			icon: MessageSquare,
			title: 'Save Quotes',
			description: 'Capture memorable moments from your Discord server instantly'
		},
		{
			icon: ThumbsUp,
			title: 'Vote & React',
			description: 'Let your community upvote and downvote their favorite quotes'
		},
		{
			icon: Hash,
			title: 'Channel Control',
			description: 'Choose exactly which channels Quota listens to'
		},
		{
			icon: Shield,
			title: 'Permission System',
			description: 'Fine-grained access control for server administrators'
		},
		{
			icon: Zap,
			title: 'Instant Setup',
			description: 'Add to your server and start quoting in under a minute'
		},
		{
			icon: BarChart3,
			title: 'Statistics',
			description: 'Track quote activity with a beautiful analytics dashboard'
		}
	];
</script>

<div class="relative flex flex-1 flex-col overflow-x-hidden overflow-y-auto">
	<!-- Ambient glow effects -->
	<div class="pointer-events-none absolute inset-0 overflow-hidden">
		<div
			class="animate-glow-drift bg-primary/5 absolute -top-40 -left-40 h-[500px] w-[500px] rounded-full blur-[120px]"
		></div>
		<div
			class="animate-glow-drift-reverse bg-primary/8 absolute top-1/3 -right-40 h-[400px] w-[400px] rounded-full blur-[100px]"
		></div>
		<div
			class="animate-glow-drift bg-primary/4 absolute bottom-0 left-1/3 h-[350px] w-[350px] rounded-full blur-[100px]"
		></div>
	</div>

	<!-- Hero Section -->
	<section
		class="relative z-10 flex flex-col items-center justify-center px-6 pt-16 pb-12 text-center md:pt-28 md:pb-20"
	>
		{#if visible}
			<div in:scale={{ duration: 800, start: 0.8, opacity: 0 }} class="mb-10">
				<div class="animate-float relative inline-flex items-center justify-center">
					<div class="bg-primary/20 absolute inset-0 animate-pulse rounded-full blur-xl"></div>
					<div class="text-primary relative">
						<QuotaLogo size={100} />
					</div>
				</div>
			</div>

			<!-- Cycling headline -->
			<div
				in:fly={{ y: 30, duration: 600, delay: 200 }}
				class="mb-4 flex flex-col items-center gap-2"
			>
				<h1 class="text-light text-4xl font-bold tracking-tight md:text-5xl lg:text-6xl">
					Your quotes,
				</h1>
				<div
					class="cycling-word-container relative h-[1.2em] text-4xl font-bold md:text-5xl lg:text-6xl"
				>
					{#key cyclingWord}
						<span
							class="text-primary absolute top-0 left-1/2 -translate-x-1/2 font-bold whitespace-nowrap"
							in:fly={{ y: -30, duration: 400 }}
							out:fly={{ y: 30, duration: 300 }}
						>
							{cyclingWord}
						</span>
					{/key}
				</div>
			</div>

			<p
				in:fly={{ y: 30, duration: 600, delay: 400 }}
				class="text-light/50 mb-10 max-w-xl text-lg md:text-xl"
			>
				The Discord bot that captures, curates, and celebrates the best moments from your server.
			</p>

			<div
				in:fly={{ y: 30, duration: 600, delay: 600 }}
				class="flex flex-wrap items-center justify-center gap-4"
			>
				<ButtonPrimary onclick={() => (session ? goto('/dashboard') : goto('/auth/login'))}>
					<div class="text-light flex items-center gap-3 px-8 py-3 text-lg font-bold">
						Get started
						<ArrowBigRightDash size={24} />
					</div>
				</ButtonPrimary>

				<button
					onclick={() => goto('/auth/invite')}
					class="text-light/70 hover:text-light border-light/10 hover:border-light/30 rounded-xl border px-8 py-3 text-lg font-semibold transition-all duration-300 hover:cursor-pointer"
				>
					Add to Server
				</button>
			</div>
		{/if}
	</section>

	<!-- Floating quote preview -->
	{#if visible}
		<section
			in:fly={{ y: 60, duration: 800, delay: 800 }}
			class="relative z-10 mx-auto mb-20 w-full max-w-2xl px-6"
		>
			<div
				class="bg-dark/80 border-light/5 animate-float-slow relative overflow-hidden rounded-2xl border p-6 shadow-2xl shadow-black/40 backdrop-blur-xl"
			>
				<div class="text-light/30 absolute top-4 right-4 text-4xl font-bold">"</div>
				<div class="flex items-start gap-4">
					<div
						class="bg-primary/20 flex h-10 w-10 shrink-0 items-center justify-center rounded-full"
					>
						<span class="text-lg">🎮</span>
					</div>
					<div class="flex flex-col gap-1">
						<div class="flex items-center gap-2">
							<span class="text-primary text-sm font-bold">PlayerOne</span>
							<span class="text-light/20 text-xs">Today at 4:20 PM</span>
						</div>
						<p class="text-light/80 text-base leading-relaxed">
							"I didn't say it was a good plan, I said it was <em class="text-primary/80">a</em> plan."
						</p>
						<div class="mt-2 flex gap-3">
							<span class="text-light/40 flex items-center gap-1 text-xs"
								>👍 <span class="text-light/60">12</span></span
							>
							<span class="text-light/40 flex items-center gap-1 text-xs"
								>👎 <span class="text-light/60">2</span></span
							>
						</div>
					</div>
				</div>
			</div>
		</section>
	{/if}

	<!-- Features -->
	{#if visible}
		<section class="relative z-10 mx-auto w-full max-w-5xl px-6 pb-24">
			<div in:fly={{ y: 30, duration: 600, delay: 1000 }} class="mb-12 text-center">
				<h2 class="text-light mb-3 text-3xl font-bold md:text-4xl">
					Everything you need to
					<span class="text-primary">quote</span>
				</h2>
				<p class="text-light/40 text-lg">Powerful features, zero setup friction</p>
			</div>

			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
				{#each features as feature, i (feature.title)}
					<div
						in:fly={{ y: 40, duration: 500, delay: 1100 + i * 100 }}
						class="group bg-dark/60 border-light/5 hover:border-primary/20 hover:bg-dark/80 relative overflow-hidden rounded-xl border p-6 transition-all duration-500"
					>
						<div
							class="bg-primary/0 group-hover:bg-primary/5 pointer-events-none absolute inset-0 transition-colors duration-500"
						></div>

						<div class="relative">
							<div
								class="bg-primary/10 group-hover:bg-primary/20 text-primary mb-4 inline-flex rounded-lg p-2.5 transition-colors duration-300"
							>
								<feature.icon size={22} />
							</div>
							<h3 class="text-light mb-2 text-lg font-semibold">{feature.title}</h3>
							<p
								class="text-light/40 group-hover:text-light/60 text-sm leading-relaxed transition-colors duration-300"
							>
								{feature.description}
							</p>
						</div>
					</div>
				{/each}
			</div>
		</section>
	{/if}

	<!-- Bottom CTA -->
	{#if visible}
		<section in:fade={{ duration: 800, delay: 1800 }} class="relative z-10 px-6 pb-20">
			<div
				class="bg-dark/60 border-light/5 relative mx-auto max-w-3xl overflow-hidden rounded-2xl border p-10 text-center backdrop-blur-xl"
			>
				<div
					class="bg-primary/10 pointer-events-none absolute -top-20 -right-20 h-60 w-60 rounded-full blur-[80px]"
				></div>
				<div
					class="bg-primary/5 pointer-events-none absolute -bottom-20 -left-20 h-60 w-60 rounded-full blur-[80px]"
				></div>

				<h2 class="text-light relative mb-3 text-2xl font-bold md:text-3xl">
					Ready to start quoting?
				</h2>
				<p class="text-light/40 relative mb-8 text-base">
					Join thousands of Discord servers already using Quota
				</p>
				<div class="relative flex flex-wrap items-center justify-center gap-4">
					<ButtonPrimary onclick={() => goto('/auth/invite')}>
						<div class="text-light px-8 py-3 text-lg font-bold">Add Quota to Your Server</div>
					</ButtonPrimary>
				</div>
			</div>
		</section>
	{/if}

	<!-- Footer -->
	<footer class="text-light/20 relative z-10 pt-4 pb-8 text-center text-sm">
		<p>Built by <span class="text-primary/60">Randware</span> · Powered by Discord</p>
	</footer>
</div>

<style>
	@keyframes float {
		0%,
		100% {
			transform: translateY(0);
		}
		50% {
			transform: translateY(-10px);
		}
	}
	@keyframes float-slow {
		0%,
		100% {
			transform: translateY(0) rotate(0deg);
		}
		50% {
			transform: translateY(-6px) rotate(0.5deg);
		}
	}
	@keyframes glow-drift {
		0%,
		100% {
			transform: translate(0, 0) scale(1);
		}
		33% {
			transform: translate(30px, -20px) scale(1.1);
		}
		66% {
			transform: translate(-20px, 10px) scale(0.95);
		}
	}
	@keyframes glow-drift-reverse {
		0%,
		100% {
			transform: translate(0, 0) scale(1);
		}
		33% {
			transform: translate(-30px, 20px) scale(0.95);
		}
		66% {
			transform: translate(20px, -10px) scale(1.1);
		}
	}

	:global(.animate-float) {
		animation: float 4s ease-in-out infinite;
	}
	:global(.animate-float-slow) {
		animation: float-slow 6s ease-in-out infinite;
	}
	:global(.animate-glow-drift) {
		animation: glow-drift 12s ease-in-out infinite;
	}
	:global(.animate-glow-drift-reverse) {
		animation: glow-drift-reverse 15s ease-in-out infinite;
	}
</style>
