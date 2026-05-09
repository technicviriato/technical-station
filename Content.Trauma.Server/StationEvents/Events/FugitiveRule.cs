// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Server.Communications;
using Content.Server.StationEvents.Events;
using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Trauma.Server.StationEvents.Components;
using Robust.Shared.Utility;

namespace Content.Trauma.Server.StationEvents.Events;

public sealed class FugitiveRule : StationEventSystem<FugitiveRuleComponent>
{
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FugitiveRuleComponent, AfterAntagEntitySelectedEvent>(OnEntitySelected);
    }
    /// <summary>
    /// Sends the report to every comms console on the station, and prevents any possible funnies
    /// </summary>
    protected override void ActiveTick(EntityUid uid, FugitiveRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (component.NextAnnounce is not { } next || next > Timing.CurTime)
            return;

        var announcement = Loc.GetString(component.Announcement);
        var sender = Loc.GetString(component.Sender);
        ChatSystem.DispatchGlobalAnnouncement(announcement, sender: sender, colorOverride: component.Color);

        var query = EntityQueryEnumerator<CommunicationsConsoleComponent, TransformComponent>();
        var consoles = new List<TransformComponent>();
        while (query.MoveNext(out var console, out _, out var xform))
        {
            if (StationSystem.GetOwningStation(console, xform) != component.Station || HasComp<GhostComponent>(console))
                continue;

            consoles.Add(xform);
        }

        foreach (var xform in consoles)
        {
            SpawnReport(component, xform);
        }

        component.NextAnnounce = null;

        RemCompDeferred(uid, component);
    }
     /// <summary>
     /// Called when the fugitive is selected.
     /// Generates the report, schedules the station announcement, and gives the fugitive a report.
     /// </summary>
     private void OnEntitySelected(Entity<FugitiveRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
     {
        var (uid, comp) = ent;
        if (comp.NextAnnounce != null)
        {
            Log.Error("Fugitive rule spawning multiple fugitives isn't supported, sorry.");
            return;
        }
        var fugi = args.EntityUid;
        comp.Report = GenerateReport(fugi, comp).ToMarkup();
        comp.Station = StationSystem.GetOwningStation(fugi);
        comp.NextAnnounce = Timing.CurTime + comp.AnnounceDelay;

        _popup.PopupEntity(Loc.GetString("fugitive-spawn"), fugi, fugi);

        var report = SpawnReport(comp, Transform(fugi));

        _hands.TryPickupAnyHand(fugi, report);

     }
     /// <summary>
     /// Spawns the fugitive report at a given location
     /// </summary>
     private Entity<PaperComponent> SpawnReport(FugitiveRuleComponent rule, TransformComponent xform)
     {
         var report = Spawn(rule.ReportPaper, xform.Coordinates);
         var paper = Comp<PaperComponent>(report);
         var ent = (report, paper);
         _paper.SetContent(ent, rule.Report);
         return ent;
    }
    /// <summary>
    /// Adds the content to the fugitive report: crime list, species, age, etc
    /// Adds a random identifying quality that officers can use to track them down
    /// Generates some random crimes
    /// </summary>
    private FormattedMessage GenerateReport(EntityUid uid, FugitiveRuleComponent rule)
    {
        var report = new FormattedMessage();
        report.AddMarkupOrThrow(Loc.GetString("fugitive-report-title"));
        report.PushNewline();
        report.AddMarkupOrThrow(Loc.GetString("fugitive-report-first-line"));
        report.PushNewline();

        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
        {
            report.AddMarkupOrThrow(Loc.GetString("fugitive-report-inhuman", ("name", uid)));
            return report;
        }

        var species = PrototypeManager.Index(humanoid.Species);

        report.AddMarkupOrThrow(Loc.GetString("fugitive-report-species", ("species", Loc.GetString(species.Name))));
        report.PushNewline();
        report.AddMarkupOrThrow(Loc.GetString("fugitive-report-age", ("age", humanoid.Age)));
        report.PushNewline();
        report.AddMarkupOrThrow(Loc.GetString("fugitive-report-sex", ("sex", humanoid.Sex.ToString())));
        report.PushNewline();

        report.AddMarkupOrThrow(RobustRandom.Next(0, 2) switch
        {
            0 => Loc.GetString("fugitive-report-detail-dna", ("dna", GetDNA(uid))),
            _ => Loc.GetString("fugitive-report-detail-prints", ("prints", GetPrints(uid)))
        });
        report.PushNewline();
        report.PushNewline();
        report.AddMarkupOrThrow(Loc.GetString("fugitive-report-crimes-header"));

        AddCharges(report, rule);

        report.PushNewline();
        report.AddMarkupOrThrow(Loc.GetString("fugitive-report-last-line"));

        return report;
    }
    /// <summary>
    /// DNA string of fugitive, or "?" if unavailable somehow
    /// </summary>
    private string GetDNA(EntityUid uid)
    {
        return CompOrNull<DnaComponent>(uid)?.DNA ?? "?";
    }
    /// <summary>
    /// Fingerprints of fugitive, or "?" if unavailable somehow
    /// </summary>
    private string GetPrints(EntityUid uid)
    {
        return CompOrNull<FingerprintComponent>(uid)?.Fingerprint ?? "?";
    }

    /// <summary>
    /// Picks a random set of unique crimes from the dataset and adds them to the report, each with a random count(within the range)
    /// </summary>
    private void AddCharges(FormattedMessage report, FugitiveRuleComponent rule)
    {
        var crimeTypes = PrototypeManager.Index(rule.CrimeDataset);
        var crimes = new HashSet<LocId>();
        var total = RobustRandom.Next(rule.MinCrimes, rule.MaxCrimes + 1);
        while (crimes.Count < total)
        {
            crimes.Add(RobustRandom.Pick(crimeTypes));
        }

        foreach (var crime in crimes)
        {
            var count = RobustRandom.Next(rule.MinCounts, rule.MaxCounts + 1);
            report.AddMarkupOrThrow(Loc.GetString("fugitive-report-crime", ("crime", Loc.GetString(crime)), ("count", count)));
            report.PushNewline();
        }
    }
}
