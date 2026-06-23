// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Strip.Events;

[Serializable, NetSerializable]
public sealed partial class BagAccessDoAfterEvent : DoAfterEvent
{
    public readonly string SlotName;
    public readonly NetEntity BagEntity;

    public BagAccessDoAfterEvent(string slotName, NetEntity bagEntity)
    {
        SlotName = slotName;
        BagEntity = bagEntity;
    }

    public override DoAfterEvent Clone() => this;
}
