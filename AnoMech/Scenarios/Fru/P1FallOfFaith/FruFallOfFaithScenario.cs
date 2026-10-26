using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P1FallOfFaith.FallOfFaithConstants;

namespace AnoMech.Scenarios.Fru.P1FallOfFaith;

public sealed class FruFallOfFaithScenario : IScenario
{
    public string Name => "Fall of Faith";
    public bool SupportsMultiplayer => true;
    public float Duration => Start + Length;
    public IPhase Phase => FruZone.P1;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } =
    [
        new FallOfFaithAi("NA · Light Party Conga", FallOfFaithPattern.LightPartyConga),
    ];

    private static readonly Vector3[] ImageSpots = [Vector3.Zero, new(-14.14f, 0, -14.14f), new(0, 0, -20), new(14.14f, 0, -14.14f)];

    private SimWorld world = null!;
    private FallOfFaithPattern pattern = null!;
    private readonly SimEnemy?[] casters = new SimEnemy?[4];
    private readonly SimTether?[] tethers = new SimTether?[4];
    private readonly List<ISimObject> owned = [];
    private readonly Dictionary<SimCharacter, float> vulnerableUntil = [];
    private readonly List<(SimEnemy Actor, Vector3 From, Vector3 To, float StartedAt)> charges = [];
    private bool running;

    public void Run(SimWorld worldParam, int? selectedAi) => Run(worldParam, FallOfFaithPattern.Random(FruUtils.Random(worldParam)), selectedAi ?? 0);

    internal void Run(SimWorld worldParam, FallOfFaithPattern chosen, int strat)
    {
        world = worldParam; pattern = chosen;
        owned.Clear(); vulnerableUntil.Clear(); charges.Clear();
        Array.Clear(casters); Array.Clear(tethers);
        running = true;

        At(0.5f, SpawnActors);
        for (var i = 0; i < 4; i++)
        {
            var tether = i;
            if (tether > 0) At(Start + ImageShowAt[tether], () => casters[tether]?.PlayActionTimeline(ActionTimelineId.WarpEnd));
            At(Start + TetherAt[tether], () => Tether(tether));
            At(Start + CastAt[tether], () => casters[tether]?.Cast(pattern.Fire[tether] ? ActionId.FallOfFaithFire : ActionId.FallOfFaithLightning, castSeconds: CastTime));
            At(Start + FettersAt[tether], () => Fetter(tether));
            At(Start + ChargeAt[tether], () => Charge(tether));
            At(Start + HitAt[tether], () => Resolve(tether));
            if (!pattern.Fire[tether]) At(Start + HitAt[tether] + BowShockDelay, () => BowShocks(tether));
            if (tether > 0) At(Start + ChargeAt[tether] + 1.8f, () => casters[tether]?.Despawn());
        }
        At(Duration, Finish);

        var ai = (FallOfFaithAi)AiStrats[Math.Clamp(strat, 0, AiStrats.Count - 1)];
        ai.Run(pattern, world, () => running);
    }

    private void At(float time, Action action) => world.Events.Add(time, () => { if (running) action(); });
    private SimCharacter[] Members() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToArray();

    private void SpawnActors()
    {
        for (var i = 0; i < 4; i++)
        {
            var boss = i == 0;
            var spot = ImageSpots[i];
            var facing = boss ? MathF.PI : MathF.Atan2(-spot.X, -spot.Z);
            var actor = world.SpawnEnemy(new EnemySpawnConfig(boss ? BNpcBaseId.Fatebreaker : BNpcBaseId.FatebreakerImage,
                NameId: boss ? BNpcNameId.Fatebreaker : BNpcNameId.FatebreakerImage,
                Level: Level, Targetable: boss, EnemyList: boss ? EnemyListMode.Always : EnemyListMode.Never,
                Visibility: boss ? SpawnVisibility.Visible : SpawnVisibility.HiddenUntilShown,
                Placement: new(spot, facing)));
            if (actor == null) { Finish(); return; }
            owned.Add(actor);
            casters[i] = actor;
        }
        casters[0]!.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false);
    }

    private void Tether(int index)
    {
        var target = world.Party.Get(pattern.Tethered[index]);
        if (!target.IsAlive()) return;
        tethers[index] = world.Tether(casters[index], target, pattern.Fire[index] ? TetherId.FallOfFaithFire : TetherId.FallOfFaithLightning);
        owned.Add(tethers[index]!);
    }

    private void Fetter(int index)
    {
        if (world.Party.Get(pattern.Tethered[index]) is not { } target || !target.IsAlive()) return;
        target.StopMoving();
        target.AddStatus(StatusId.FloatingFetters, FettersTime);
    }

    private void Charge(int index)
    {
        tethers[index]?.Despawn();
        tethers[index] = null;
        var caster = casters[index];
        var target = world.Party.Get(pattern.Tethered[index]);
        if (caster == null || !target.IsAlive()) return;
        caster.Cast(pattern.Fire[index] ? ActionId.FallOfFaithChargeFire : ActionId.FallOfFaithChargeLightning, target: target, castSeconds: 0);
        var offset = target!.Position - caster.Position;
        var to = offset.Length() <= ChargeStop ? caster.Position : target.Position - Vector3.Normalize(offset) * ChargeStop;
        caster.SetRotation(MathF.Atan2(offset.X, offset.Z));
        charges.Add((caster, caster.Position, to, world.Events.Elapsed));
    }

    private void Resolve(int index)
    {
        var target = world.Party.Get(pattern.Tethered[index]);
        if (!target.IsAlive()) return;
        var apex = target!.Position;

        if (!pattern.Fire[index])
        {
            Effect(ActionId.Sinsmite, apex, 0, target);
            Hit(target, ActionId.Sinsmite);
            return;
        }

        var outward = new Vector3(apex.X, 0, apex.Z);
        var direction = outward.LengthSquared() > 0.01f ? Vector3.Normalize(outward) : new Vector3(FallOfFaithPattern.Side(index), 0, 0);
        Effect(ActionId.Sinblaze, apex, MathF.Atan2(direction.X, direction.Z));
        var stack = Members().Where(m => m == target || InsideCone(m.Position, apex, direction)).ToArray();
        // Three players survived a Sinblaze in the logs; two did not.
        if (stack.Length < 3)
        {
            foreach (var m in stack) Hit(m, ActionId.Sinblaze, $"{stack.Length}/4 in stack", lethal: true);
            return;
        }
        foreach (var m in stack) Hit(m, ActionId.Sinblaze);
    }

    private void BowShocks(int index)
    {
        var target = world.Party.Get(pattern.Tethered[index]);
        if (!target.IsAlive()) return;
        var apex = target!.Position;
        var members = Members();
        var baits = members.Where(m => m != target).OrderBy(m => Vector3.DistanceSquared(m.Position, apex)).Take(3).ToArray();
        var hits = new Dictionary<SimCharacter, int>();
        foreach (var bait in baits)
        {
            var offset = bait.Position - apex;
            var direction = offset.LengthSquared() > 0.0001f ? Vector3.Normalize(offset) : Vector3.UnitZ;
            Effect(ActionId.BowShock, apex, MathF.Atan2(direction.X, direction.Z));
            foreach (var m in members.Where(m => m != target && InsideCone(m.Position, apex, direction)))
                hits[m] = hits.GetValueOrDefault(m) + 1;
        }
        foreach (var (m, count) in hits) Hit(m, ActionId.BowShock, count > 1 ? "hit by two cones" : null, lethal: count > 1);
    }

    // Every Fall of Faith hit leaves Magic Vulnerability Up; a second hit inside it kills.
    private void Hit(SimCharacter member, uint action, string? cause = null, bool lethal = false)
    {
        if (!member.IsAlive()) return;
        var now = world.Events.Elapsed;
        var vulnerable = vulnerableUntil.TryGetValue(member, out var until) && now < until;
        if (lethal || vulnerable)
        {
            member.Die(action, vulnerable ? "Magic Vulnerability Up" : cause);
            return;
        }
        vulnerableUntil[member] = now + VulnerabilityTime;
        member.AddStatus(StatusId.MagicVulnerabilityUp, VulnerabilityTime);
    }

    private static bool InsideCone(Vector3 position, Vector3 apex, Vector3 direction)
    {
        var offset = position - apex;
        offset.Y = 0;
        if (offset.LengthSquared() < 0.0001f) return true;
        return offset.LengthSquared() <= 3600 && Vector3.Dot(Vector3.Normalize(offset), direction) >= MathF.Cos(ConeHalfAngle);
    }

    private void Effect(uint action, Vector3 position, float rotation, SimCharacter? target = null)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId.Helper, Level: Level,
            EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper, Placement: new(position, rotation)));
        if (helper == null) return;
        owned.Add(helper);
        // UNVERIFIED: delivered to the tethered player, the cone was drawn along that player's
        // facing (inward for tethers 3/4, who step in to resolve); delivered to no one, it
        // should follow the helper's facing set here.
        helper.SetRotation(rotation);
        helper.Cast(action, target: target, castSeconds: 0);
        world.Events.Add(3, helper.Despawn);
    }

    public void Tick(float delta, float elapsed)
    {
        if (!running) return;
        var time = world.Events.Elapsed;
        for (var i = charges.Count - 1; i >= 0; i--)
        {
            var (actor, from, to, startedAt) = charges[i];
            var t = Math.Clamp((time - startedAt) / 0.5f, 0, 1);
            actor.SetPosition(Vector3.Lerp(from, to, t * t * (3 - 2 * t)));
            if (t >= 1) charges.RemoveAt(i);
        }
    }

    private void Finish()
    {
        running = false; charges.Clear(); vulnerableUntil.Clear();
        foreach (var role in FallOfFaithPattern.Roles)
            if (world.Party.Get(role) is { } member)
            {
                member.RemoveStatus(StatusId.FloatingFetters); member.RemoveStatus(StatusId.MagicVulnerabilityUp);
                if (world.Party.IsBotDriven(member)) member.StopMoving();
            }
        foreach (var obj in owned) obj.Despawn();
        owned.Clear();
    }
}
