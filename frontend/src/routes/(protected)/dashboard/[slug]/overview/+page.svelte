<script lang="ts">
	import QuoteCounter from '$lib/components/overview/QuoteCounter.svelte';
	import ServerPreview from '$lib/components/overview/ServerPreview.svelte';
	import AllowedChannelsPreview from '$lib/components/overview/AllowedChannelsPreview.svelte';
	import EnabledFeaturesPreview from '$lib/components/overview/EnabledFeaturesPreview.svelte';
</script>

<div class="flex flex-col gap-8 p-8">
	<div class="text-light text-2xl font-semibold">
		Your <span class="text-primary">Quota</span> Dashboard
	</div>

	<div class="grid grid-cols-1 gap-8 md:grid-cols-2 xl:grid-cols-3">
		<ServerPreview />
		<QuoteCounter />
		<AllowedChannelsPreview />
		<EnabledFeaturesPreview />
	</div>
</div>
