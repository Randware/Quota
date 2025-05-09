<script lang="ts">
	import { DropdownMenu } from 'bits-ui';
	import UserAvatar from '../ui/UserAvatar.svelte';

	let { user } = $props<{ user: { name: string; picture: string } }>();

	let open = $state(false);
</script>

<DropdownMenu.Root bind:open>
	<DropdownMenu.Trigger class="flex h-full items-center rounded-xl">
		<UserAvatar name={user.name} picture={user.picture} />
	</DropdownMenu.Trigger>

	<DropdownMenu.Content class="text-light bg-dark text-md rounded-xl p-4 font-medium">
		<DropdownMenu.Arrow />

		<DropdownMenu.Group>
			<DropdownMenu.GroupHeading class="mb-2 flex items-center gap-2">
				<UserAvatar name={user.name} picture={user.picture} />
				<div class="text-lg font-semibold">@{user.name}</div>
			</DropdownMenu.GroupHeading>
			<DropdownMenu.Item>Servers</DropdownMenu.Item>
			<DropdownMenu.Item>Settings</DropdownMenu.Item>
		</DropdownMenu.Group>

		<DropdownMenu.Separator class="border-light my-2 border-1" />

		<DropdownMenu.Group>
			<DropdownMenu.Item>Logout</DropdownMenu.Item>
		</DropdownMenu.Group>
	</DropdownMenu.Content>
</DropdownMenu.Root>
