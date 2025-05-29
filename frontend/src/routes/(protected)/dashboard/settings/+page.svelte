<script lang="ts">
	import ChannelsSettings from '$lib/components/settings/ChannelsSettings.svelte';
	import CommentsSettings from '$lib/components/settings/CommentsSettings.svelte';
	import VotingSettings from '$lib/components/settings/VotingSettings.svelte';
</script>

<div class="grid grid-cols-1 gap-8 p-8 xl:grid-cols-2">
	<ChannelsSettings />
	<VotingSettings />
	<CommentsSettings />
</div>
