// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Content.Trauma.Shared.Strip.Components;

namespace Content.Trauma.Shared.Strip;

/// <summary>
/// Enforces the free-hand strip limit and provides direct bag/storage access verbs.
/// Stripping someone requires a free hand per active doafter.
/// </summary>
public sealed partial class TraumaStrippingSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveStrippingComponent, DoAfterAttemptEvent<StrippableDoAfterEvent>>(OnStripAttempt);
        SubscribeLocalEvent<ActiveStrippingComponent, StrippableDoAfterEvent>(OnStripDoAfterFinished);
        SubscribeLocalEvent<HandsComponent, BeforeStripEvent>(OnBeforeStripEnsureComp);

        InitializeBagAccess();
    }

    private void OnBeforeStripEnsureComp(Entity<HandsComponent> user, ref BeforeStripEvent args)
    {
        EnsureComp<ActiveStrippingComponent>(user.Owner);
    }

    private void OnStripAttempt(Entity<ActiveStrippingComponent> user, ref DoAfterAttemptEvent<StrippableDoAfterEvent> args)
    {
        // Only limit removals, inserting items back doesn't require a free hand slot.
        if (args.Event.InsertOrRemove)
            return;

        if (!TryComp<HandsComponent>(user.Owner, out var hands))
            return;

        var freeHands = CountFreeHands((user.Owner, hands));

        if (!user.Comp.TrackedDoAfters.Contains(args.DoAfter.Index))
        {
            // Cannot remove jumpsuit while outer clothing is still worn.
            if (args.Event.InventoryOrHand
                && args.Event.SlotOrHandName == "jumpsuit"
                && args.DoAfter.Args.Target is { } target
                && _inventory.TryGetSlotEntity(target, "outerClothing", out _))
            {
                _popup.PopupEntity(
                    Loc.GetString("trauma-strip-jumpsuit-blocked"),
                    user.Owner,
                    user.Owner,
                    PopupType.SmallCaution);
                args.Cancel();
                return;
            }

            if (user.Comp.ActiveCount >= freeHands)
            {
                args.Cancel();
                return;
            }

            user.Comp.TrackedDoAfters.Add(args.DoAfter.Index);
            user.Comp.ActiveCount++;
            Dirty(user.Owner, user.Comp);
        }
        else
        {
            if (user.Comp.ActiveCount > freeHands)
                args.Cancel();
        }
    }

    private void OnStripDoAfterFinished(Entity<ActiveStrippingComponent> user, ref StrippableDoAfterEvent args)
    {
        if (args.InsertOrRemove)
            return;

        user.Comp.TrackedDoAfters.Remove(args.DoAfter.Index);
        DecrementActiveCount(user);
    }

    /// <summary>
    /// Returns the number of hands currently holding nothing.
    /// </summary>
    public int CountFreeHands(Entity<HandsComponent> ent)
    {
        var count = 0;
        foreach (var (name, _) in ent.Comp.Hands)
        {
            if (_handsSystem.GetHeldItem((ent.Owner, ent.Comp), name) == null)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Safely decrements the active strip count, floored at zero.
    /// </summary>
    public void DecrementActiveCount(Entity<ActiveStrippingComponent> ent)
    {
        if (ent.Comp.ActiveCount <= 0)
            return;

        ent.Comp.ActiveCount--;
        Dirty(ent.Owner, ent.Comp);
    }
}
