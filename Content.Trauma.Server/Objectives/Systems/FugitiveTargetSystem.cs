// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Trauma.Server.Objectives.Components;
using Content.Trauma.Server.StationEvents.Components;

namespace Content.Trauma.Server.Objectives.Systems;

/// <summary>
/// Sets the objective title to the fugitive's short identifier when assigned.
/// </summary>
public sealed class FugitiveTargetSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FugitiveTargetComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(EntityUid uid, FugitiveTargetComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(uid, out var targetMind))
            return;

        var query = EntityQueryEnumerator<FugitiveRuleComponent>();
        while (query.MoveNext(out _, out var rule))
        {
            foreach (var report in rule.Reports)
            {
                if (!TryComp<MindComponent>(targetMind.Value, out var mind) || mind.OwnedEntity == null)
                    continue;

                // Store identifier on the report indexed by mind entity
                _metaData.SetEntityName(uid, report.Identifier, args.Meta);
                return;
            }
        }
    }
}
