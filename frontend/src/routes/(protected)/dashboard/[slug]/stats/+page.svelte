<script lang="ts">
	import type { Stats, MonthlyQuotes, TopQuotee, TopQuote } from '$lib/server/types';
	import { page } from '$app/state';
	import { MessageSquare, ThumbsUp, ThumbsDown, TrendingUp } from 'lucide-svelte';
	import { fly, fade } from 'svelte/transition';
	import { onMount } from 'svelte';
	import SkeletonSquare from '$lib/components/ui/SkeletonSquare.svelte';

	let statsPromise: Promise<Stats> = $derived(page.data.stats);
	let stats = $state<Stats | null>(null);

	$effect(() => {
		statsPromise.then((s) => {
			stats = s;
		});
	});

	let visible = $state(false);
	onMount(() => {
		visible = true;
	});

	// === Area Chart helpers ===
	function monthLabel(m: MonthlyQuotes): string {
		const months = [
			'Jan',
			'Feb',
			'Mar',
			'Apr',
			'May',
			'Jun',
			'Jul',
			'Aug',
			'Sep',
			'Oct',
			'Nov',
			'Dec'
		];
		return months[m.month - 1] ?? '';
	}

	let maxMonthly = $derived(
		stats ? Math.max(...(stats.quotesByMonth?.map((m) => m.count) ?? [0]), 1) : 1
	);

	function areaPath(data: MonthlyQuotes[]): string {
		if (!data || data.length === 0) return '';
		const w = 600,
			h = 200,
			px = 40,
			py = 20;
		const chartW = w - px * 2,
			chartH = h - py * 2;
		const step = chartW / Math.max(data.length - 1, 1);
		let path = '';
		data.forEach((d, i) => {
			const x = px + i * step;
			const y = py + chartH - (d.count / maxMonthly) * chartH;
			path += i === 0 ? `M${x},${y}` : ` L${x},${y}`;
		});
		// close area
		const lastX = px + (data.length - 1) * step;
		path += ` L${lastX},${py + chartH} L${px},${py + chartH} Z`;
		return path;
	}

	function linePath(data: MonthlyQuotes[]): string {
		if (!data || data.length === 0) return '';
		const w = 600,
			h = 200,
			px = 40,
			py = 20;
		const chartW = w - px * 2,
			chartH = h - py * 2;
		const step = chartW / Math.max(data.length - 1, 1);
		let path = '';
		data.forEach((d, i) => {
			const x = px + i * step;
			const y = py + chartH - (d.count / maxMonthly) * chartH;
			path += i === 0 ? `M${x},${y}` : ` L${x},${y}`;
		});
		return path;
	}

	// === Donut Chart ===
	let totalVotes = $derived(stats ? (stats.totalUpvotes ?? 0) + (stats.totalDownvotes ?? 0) : 0);
	let upPct = $derived(totalVotes > 0 ? (stats?.totalUpvotes ?? 0) / totalVotes : 0.5);

	function donutArc(pct: number, radius: number, cx: number, cy: number): string {
		const angle = pct * 360;
		const rad = (angle - 90) * (Math.PI / 180);
		const x = cx + radius * Math.cos(rad);
		const y = cy + radius * Math.sin(rad);
		const largeArc = angle > 180 ? 1 : 0;
		return `M${cx},${cy - radius} A${radius},${radius} 0 ${largeArc} 1 ${x},${y}`;
	}

	// === Bar chart ===
	let maxQuoteeCount = $derived(
		stats ? Math.max(...(stats.topQuotees?.map((q) => q.count) ?? [0]), 1) : 1
	);
</script>

{#if visible}
	<div class="flex h-full w-full flex-col gap-6 overflow-y-auto p-6 md:p-10">
		<h1 in:fly={{ y: 20, duration: 400 }} class="text-light text-2xl font-bold md:text-3xl">
			Statistics
		</h1>

		{#await statsPromise}
			<div class="grid grid-cols-2 gap-4 lg:grid-cols-4">
				<SkeletonSquare height={100} />
				<SkeletonSquare height={100} />
				<SkeletonSquare height={100} />
				<SkeletonSquare height={100} />
			</div>
			<div class="grid gap-6 lg:grid-cols-3">
				<SkeletonSquare height={280} />
				<SkeletonSquare height={280} />
			</div>
		{:then _}
			{#if stats}
				<!-- Stat cards -->
				<div
					in:fly={{ y: 20, duration: 400, delay: 100 }}
					class="grid grid-cols-2 gap-4 lg:grid-cols-4"
				>
					<div class="bg-dark/80 border-light/5 flex flex-col gap-1 rounded-xl border p-5">
						<div class="text-light/40 flex items-center gap-2 text-sm">
							<MessageSquare size={16} /> Total Quotes
						</div>
						<div class="text-light text-3xl font-bold">{stats.totalQuotes ?? 0}</div>
					</div>
					<div class="bg-dark/80 border-light/5 flex flex-col gap-1 rounded-xl border p-5">
						<div class="text-light/40 flex items-center gap-2 text-sm">
							<ThumbsUp size={16} /> Total Upvotes
						</div>
						<div class="text-3xl font-bold text-green-400">{stats.totalUpvotes ?? 0}</div>
					</div>
					<div class="bg-dark/80 border-light/5 flex flex-col gap-1 rounded-xl border p-5">
						<div class="text-light/40 flex items-center gap-2 text-sm">
							<ThumbsDown size={16} /> Total Downvotes
						</div>
						<div class="text-3xl font-bold text-red-400">{stats.totalDownvotes ?? 0}</div>
					</div>
					<div class="bg-dark/80 border-light/5 flex flex-col gap-1 rounded-xl border p-5">
						<div class="text-light/40 flex items-center gap-2 text-sm">
							<TrendingUp size={16} /> Avg Score
						</div>
						<div class="text-primary text-3xl font-bold">
							{stats.totalQuotes
								? ((stats.totalUpvotes - stats.totalDownvotes) / stats.totalQuotes).toFixed(1)
								: '0'}
						</div>
					</div>
				</div>

				<!-- Charts row -->
				<div in:fly={{ y: 20, duration: 400, delay: 200 }} class="grid gap-6 lg:grid-cols-3">
					<!-- Area Chart: Quotes Over Time (spans 2 cols) -->
					<div class="bg-dark/80 border-light/5 rounded-xl border p-5 lg:col-span-2">
						<h3 class="text-light mb-4 text-lg font-semibold">Quotes Over Time</h3>
						{#if stats.quotesByMonth && stats.quotesByMonth.length > 0}
							<svg viewBox="0 0 600 240" class="w-full" preserveAspectRatio="xMidYMid meet">
								<defs>
									<linearGradient id="areaGrad" x1="0" y1="0" x2="0" y2="1">
										<stop offset="0%" stop-color="rgb(247, 111, 83)" stop-opacity="0.3" />
										<stop offset="100%" stop-color="rgb(247, 111, 83)" stop-opacity="0.02" />
									</linearGradient>
								</defs>

								<!-- Grid lines -->
								{#each [0, 0.25, 0.5, 0.75, 1] as pct}
									<line
										x1="40"
										y1={20 + (200 - 40) * (1 - pct)}
										x2="560"
										y2={20 + (200 - 40) * (1 - pct)}
										stroke="rgba(255,255,255,0.05)"
										stroke-width="1"
									/>
									<text
										x="34"
										y={20 + (200 - 40) * (1 - pct) + 4}
										fill="rgba(255,255,255,0.25)"
										font-size="10"
										text-anchor="end"
									>
										{Math.round(maxMonthly * pct)}
									</text>
								{/each}

								<!-- Area fill -->
								<path d={areaPath(stats.quotesByMonth)} fill="url(#areaGrad)" />

								<!-- Line -->
								<path
									d={linePath(stats.quotesByMonth)}
									fill="none"
									stroke="rgb(247, 111, 83)"
									stroke-width="2.5"
									stroke-linecap="round"
									stroke-linejoin="round"
								/>

								<!-- Dots + month labels -->
								{#each stats.quotesByMonth as m, i}
									{@const x = 40 + i * (520 / Math.max(stats.quotesByMonth.length - 1, 1))}
									{@const y = 20 + 160 - (m.count / maxMonthly) * 160}
									<circle cx={x} cy={y} r="3.5" fill="rgb(247, 111, 83)" />
									<circle cx={x} cy={y} r="6" fill="rgb(247, 111, 83)" opacity="0.15" />
									{#if i % 2 === 0 || stats.quotesByMonth.length <= 6}
										<text
											{x}
											y="210"
											fill="rgba(255,255,255,0.3)"
											font-size="10"
											text-anchor="middle"
										>
											{monthLabel(m)}
										</text>
									{/if}
								{/each}
							</svg>
						{:else}
							<div class="text-light/20 flex h-40 items-center justify-center">No data yet</div>
						{/if}
					</div>

					<!-- Donut Chart: Vote Distribution -->
					<div class="bg-dark/80 border-light/5 rounded-xl border p-5">
						<h3 class="text-light mb-4 text-lg font-semibold">Vote Distribution</h3>
						{#if totalVotes > 0}
							<div class="flex flex-col items-center gap-4">
								<svg viewBox="0 0 140 140" class="h-36 w-36">
									<!-- Background ring -->
									<circle
										cx="70"
										cy="70"
										r="55"
										fill="none"
										stroke="rgba(239,68,68,0.3)"
										stroke-width="14"
									/>
									<!-- Upvote arc -->
									<path
										d={donutArc(upPct, 55, 70, 70)}
										fill="none"
										stroke="rgb(74,222,128)"
										stroke-width="14"
										stroke-linecap="round"
									/>
									<!-- Center text -->
									<text
										x="70"
										y="66"
										text-anchor="middle"
										fill="white"
										font-size="18"
										font-weight="bold"
									>
										{Math.round(upPct * 100)}%
									</text>
									<text
										x="70"
										y="82"
										text-anchor="middle"
										fill="rgba(255,255,255,0.4)"
										font-size="10"
									>
										positive
									</text>
								</svg>
								<div class="flex gap-6 text-sm">
									<div class="flex items-center gap-2">
										<div class="h-3 w-3 rounded-full bg-green-400"></div>
										<span class="text-light/60">Upvotes ({stats.totalUpvotes})</span>
									</div>
									<div class="flex items-center gap-2">
										<div class="h-3 w-3 rounded-full bg-red-400"></div>
										<span class="text-light/60">Downvotes ({stats.totalDownvotes})</span>
									</div>
								</div>
							</div>
						{:else}
							<div class="text-light/20 flex h-40 items-center justify-center">No votes yet</div>
						{/if}
					</div>
				</div>

				<!-- Bottom row -->
				<div in:fly={{ y: 20, duration: 400, delay: 300 }} class="grid gap-6 lg:grid-cols-2">
					<!-- Horizontal Bar Chart: Top Quotees -->
					<div class="bg-dark/80 border-light/5 rounded-xl border p-5">
						<h3 class="text-light mb-4 text-lg font-semibold">Most Quoted People</h3>
						{#if stats.topQuotees && stats.topQuotees.length > 0}
							<div class="flex flex-col gap-3">
								{#each stats.topQuotees as quotee, i}
									<div class="flex items-center gap-3">
										<div class="text-light/30 w-5 text-right text-sm font-bold">#{i + 1}</div>
										<div class="flex-grow">
											<div class="text-light mb-1 flex items-center justify-between text-sm">
												<span class="font-medium">{quotee.name}</span>
												<span class="text-light/40">{quotee.count} quotes</span>
											</div>
											<div class="bg-light/5 h-2 overflow-hidden rounded-full">
												<div
													class="h-full rounded-full transition-all duration-700"
													style="width: {(quotee.count / maxQuoteeCount) *
														100}%; background: linear-gradient(90deg, rgb(247, 111, 83), rgb(251, 146, 60));"
												></div>
											</div>
										</div>
									</div>
								{/each}
							</div>
						{:else}
							<div class="text-light/20 flex h-32 items-center justify-center">No quotees yet</div>
						{/if}
					</div>

					<!-- Top Quotes -->
					<div class="bg-dark/80 border-light/5 rounded-xl border p-5">
						<h3 class="text-light mb-4 text-lg font-semibold">Top Quotes</h3>
						{#if stats.topQuotes && stats.topQuotes.length > 0}
							<div class="flex flex-col gap-3">
								{#each stats.topQuotes as quote, i}
									<div class="bg-light/3 hover:bg-light/5 group rounded-lg p-3 transition-colors">
										<div class="mb-2 flex items-start gap-2">
											<div
												class="text-primary bg-primary/10 mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded text-xs font-bold"
											>
												{i + 1}
											</div>
											<p
												class="text-light/70 group-hover:text-light/90 line-clamp-2 text-sm leading-relaxed transition-colors"
											>
												"{quote.content}"
											</p>
										</div>
										<div class="flex items-center gap-3 pl-7">
											<div class="flex items-center gap-1">
												<span class="text-xs text-green-400">👍 {quote.upvotes}</span>
											</div>
											<div class="flex items-center gap-1">
												<span class="text-xs text-red-400">👎 {quote.downvotes}</span>
											</div>
											<div class="text-primary text-xs font-semibold">
												Score: {quote.score}
											</div>
											{#if quote.createdAt}
												<span class="text-light/20 ml-auto text-xs">
													{new Date(quote.createdAt).toLocaleDateString()}
												</span>
											{/if}
										</div>
									</div>
								{/each}
							</div>
						{:else}
							<div class="text-light/20 flex h-32 items-center justify-center">No quotes yet</div>
						{/if}
					</div>
				</div>
			{/if}
		{:catch error}
			<div class="text-light/40 flex h-64 items-center justify-center">
				Failed to load statistics
			</div>
		{/await}
	</div>
{/if}
