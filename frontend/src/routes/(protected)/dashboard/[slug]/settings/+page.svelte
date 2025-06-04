<script lang="ts">
	import type { Guild, Settings } from '$lib/server/types';
	import type { ActionResult, SubmitFunction } from '@sveltejs/kit';
	import { page } from '$app/state';
	import ChannelsSettings from '$lib/components/settings/ChannelsSettings.svelte';
	import VotingSettings from '$lib/components/settings/VotingSettings.svelte';
	import CommentsSettings from '$lib/components/settings/CommentsSettings.svelte';
	import SkeletonSquare from '$lib/components/ui/SkeletonSquare.svelte';
	import ApplyPanel from '$lib/components/settings/ApplyPanel.svelte';
	import { fly } from 'svelte/transition';

	let settingsPromise: Promise<Settings> = $state(page.data.settings);
	let guild: Guild = page.data.guild;

	let initialSettings = $state<Settings | null>(null);
	let workingSettings = $state<Settings | null>(null);

	$effect(() => {
		settingsPromise.then((settings) => {
			initialSettings = { ...settings };
			workingSettings = { ...settings };
		});
	});

	const isModified = $derived(
		initialSettings && workingSettings
			? JSON.stringify(initialSettings) !== JSON.stringify(workingSettings)
			: false
	);

	const handleApply: SubmitFunction = async ({ formData }) => {
		if (!workingSettings) return;

		formData.set('payload', JSON.stringify(workingSettings));
		formData.set('guild', JSON.stringify(guild));

		return async ({ result }: { result: ActionResult }) => {
			if (result.data.success) {
				initialSettings = { ...workingSettings };

				if (initialSettings) {
					settingsPromise = Promise.resolve(initialSettings);
				}
			} else {
				settingsPromise = Promise.reject(new Error(result.data.error));
			}
		};
	};

	function handleRevert() {
		if (initialSettings) {
			workingSettings = initialSettings;
			settingsPromise = Promise.resolve(initialSettings);
		}
	}
</script>

<div class="flex flex-col gap-8 p-8">
	<div class="text-light text-2xl font-semibold">Settings</div>

	<div class="grid grid-cols-1 gap-8 xl:grid-cols-2">
		{#await settingsPromise}
			<SkeletonSquare height={700} />
			<SkeletonSquare height={500} />
			<SkeletonSquare height={300} />
		{:then}
			{#if workingSettings}
				<ChannelsSettings bind:settings={workingSettings} />
				<VotingSettings bind:settings={workingSettings} />
				<CommentsSettings bind:settings={workingSettings} />
			{/if}
		{:catch error: Error}
			<div class="flex flex-col items-center gap-4">
				<div class="text-light text-3xl font-semibold">An error occured</div>
				<div class="text-light text-xl font-medium">
					{error.message}
				</div>
			</div>
		{/await}
	</div>

	{#if isModified}
		<div class="sticky bottom-8 mx-8" transition:fly={{ y: 10 }}>
			<ApplyPanel {handleApply} {handleRevert} />
		</div>
	{/if}
</div>
