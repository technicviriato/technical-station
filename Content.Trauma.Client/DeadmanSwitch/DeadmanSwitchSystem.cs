using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Trauma.Shared.DeadmanSwitch;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.DeadmanSwitch;

public sealed partial class DeadmanSwitchSystem : SharedDeadmanSwitchSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeadmanSwitchComponent, UseInHandEvent>(OnUseInHand);
    }
    
    protected override void ToggleInHandFeedback(Entity<DeadmanSwitchComponent?> ent, EntityUid? user)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(ent, ref ent.Comp))
            return;

        if (user != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.Armed ? "deadman-on-activate" : "deadman-on-deactivate", ("name", ent)), ent, user.Value);

        _audio.PlayPvs(ent.Comp.SwitchSound, ent);
    }
}