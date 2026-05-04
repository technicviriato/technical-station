using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

public sealed class FugitiveRule : StationEventSystem<FugitiveRuleComponent>
{
    private bool IsInitialized { get; set; }
    private TimeSpan _targetTime = TimeSpan.Zero;

    protected override void ActiveTick(EntityUid uid, FugitiveRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        Console.WriteLine("active tick");
        if (!IsInitialized)
        {
            _targetTime = Timing.CurTime.Add(new TimeSpan(0, 10, 0));
            IsInitialized = true;
        }


        if (Timing.CurTime >= _targetTime)
        {
            // 10 minutes have passed

        }
    }
}
