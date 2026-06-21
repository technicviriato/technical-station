using Content.Medical.Shared.Wounds;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    /// <summary>
    /// Heals bone damage on a woundable, if it has a bone. Does nothing if it has no bone.
    /// </summary>
    public void HealBone(Entity<WoundableComponent> woundable, FixedPoint2 amount)
    {
        if (GetBone(woundable.AsNullable()) is not { } bone)
            return;

        ApplyDamageToBone(bone, -amount, bone.Comp);
    }
}