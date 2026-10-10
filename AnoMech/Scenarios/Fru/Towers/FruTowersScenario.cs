using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.Towers.TowersConstants;

namespace AnoMech.Scenarios.Fru.Towers;

public sealed class FruTowersScenario : IScenario
{
    public string Name => "Towers";
    public bool SupportsMultiplayer => true;
    public float Duration => Length;
    public IPhase Phase => FruZone.P1;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new TowersAi()];

    private SimWorld world = null!;
    private DamageSolver damage = null!;
    private TowersPattern pattern = null!;
    private SimEnemy? boss;
    private readonly List<ISimObject> owned = [];
    private bool running;

    public void Run(SimWorld worldParam, int? selectedAi) => Run(worldParam, TowersPattern.Random(FruUtils.Random(worldParam)));

    internal void Run(SimWorld worldParam, TowersPattern chosen)
    {
        world = worldParam; damage = new(); pattern = chosen;
        owned.Clear(); boss = null; running = true;

        At(0.5f, SpawnBoss);
        At(PowderCast, () => boss?.Cast(PowderMarkTrail, target: world.Party.Get(PartyRole.MainTank), castSeconds: 4.7f));
        At(PowderHit, ApplyPowderMark);
        At(Strike, StartStrike);
        At(StrikeHit, () => Line(StrikeHalfWidth, pattern.Fire ? BurntStrikeFire : BurntStrikeLightning, "Burnt Strike"));
        if (pattern.Fire) At(BlastburnHit, ResolveBlastburn);
        else At(BurnoutHit, () => Line(BurnoutHalfWidth, Burnout, "Burnout"));
        At(TowerHit, ResolveTowers);
        At(BurnMarkHit, ResolveBurnMark);
        At(Duration, Finish);

        ((TowersAi)AiStrats[0]).Run(pattern, world, () => running);
    }

    private void At(float time, Action action) => world.Events.Add(time, () => { if (running) action(); });
    private SimCharacter[] Members() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToArray();

    private void SpawnBoss()
    {
        boss = world.SpawnEnemy(new EnemySpawnConfig(Fatebreaker, NameId: FatebreakerName, Level: FruConstants.Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Placement: new(Vector3.Zero, MathF.PI)));
        if (boss == null) { Finish(); return; }
        owned.Add(boss);
        boss.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false);
    }

    private void ApplyPowderMark()
    {
        if (world.Party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        Hit(tank, PowderMarkTrail, "Powder Mark Trail", false);
        tank.AddStatus(PowderMarkStatus, PowderDuration);
        tank.AddStatus(PhysicalVulnerability, 14);
    }

    private void StartStrike()
    {
        // Fatebreaker snaps north for the strike: Burnt Strike and Burnout always resolve as a
        // north-south line through the centre.
        boss?.SetRotation(MathF.PI);
        boss?.Cast(pattern.Fire ? BurntStrikeFire : BurntStrikeLightning, castSeconds: 6.2f);
        if (!pattern.Fire) Effect(Burnout, Vector3.Zero, MathF.PI, 7.9f);
        var towers = pattern.Fire ? FireTower : LightningTower;
        for (var i = 0; i < 3; i++) Effect(towers[pattern.Counts[i]], pattern.Tower(i), 0, 10.2f);
    }

    private void Line(float halfWidth, uint action, string cause)
    {
        foreach (var m in Members())
            if (MathF.Abs(m.Position.X) < halfWidth) Hit(m, action, cause, true);
    }

    private void ResolveBlastburn()
    {
        boss?.SetRotation(MathF.PI);
        boss?.Cast(Blastburn, castSeconds: 0);
        foreach (var m in Members())
        {
            var side = m.Position.X >= 0 ? 1 : -1;
            var source = new Vector3(m.Position.X - side, 0, m.Position.Z);
            (m as ISimPartyMember)?.Knockback(source, KnockbackDistance, KnockbackSpeed);
        }
    }

    private void ResolveTowers()
    {
        var members = Members();
        var towers = pattern.Fire ? FireTower : LightningTower;
        for (var i = 0; i < 3; i++)
        {
            var center = pattern.Tower(i);
            var inside = members.Where(m => Flat(m.Position - center) <= TowerRadius).ToArray();
            if (inside.Length < pattern.Counts[i])
            {
                Effect(UnmitigatedExplosion, center, 0, 0);
                foreach (var m in members) Hit(m, UnmitigatedExplosion, $"Unmitigated Explosion ({inside.Length}/{pattern.Counts[i]} in tower)", true);
                return;
            }
            foreach (var m in inside) Hit(m, towers[pattern.Counts[i]], "Explosion", false);
        }
    }

    // The marked tank and whoever stands nearest them both explode.
    private void ResolveBurnMark()
    {
        var members = Members();
        if (world.Party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        tank.RemoveStatus(PowderMarkStatus);
        var nearest = members.Where(m => m != tank).OrderBy(m => Flat(m.Position - tank.Position)).FirstOrDefault();
        var exploding = nearest == null ? new[] { tank } : new[] { tank, nearest };
        foreach (var source in exploding)
        {
            Effect(BurnMark, source.Position, 0, 0, source);
            foreach (var m in members)
                if (m != source && Flat(m.Position - source.Position) <= BurnMarkRadius)
                    Hit(m, BurnMark, "Burn Mark", true);
        }
    }

    private void Hit(SimCharacter member, uint action, string cause, bool lethal)
    { if (member.IsAlive()) damage.ApplyDamage(member, lethal ? 1 : 0.35f, action, cause, lethal); }

    private static float Flat(Vector3 v) => new Vector2(v.X, v.Z).Length();

    private void Effect(uint action, Vector3 position, float rotation, float castSeconds, SimCharacter? target = null)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(FruConstants.BNpcBaseId.Helper, Level: FruConstants.Level,
            EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper, Placement: new(position, rotation)));
        if (helper == null) return;
        owned.Add(helper);
        helper.SetRotation(rotation);
        helper.Cast(action, target: target, castSeconds: castSeconds);
        world.Events.Add(castSeconds + 3, helper.Despawn);
    }

    private void Finish()
    {
        running = false;
        foreach (var role in TowersPattern.Roles)
            if (world.Party.Get(role) is { } member)
            {
                member.RemoveStatus(PowderMarkStatus); member.RemoveStatus(PhysicalVulnerability);
                if (member is SimPartyNpc bot) bot.StopMoving();
            }
        foreach (var obj in owned) obj.Despawn();
        owned.Clear();
    }
}
