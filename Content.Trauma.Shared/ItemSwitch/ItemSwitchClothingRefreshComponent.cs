// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.ItemSwitch;

/// <summary>
/// Tracks components granted to the wearer by ItemSwitch states so they can be cleaned up on state change.
/// </summary>
[RegisterComponent]
public sealed partial class ItemSwitchGrantTrackerComponent : Component
{
    /// <summary>
    /// The wearer entity and the component type names that were granted to them.
    /// </summary>
    [DataField]
    public EntityUid? Wearer;

    [DataField]
    public HashSet<string> GrantedComponents = new();
}
