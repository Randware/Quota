<script lang="ts">
	import { page } from '$app/state';
	import { goto, invalidateAll } from '$app/navigation';
	import { enhance } from '$app/forms';
	import { fly, fade } from 'svelte/transition';
	import { onMount } from 'svelte';
	import {
		Search,
		Pencil,
		Trash2,
		ChevronLeft,
		ChevronRight,
		MessageSquareText,
		ThumbsUp,
		ThumbsDown,
		Check,
		X,
		AlertTriangle
	} from 'lucide-svelte';
	import ButtonPrimary from '$lib/components/ui/ButtonPrimary.svelte';
	import ButtonDark from '$lib/components/ui/ButtonDark.svelte';
	import type { ActionResult, SubmitFunction } from '@sveltejs/kit';
	import type { QuoteItem } from '$lib/server/quotes';

	let quotesData = $derived(page.data.quotesData);
	let searchQuery = $state(page.data.search ?? '');

	let visible = $state(false);
	onMount(() => {
		visible = true;
	});

	// Edit state
	let editingQuoteId = $state<string | null>(null);
	let editContent = $state('');
	let editQuotees = $state('');

	// Delete confirmation state
	let deletingQuoteId = $state<string | null>(null);
	let isResetConfirming = $state(false);
	let isResetting = $state(false);

	// Toast notification
	let toast = $state<{ message: string; type: 'success' | 'error' | 'warning' } | null>(null);
	let toastTimeout: ReturnType<typeof setTimeout>;

	function showToast(message: string, type: 'success' | 'error' | 'warning' = 'success') {
		if (toastTimeout) clearTimeout(toastTimeout);
		toast = { message, type };
		toastTimeout = setTimeout(() => {
			toast = null;
		}, 4000);
	}

	function startEdit(quote: QuoteItem) {
		editingQuoteId = quote.id;
		editContent = quote.content;
		editQuotees = quote.quotees.join(', ');
	}

	function cancelEdit() {
		editingQuoteId = null;
		editContent = '';
		editQuotees = '';
	}

	function confirmDelete(quoteId: string) {
		deletingQuoteId = quoteId;
	}

	function cancelDelete() {
		deletingQuoteId = null;
	}

	// Search handler
	let searchTimeout: ReturnType<typeof setTimeout>;
	function handleSearch(e: Event) {
		const value = (e.target as HTMLInputElement).value;
		searchQuery = value;
		if (searchTimeout) clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			const url = new URL(window.location.href);
			if (value) {
				url.searchParams.set('search', value);
			} else {
				url.searchParams.delete('search');
			}
			url.searchParams.set('page', '1');
			goto(url.toString(), { invalidateAll: true });
		}, 400);
	}

	function goToPage(pageNum: number) {
		const url = new URL(window.location.href);
		url.searchParams.set('page', pageNum.toString());
		goto(url.toString(), { invalidateAll: true });
	}

	const handleEdit: SubmitFunction = () => {
		return async ({ result }: { result: ActionResult }) => {
			if (result.type === 'success' && result.data?.success) {
				editingQuoteId = null;
				editContent = '';
				editQuotees = '';
				if (result.data.discordUpdated) {
					showToast('Quote updated — Discord message synced!', 'success');
				} else {
					showToast('Quote updated in database (Discord sync unavailable)', 'warning');
				}
				await invalidateAll();
			} else {
				showToast('Failed to update quote', 'error');
			}
		};
	};

	const handleDelete: SubmitFunction = () => {
		return async ({ result }: { result: ActionResult }) => {
			if (result.type === 'success' && result.data?.success) {
				deletingQuoteId = null;
				if (result.data.discordDeleted) {
					showToast('Quote deleted — Discord message removed!', 'success');
				} else {
					showToast('Quote deleted from database (Discord message may persist)', 'warning');
				}
				await invalidateAll();
			} else {
				showToast('Failed to delete quote', 'error');
			}
		};
	};

	const handleResetQuotes: SubmitFunction = () => {
		isResetting = true;
		return async ({ result }: { result: ActionResult }) => {
			isResetting = false;
			if (result.type === 'success' && result.data?.success) {
				isResetConfirming = false;
				showToast('All quotes deleted and Discord messages removed.', 'success');
				await invalidateAll();
			} else {
				showToast('Failed to delete all quotes', 'error');
			}
		};
	};

	function openResetConfirm() {
		isResetConfirming = true;
	}

	function cancelResetConfirm() {
		isResetConfirming = false;
	}

	function confirmReset() {
		const form = document.getElementById('reset-quotes-form') as HTMLFormElement | null;
		form?.requestSubmit();
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleDateString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}
</script>

{#if visible}
	<div class="flex h-full w-full flex-col gap-6 overflow-y-auto p-6 md:p-10">
		<!-- Header -->
		<div in:fly={{ y: 20, duration: 400 }} class="flex items-center justify-between gap-4">
			<div class="flex items-center gap-3">
				<MessageSquareText size={28} class="text-primary" />
				<h1 class="text-light text-2xl font-bold md:text-3xl">Quotes</h1>
				{#if quotesData.totalCount > 0}
					<span class="bg-primary/10 text-primary rounded-full px-3 py-0.5 text-sm font-medium">
						{quotesData.totalCount}
					</span>
				{/if}
			</div>
			<ButtonDark onclick={openResetConfirm} type="button">
				<div class="text-light px-4 py-2 text-sm font-semibold">Reset Quotes</div>
			</ButtonDark>
		</div>

		<form id="reset-quotes-form" method="POST" action="?/resetQuotes" use:enhance={handleResetQuotes} />

		<!-- Search bar -->
		<div in:fly={{ y: 20, duration: 400, delay: 100 }} class="relative">
			<Search
				size={18}
				class="text-light/30 pointer-events-none absolute top-1/2 left-4 -translate-y-1/2"
			/>
			<input
				type="text"
				placeholder="Search by content or quotee name…"
				value={searchQuery}
				oninput={handleSearch}
				class="bg-dark/80 border-light/10 text-light placeholder:text-light/30 focus:border-primary/50 focus:ring-primary/20 w-full rounded-xl border py-3 pr-4 pl-11 text-sm transition-all outline-none focus:ring-2"
			/>
		</div>

		<!-- Quotes list -->
		<div in:fly={{ y: 20, duration: 400, delay: 200 }} class="flex flex-col gap-3">
			{#if quotesData.quotes.length === 0}
				<div
					class="bg-dark/80 border-light/5 text-light/30 flex h-48 items-center justify-center rounded-xl border text-lg"
				>
					{searchQuery ? 'No quotes match your search' : 'No quotes yet'}
				</div>
			{:else}
				{#each quotesData.quotes as quote (quote.id)}
					<div
						class="bg-dark/80 border-light/5 hover:border-light/10 group rounded-xl border p-5 transition-all duration-200"
						transition:fly={{ y: 10, duration: 200 }}
					>
						<!-- Header row -->
						<div class="mb-3 flex items-start justify-between gap-4">
							<div class="flex items-center gap-2">
								{#each quote.quotees as quotee}
									<span
										class="bg-primary/10 text-primary rounded-md px-2 py-0.5 text-xs font-medium"
									>
										{quotee}
									</span>
								{/each}
								<span class="text-light/20 text-xs">{formatDate(quote.createdAt)}</span>
							</div>

							<!-- Action buttons -->
							<div
								class="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100"
							>
								{#if editingQuoteId !== quote.id && deletingQuoteId !== quote.id}
									<button
										class="hover:bg-light/10 rounded-lg p-2 text-blue-400 transition-colors"
										title="Edit quote"
										onclick={() => startEdit(quote)}
									>
										<Pencil size={15} />
									</button>
									<button
										class="hover:bg-light/10 rounded-lg p-2 text-red-400 transition-colors"
										title="Delete quote"
										onclick={() => confirmDelete(quote.id)}
									>
										<Trash2 size={15} />
									</button>
								{/if}
							</div>
						</div>

						<!-- Content -->
						{#if editingQuoteId === quote.id}
							<!-- Edit mode -->
							<form method="POST" action="?/editQuote" use:enhance={handleEdit}>
								<input type="hidden" name="quoteId" value={quote.id} />
								<input type="hidden" name="quotees" bind:value={editQuotees} />
								<label class="text-light/40 mb-1 block text-xs font-medium">Said by</label>
								<input
									type="text"
									bind:value={editQuotees}
									placeholder="Name1, Name2, …"
									class="bg-darker border-primary/30 text-light focus:border-primary focus:ring-primary/20 mb-2 w-full rounded-lg border px-3 py-2 text-sm transition-all outline-none focus:ring-2"
								/>
								<label class="text-light/40 mb-1 block text-xs font-medium">Quote</label>
								<textarea
									name="content"
									bind:value={editContent}
									rows="3"
									class="bg-darker border-primary/30 text-light focus:border-primary focus:ring-primary/20 mb-3 w-full resize-none rounded-lg border p-3 text-sm transition-all outline-none focus:ring-2"
								></textarea>
								<div class="flex items-center gap-2">
									<button
										type="submit"
										class="bg-primary hover:bg-primary/80 flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium text-white transition-colors"
									>
										<Check size={14} /> Save
									</button>
									<button
										type="button"
										onclick={cancelEdit}
										class="bg-light/10 hover:bg-light/20 text-light/60 flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium transition-colors"
									>
										<X size={14} /> Cancel
									</button>
									<span class="text-light/20 ml-2 text-xs italic"
										>This will update the Discord message in real-time</span
									>
								</div>
							</form>
						{:else if deletingQuoteId === quote.id}
							<!-- Delete confirmation -->
							<div class="rounded-lg border border-red-500/20 bg-red-500/10 p-4">
								<div class="mb-3 flex items-center gap-2 text-red-400">
									<AlertTriangle size={16} />
									<span class="text-sm font-semibold"
										>This will permanently delete this quote and its Discord message.</span
									>
								</div>
								<form method="POST" action="?/deleteQuote" use:enhance={handleDelete}>
									<input type="hidden" name="quoteId" value={quote.id} />
									<div class="flex items-center gap-2">
										<button
											type="submit"
											class="flex items-center gap-1.5 rounded-lg bg-red-500 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-red-600"
										>
											<Trash2 size={14} /> Delete Forever
										</button>
										<button
											type="button"
											onclick={cancelDelete}
											class="bg-light/10 hover:bg-light/20 text-light/60 flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium transition-colors"
										>
											<X size={14} /> Cancel
										</button>
									</div>
								</form>
							</div>
						{:else}
							<!-- Display mode -->
							<p class="text-light/70 mb-3 leading-relaxed">
								{#if quote.content}
									"{quote.content}"
								{:else}
									<span class="text-light/30 italic">Media-only quote</span>
								{/if}
							</p>
						{/if}

						<!-- Footer stats -->
						<div class="border-light/5 flex items-center gap-4 border-t pt-3">
							<div class="flex items-center gap-1.5 text-green-400">
								<ThumbsUp size={13} />
								<span class="text-xs">{quote.upvotes}</span>
							</div>
							<div class="flex items-center gap-1.5 text-red-400">
								<ThumbsDown size={13} />
								<span class="text-xs">{quote.downvotes}</span>
							</div>
							<div class="text-primary text-xs font-semibold">
								Score: {quote.score}
							</div>
							{#if quote.mediaUrls}
								<span class="bg-light/5 text-light/30 rounded px-1.5 py-0.5 text-xs">📎 Media</span>
							{/if}
						</div>
					</div>
				{/each}
			{/if}
		</div>

		<!-- Pagination -->
		{#if quotesData.totalPages > 1}
			<div
				in:fly={{ y: 20, duration: 400, delay: 300 }}
				class="flex items-center justify-center gap-2 pb-6"
			>
				<button
					disabled={quotesData.page <= 1}
					onclick={() => goToPage(quotesData.page - 1)}
					class="bg-dark/80 border-light/10 text-light/60 hover:bg-light/10 rounded-lg border p-2 transition-colors disabled:opacity-30"
				>
					<ChevronLeft size={18} />
				</button>

				{#each Array(quotesData.totalPages) as _, i}
					{@const pageNum = i + 1}
					{#if pageNum === 1 || pageNum === quotesData.totalPages || Math.abs(pageNum - quotesData.page) <= 1}
						<button
							onclick={() => goToPage(pageNum)}
							class="rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors {pageNum ===
							quotesData.page
								? 'bg-primary border-primary text-white'
								: 'bg-dark/80 border-light/10 text-light/60 hover:bg-light/10'}"
						>
							{pageNum}
						</button>
					{:else if Math.abs(pageNum - quotesData.page) === 2}
						<span class="text-light/20 px-1">…</span>
					{/if}
				{/each}

				<button
					disabled={quotesData.page >= quotesData.totalPages}
					onclick={() => goToPage(quotesData.page + 1)}
					class="bg-dark/80 border-light/10 text-light/60 hover:bg-light/10 rounded-lg border p-2 transition-colors disabled:opacity-30"
				>
					<ChevronRight size={18} />
				</button>
			</div>
		{/if}
	</div>
{/if}

{#if isResetConfirming}
	<div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
		<div class="bg-dark ring-highlight w-full max-w-md rounded-xl p-6 ring-2">
			<div class="text-light text-lg font-semibold">Delete all quotes?</div>
			<div class="text-light/70 mt-2 text-sm">
				This will permanently delete all quotes for this server from Discord and the database.
			</div>
			<div class="text-red-400 mt-3 text-sm font-semibold">
				This action is irreversible. Your data will be permanently deleted.
			</div>
			<div class="mt-6 flex justify-end gap-3">
				<ButtonDark onclick={cancelResetConfirm} disabled={isResetting} type="button">
					<div class="text-light px-4 py-2 font-semibold">Cancel</div>
				</ButtonDark>
				<ButtonPrimary onclick={confirmReset} disabled={isResetting} type="button">
					<div class="text-light px-4 py-2 font-semibold">
						{isResetting ? 'Deleting…' : 'Delete All'}
					</div>
				</ButtonPrimary>
			</div>
		</div>
	</div>
{/if}

<!-- Toast notification -->
{#if toast}
	<div
		class="fixed right-6 bottom-6 z-50 flex items-center gap-3 rounded-xl px-5 py-3 shadow-2xl backdrop-blur-sm
			{toast.type === 'success'
			? 'border border-green-500/20 bg-green-500/10 text-green-400'
			: toast.type === 'warning'
				? 'border border-yellow-500/20 bg-yellow-500/10 text-yellow-400'
				: 'border border-red-500/20 bg-red-500/10 text-red-400'}"
		transition:fly={{ y: 20, duration: 300 }}
	>
		{#if toast.type === 'success'}
			<Check size={18} />
		{:else if toast.type === 'warning'}
			<AlertTriangle size={18} />
		{:else}
			<X size={18} />
		{/if}
		<span class="text-sm font-medium">{toast.message}</span>
	</div>
{/if}
