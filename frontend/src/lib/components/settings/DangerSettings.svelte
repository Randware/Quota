<script lang="ts">
	import { enhance } from '$app/forms';
	import type { SubmitFunction } from '@sveltejs/kit';
	import SettingsItem from './SettingsItem.svelte';
	import SettingsItemSection from './SettingsItemSection.svelte';
	import { toast } from 'svelte-sonner';

	let isDeleting = false;
	let isResetting = false;

	const handleDelete: SubmitFunction = () => {
		isDeleting = true;
		return async ({ result }) => {
			isDeleting = false;
			if (result.type === 'success') {
				toast.success('All quotes have been deleted.');
			} else {
				toast.error('Failed to delete quotes.');
			}
		};
	};

	const handleReset: SubmitFunction = () => {
		isResetting = true;
		return async ({ result }) => {
			isResetting = false;
			if (result.type === 'success') {
				toast.success('Bot data has been reset.');
				window.location.href = '/dashboard';
			} else {
				toast.error('Failed to reset bot data.');
			}
		};
	};
</script>

<SettingsItem heading={'Danger Zone'} danger={true}>
	<div class="flex flex-col gap-4 px-2">
		<SettingsItemSection heading={'Delete All Quotes'} danger={true}>
			<div class="flex items-center gap-4">
				<div class="text-light flex-1">
					<p class="font-semibold text-red-500">Irreversible Action</p>
					<p class="text-sm opacity-80">
						This will permanently delete all quotes for this server from the database. The Discord
						messages will remain, but they will no longer be tracked.
					</p>
				</div>

				<form action="?/deleteQuotes" method="POST" use:enhance={handleDelete}>
					<button
						class="rounded-xl bg-red-500/20 px-4 py-2 font-semibold text-red-500 transition hover:bg-red-500/30"
						disabled={isDeleting}
						onclick={(e) => {
							if (
								!confirm(
									'Are you absolutely sure you want to delete ALL quotes for this server? This cannot be undone.'
								)
							) {
								e.preventDefault();
							}
						}}
					>
						{isDeleting ? 'Deleting...' : 'Delete Quotes'}
					</button>
				</form>
			</div>
		</SettingsItemSection>

		<SettingsItemSection heading={'Reset Bot Data'} danger={true}>
			<div class="flex items-center gap-4">
				<div class="text-light flex-1">
					<p class="font-semibold text-red-500">Irreversible Action</p>
					<p class="text-sm opacity-80">
						This will permanently delete all quotes, wipe all settings/configurations, and remove
						all custom permissions for this server. The bot will appear as if it just joined.
					</p>
				</div>

				<form action="?/resetData" method="POST" use:enhance={handleReset}>
					<button
						class="text-light rounded-xl bg-red-500 px-4 py-2 font-semibold transition hover:bg-red-600"
						disabled={isResetting}
						onclick={(e) => {
							if (
								!confirm(
									'Are you absolutely sure you want to RESET the bot? All quotes, settings, and permissions will be lost forever.'
								)
							) {
								e.preventDefault();
							}
						}}
					>
						{isResetting ? 'Resetting...' : 'Reset Bot'}
					</button>
				</form>
			</div>
		</SettingsItemSection>
	</div>
</SettingsItem>
