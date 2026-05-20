using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Content.Trauma.Shared.DeadmanSwitch;

namespace Content.Trauma.Server.DeadmanSwitch;

public sealed partial class DeadmanSwitchSystem : SharedDeadmanSwitchSystem
{
    [Dependency] private SignallerSystem _signal = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeadmanSwitchComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SignallerSystem)]);
    }

    public override void Trigger(Entity<DeadmanSwitchComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<SignallerComponent>(ent, out var signaller))
            return;

        var linkedDevices = _deviceLink.GetLinkedSinks(ent.Owner, signaller.Port);

        if (linkedDevices.Count > 0)
        {
            var switchXform = Transform(ent);
            var switchPos = _transformSystem.GetWorldPosition(switchXform);

            foreach (var linkedUid in linkedDevices)
            {
                var linkXform = Transform(linkedUid);
                var linkPos = _transformSystem.GetWorldPosition(linkXform);
                if (switchXform.MapID != linkXform.MapID || (switchPos - linkPos).Length() > ent.Comp.InstantTriggerRange)
                    continue;

                if (!TryComp<TimerTriggerComponent>(linkedUid, out var timerTrigger))
                    continue;

                if (!TryComp<TriggerOnSignalComponent>(linkedUid, out var signalTrigger))
                    continue;

                if (signalTrigger.KeyOut == null || !timerTrigger.KeysIn.Contains(signalTrigger.KeyOut))
                    continue;

                // Manually call the trigger that would fire when the timer completes
                _trigger.Trigger(linkedUid, user: user, timerTrigger.KeyOut);
                // Block the _signal.Trigger event from starting a new countdown
                timerTrigger.Disabled = true;

                if (user != null)
                    _adminLogger.Add(LogType.Trigger,
                        $"{user} instant-triggered {ToPrettyString(linkedUid):target} with {ToPrettyString(ent):device}");
            }
        }

        // Always invoke port regardless
        _deviceLink.InvokePort(ent.Owner, signaller.Port);

        // Popup and sound
        if (user != null)
            _popup.PopupEntity(Loc.GetString("deadman-on-trigger", ("name", ent)), ent, user.Value);

        _audio.PlayPvs(ent.Comp.SwitchSound, ent);
    }
}