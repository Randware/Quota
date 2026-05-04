<script lang="ts">
	import {
		MessageSquare,
		Settings,
		Shield,
		Search,
		Shuffle,
		Trophy,
		Trash2,
		Edit2,
		Key,
		ListX,
		LayoutTemplate
	} from 'lucide-svelte';
	import { fade, fly } from 'svelte/transition';
	import ButtonPrimary from '$lib/components/ui/ButtonPrimary.svelte';

	const categories: {
		title: string;
		icon: any;
		restricted?: boolean;
		commands: { name: string; args: string; description: string; restricted?: boolean }[];
	}[] = [
		{
			title: 'Quote Management',
			icon: MessageSquare,
			commands: [
				{
					name: '/quote',
					args: '<quotee> [content] [image] [discord_user]',
					description:
						'Create a new quote. Must provide either text content or an image attachment.'
				},
				{
					name: '/quote edit',
					args: '<target> [content] [quotee]',
					description: "Edit a quote's text or quotee. Target can be a Message ID or a link.",
					restricted: true
				},
				{
					name: '/quote delete',
					args: '<target>',
					description: 'Delete a quote from the database. Target can be a Message ID or a link.',
					restricted: true
				}
			]
		},
		{
			title: 'Discovery & Stats',
			icon: Search,
			commands: [
				{
					name: '/quote search',
					args: '<query>',
					description: 'Fuzzy search for quotes by character name or content.'
				},
				{
					name: '/quote random',
					args: '',
					description: 'Displays a completely random quote from your server.'
				},
				{
					name: '/quote top',
					args: '',
					description: 'Displays a leaderboard of the top 10 highest-rated quotes.'
				}
			]
		},
		{
			title: 'Server Configuration',
			icon: Settings,
			restricted: true,
			commands: [
				{
					name: '/settings menu',
					args: '',
					description:
						'Opens an interactive, paginated menu to configure voting, comments, emojis, and channel locks.'
				},
				{
					name: '/settings clear_quotes',
					args: '',
					description: 'Danger: Deletes all quotes and votes from the database for this server.'
				},
				{
					name: '/settings reset',
					args: '',
					description:
						'Danger: Completely factory-resets all bot data, quotes, settings, and permissions for the server.'
				}
			]
		},
		{
			title: 'Access Permissions',
			icon: Shield,
			restricted: true,
			commands: [
				{
					name: '/permissions list',
					args: '',
					description: 'Lists all users and roles with special Bot/Dashboard permissions.'
				},
				{
					name: '/permissions grant',
					args: '<level> [user] [role]',
					description:
						'Grants a specific permission level (Dashboard, Admin, Manage Quotes) to a user or role.'
				},
				{
					name: '/permissions revoke',
					args: '<level> [user] [role]',
					description: 'Revokes a specific permission level from a user or role.'
				}
			]
		}
	];
</script>

<div class="relative flex min-h-screen flex-col overflow-x-hidden pt-20">
	<!-- Ambient glow effects from landing page -->
	<div class="pointer-events-none fixed inset-0 overflow-hidden">
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

	<main class="relative z-10 flex-grow pb-32">
		<div class="mx-auto max-w-5xl px-6">
			<!-- Header -->
			<div class="mx-auto mb-16 max-w-2xl text-center" in:fly={{ y: 20, duration: 600 }}>
				<h1 class="text-light mb-6 text-4xl font-bold tracking-tight md:text-5xl lg:text-6xl">
					Command <span class="text-primary">Reference</span>
				</h1>
				<p class="text-light/50 mb-10 text-lg md:text-xl">
					Everything you need to capture, discover, and manage your community's favorite moments.
				</p>
				<div class="flex items-center justify-center gap-4">
					<a
						href="/"
						class="text-light/70 hover:text-light border-light/10 hover:border-light/30 rounded-xl border px-6 py-3 font-semibold transition-all duration-300"
					>
						Back to Home
					</a>
				</div>
			</div>

			<!-- Commands Layout -->
			<div class="flex flex-col gap-10">
				{#each categories as category, i (category.title)}
					<div
						class="bg-dark/60 border-light/5 hover:border-primary/20 hover:bg-dark/80 rounded-2xl border p-6 transition-all duration-500 md:p-8"
						in:fly={{ y: 30, duration: 600, delay: 100 + i * 100 }}
					>
						<div class="mb-6 flex flex-col gap-3 md:flex-row md:items-center">
							<div class="flex items-center gap-3">
								<div
									class="bg-primary/10 text-primary flex h-12 w-12 shrink-0 items-center justify-center rounded-xl"
								>
									<category.icon size={24} />
								</div>
								<h2 class="text-light text-2xl font-bold">{category.title}</h2>
							</div>

							{#if category.restricted}
								<span
									class="ml-0 inline-flex items-center gap-1.5 self-start rounded-md border border-red-500/20 bg-red-500/10 px-2.5 py-1 text-xs font-semibold text-red-400 md:ml-3 md:self-auto"
								>
									<Shield size={14} /> Admin Only
								</span>
							{/if}
						</div>

						<div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
							{#each category.commands as cmd (cmd.name)}
								<div
									class="bg-darker hover:border-primary/20 group relative rounded-xl border border-white/5 p-5 transition-all duration-300"
								>
									<div class="mb-3 flex flex-wrap items-center gap-2">
										<code
											class="text-primary rounded-md bg-white/5 px-2 py-1 font-mono text-sm font-semibold tracking-tight"
										>
											{cmd.name}
										</code>
										{#if cmd.args}
											<code class="text-light/40 font-mono text-xs">
												{cmd.args}
											</code>
										{/if}
										{#if cmd.restricted && !category.restricted}
											<span
												class="ml-auto inline-flex items-center gap-1 rounded border border-red-500/20 bg-red-500/10 px-2 py-0.5 text-[10px] font-bold tracking-wider text-red-400 uppercase"
											>
												<Shield size={10} /> Admin
											</span>
										{/if}
									</div>
									<p
										class="text-light/50 group-hover:text-light/70 text-sm leading-relaxed transition-colors duration-300"
									>
										{cmd.description}
									</p>
								</div>
							{/each}
						</div>
					</div>
				{/each}
			</div>

			<!-- Bottom CTA -->
			<div
				class="bg-dark/60 border-light/5 relative mx-auto mt-20 max-w-3xl overflow-hidden rounded-2xl border p-10 text-center backdrop-blur-xl"
				in:fly={{ y: 20, duration: 800, delay: 400 }}
			>
				<div
					class="bg-primary/10 pointer-events-none absolute -top-20 -right-20 h-60 w-60 rounded-full blur-[80px]"
				></div>
				<div
					class="bg-primary/5 pointer-events-none absolute -bottom-20 -left-20 h-60 w-60 rounded-full blur-[80px]"
				></div>

				<h2 class="text-light relative mb-3 text-2xl font-bold md:text-3xl">Need more help?</h2>
				<p class="text-light/40 relative mb-8 text-base">
					Check out the web dashboard for a visual interface into your server's quotes.
				</p>
				<div class="relative flex justify-center gap-4">
					<a
						href="/dashboard"
						class="bg-primary hover:bg-primary/80 inline-flex items-center justify-center rounded-xl px-8 py-3 font-bold text-white transition-colors duration-300"
					>
						Open Dashboard
					</a>
				</div>
			</div>
		</div>
	</main>
</div>

<style>
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

	:global(.animate-glow-drift) {
		animation: glow-drift 12s ease-in-out infinite;
	}
	:global(.animate-glow-drift-reverse) {
		animation: glow-drift-reverse 15s ease-in-out infinite;
	}
</style>
