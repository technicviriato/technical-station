// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Trauma.Shared.DeadmanSwitch;

namespace Content.Trauma.Server.DeadmanSwitch;

public sealed partial class DeadmanSwitchSystem : SharedDeadmanSwitchSystem
{
    [Dependency] private SharedDeviceLinkSystem _device = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeadmanSwitchComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SignallerSystem)]);
    }

    public override void Trigger(Entity<DeadmanSwitchComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var signallerQuery = GetEntityQuery<SignallerComponent>();
        if (!signallerQuery.TryGetComponent(ent, out var signaller))
            return;

        var linkedDevices = _device.GetLinkedSinks(ent.Owner, signaller.Port);

        var timerQuery = GetEntityQuery<TimerTriggerComponent>();
        var signalQuery = GetEntityQuery<TriggerOnSignalComponent>();

        foreach (var linkedUid in linkedDevices)
        {
            if (!_transform.InRange(ent.Owner, linkedUid, ent.Comp.InstantTriggerRange))
                continue;

            if (!timerQuery.TryGetComponent(linkedUid, out var timerTrigger))
                continue;

            if (!signalQuery.TryGetComponent(linkedUid, out var signalTrigger))
                continue;

            if (signalTrigger.KeyOut == null || !timerTrigger.KeysIn.Contains(signalTrigger.KeyOut))
                continue;

            _trigger.Trigger(linkedUid, user: user, timerTrigger.KeyOut);
            timerTrigger.Disabled = true;

            if (user != null)
                _adminLogger.Add(LogType.Trigger,
                    $"{user} instant-triggered {ToPrettyString(linkedUid):target} with {ToPrettyString(ent):device}");
        }

        _device.InvokePort(ent.Owner, signaller.Port);
    }
}
