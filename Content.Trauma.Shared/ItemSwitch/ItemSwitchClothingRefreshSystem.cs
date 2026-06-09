// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Clothing.Components;
using Content.Medical.Shared.ItemSwitch;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Trauma.Shared.ItemSwitch;

public sealed partial class ItemSwitchClothingRefreshSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemSwitchComponent, ItemSwitchedEvent>(OnItemSwitched);
    }

    private readonly HashSet<EntityUid> _processing = new();

    private void OnItemSwitched(Entity<ItemSwitchComponent> ent, ref ItemSwitchedEvent args)
    {
        if (args.User == null
            || !TryComp<InventoryComponent>(args.User.Value, out var inventory)
            || !TryComp<ClothingComponent>(ent, out _))
            return;

        if (!_processing.Add(ent.Owner))
            return;

        try
        {
            var enumerator = new InventorySystem.InventorySlotEnumerator(inventory, SlotFlags.WITHOUT_POCKET);
            while (enumerator.NextItem(out var item, out var slotDef))
            {
                if (item != ent.Owner)
                    continue;

                var tracker = EnsureComp<ItemSwitchGrantTrackerComponent>(ent);

                // Clean up previously granted components using tracker
                if (tracker.Wearer != null)
                {
                    foreach (var name in tracker.GrantedComponents.ToList())
                    {
                        var type = Factory.GetRegistration(name).Type;
                        RemComp(tracker.Wearer.Value, type);
                    }
                    tracker.GrantedComponents.Clear();
                    tracker.Wearer = null;
                }

                // Grant new state's components and track what is granted
                if (TryComp<ClothingGrantComponentComponent>(ent, out var grant))
                {
                    // Temporarily record what is gonna be granted
                    foreach (var name in grant.Components.Keys)
                    {
                        var type = Factory.GetRegistration(name).Type;
                        if (!HasComp(args.User.Value, type))
                            tracker.GrantedComponents.Add(name);
                    }
                    tracker.Wearer = args.User.Value;
                }

                var equipEv = new GotEquippedEvent(args.User.Value, ent.Owner, slotDef);
                RaiseLocalEvent(ent.Owner, equipEv, true);
                break;
            }
        }
        finally
        {
            _processing.Remove(ent.Owner);
        }
    }
}
