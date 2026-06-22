using System.Linq;
using Content.Shared.Emag.Systems;
using Content.Shared.Whitelist;

namespace Content.Shared.Salvage.Fulton;

public abstract partial class SharedFultonSystem
{
    private void OnEmagged(Entity<FultonComponent> ent, ref GotEmaggedEvent args)
    {
        ent.Comp.Whitelist ??= new EntityWhitelist();
        ent.Comp.Whitelist.Components = (ent.Comp.Whitelist.Components ?? Array.Empty<string>())
            .Union(new[] { "MindContainer" })
            .ToArray();
        Dirty(ent);
        args.Handled = true;
    }
}
