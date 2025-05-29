<script lang="ts">
	import Switch from '../ui/Switch.svelte';
	import SettingsItem from './SettingsItem.svelte';
	import SettingsItemSection from './SettingsItemSection.svelte';
</script>

<SettingsItem heading={'Comments'}>
	<div class="flex flex-col gap-4 px-2">
		<SettingsItemSection heading={'Comment settings'}>
			<div class="flex items-center">
				<div class="text-light flex-1 font-semibold">Enable comments</div>
				<Switch />
			</div>
		</SettingsItemSection>
	</div>
</SettingsItem>
