namespace Content.Shared.Tools.Components;

public sealed partial class ToolOpenableComponent
{
    /// <summary>
    ///     If false, will not show examine when examined
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowExamine;
}
