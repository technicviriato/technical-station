using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

public abstract partial class SharedStoreSystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> DebugUplinkTag = "DebugUplink";
}