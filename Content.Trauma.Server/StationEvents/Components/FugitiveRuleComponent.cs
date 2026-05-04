// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.StationEvents.Events;
using Content.Shared.Dataset;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Server.StationEvents.Components;

/// <summary>
/// Makes a SpacePol announcement and creates a report some time after an antag spawns.
/// Removed after this is done.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class FugitiveRuleComponent : Component
{
    [DataField]
    public LocId Announcement = "station-event-fugitive-hunt-announcement";

    [DataField]
    public LocId Sender = "fugitive-announcement-SpacePol";

    [DataField]
    public Color Color = Color.Blue;

    /// <summary>
    /// Report paper to spawn. Its content is generated from the fugitive.
    /// </summary>
    [DataField]
    public EntProtoId ReportPaper = "PaperFugitiveReport";

    /// <summary>
    /// How long to wait after the antag spawns before announcing it.
    /// </summary>
    [DataField]
    public TimeSpan AnnounceDelay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Station to give the report to.
    /// </summary>
    [DataField]
    public EntityUid? Station;

    /// <summary>
    /// The report generated for the spawned fugitive.
    /// </summary>
    [DataField]
    public string Report = string.Empty;

    /// <summary>
    /// When the announcement will be made, if an antag has spawned yet.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextAnnounce;

    /// <summary>
    /// Dataset to pick crimes on the report from.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CrimeDataset = "FugitiveCrimes";

    /// <summary>
    /// Max number of unique crimes they can be charged with.
    /// Does not affect the counts of each crime.
    /// </summary>
    [DataField]
    public int MinCrimes = 4;

    /// <summary>
    /// Min number of unique crimes they can be charged with.
    /// Does not affect the counts of each crime.
    /// </summary>
    [DataField]
    public int MaxCrimes = 8;

    /// <summary>
    /// Min counts of each crime that can be rolled.
    /// </summary>
    [DataField]
    public int MinCounts = 1;

    /// <summary>
    /// Max counts of each crime that can be rolled.
    /// </summary>
    [DataField]
    public int MaxCounts = 4;
}
