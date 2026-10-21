using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.MirrorMirror.MirrorMirrorConstants;

namespace AnoMech.Scenarios.Fru.MirrorMirror;

public sealed class FruMirrorMirrorScenario : IScenario
{
    public string Name => "Mirror, Mirror";
    public bool SupportsMultiplayer => true;
    public float Duration => 37f;
    public IPhase Phase => FruZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new MirrorMirrorAi()];
    private SimWorld world = null!;
    private DamageSolver damage = null!;
    private MirrorMirrorPattern pattern = null!;
    private SimEnemy? boss, blue;
    private readonly SimEnemy?[] reds = new SimEnemy?[2];
    private readonly List<ISimObject> owned = [];
    private bool running;

    public void Run(SimWorld worldParam, int? selectedAi) => Run(worldParam, MirrorMirrorPattern.Random(FruUtils.Random(worldParam)));
    internal void Run(SimWorld worldParam, MirrorMirrorPattern chosen)
    {
        world = worldParam; damage = new(); pattern = chosen;
        owned.Clear();
        boss = blue = reds[0] = reds[1] = null;
        running = true;
        At(0.5f, () => {
            boss = Spawn(Usurper, Vector3.Zero, bossActor: true);
            if (boss == null) { Finish(); return; }
            boss.SetRotation(MathF.PI);
        });
        At(2, () => boss?.Cast(MirrorCast));
        At(MirrorsTime, () => {
            blue = SpawnMirror(pattern.Blue, BlueShow);
            for (var i = 0; i < 2; i++) reds[i] = SpawnMirror(pattern.Reds[i], RedShow);
        });
        At(BossFreeTime, () => FollowTank(humanOnly: true));
        At(BotDragTime, () => { if (!HumanTank()) boss?.MoveTo(pattern.BossSpot, RunSpeed); });
        At(KickTime - 6, () => { boss?.Follow(); boss?.Cast(ScytheKick); });
        At(KickTime - 6, () => owned.Add(world.SpawnVoiceLine(DiamondDust.DiamondDustConstants.ScytheVoice)));
        At(KickTime, () => {
            Kick(boss, ScytheKick, "Scythe Kick");
            foreach (var red in pattern.Reds) world.Map.AddEffect(RedArm, (byte)(red + 1));
        });
        At(KickTime + 0.1f, () => {
            blue?.Cast(ReflectedKickInstant, castSeconds: 0);
            Kick(blue, ReflectedKickInstant, "blue mirror Reflected Scythe Kick");
            foreach (var red in reds) red?.Cast(ReflectedKick);
        });
        At(BlueHouseTime, () => Proteans(blue));
        At(KickTime + 0.1f, () => FollowTank(humanOnly: false));
        At(BossHouseTime, () => Proteans(boss));
        At(RedKickTime, () => { foreach (var red in reds) Kick(red, ReflectedKick, "red mirror Reflected Scythe Kick"); });
        At(RedHouseTime, () => { foreach (var red in reds) Proteans(red); });
        At(BanishCastTime, () => { boss?.Follow(); boss?.Cast(pattern.Pairs ? BanishPairs : BanishSpread); });
        At(BanishTime, Banish);
        At(Duration, Finish);
        ((MirrorMirrorAi)AiStrats[0]).Run(pattern, world, () => running && boss is { IsActive: true },
            () => boss?.Position ?? pattern.BossSpot);
    }

    // A real main tank drags Shiva into place for the kick; a bot tank gets her walked to the scripted spot.
    private bool HumanTank() => world.Party.Get(PartyRole.MainTank) is { } tank && !world.Party.IsBotDriven(tank);
    private void FollowTank(bool humanOnly)
    {
        if (world.Party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive() || (humanOnly && !HumanTank())) return;
        boss?.SetTarget(tank, follow: true);
    }
    private void At(float time, Action action) => world.Events.Add(time, () => { if (running) action(); });
    private SimCharacter[] Members() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToArray();
    private void Hit(SimCharacter member, uint action, string cause, bool lethal)
    { if (member.IsAlive()) damage.ApplyDamage(member, lethal ? 1 : 0.35f, action, cause, lethal); }
    private SimEnemy? Spawn(uint id, Vector3 position, bool bossActor = false)
    {
        var actor = world.SpawnEnemy(new EnemySpawnConfig(id,
            NameId: bossActor ? UsurperName : id == FrozenMirror ? FrozenMirrorName : 0, Level: FruConstants.Level,
            Targetable: bossActor, EnemyList: bossActor ? EnemyListMode.OnlyWhenVisible : EnemyListMode.Never,
            Visibility: id == FrozenMirror ? SpawnVisibility.InvisibleHelper : FruUtils.SpawnVisibilityOf(id), Placement: new(position, 0), ModelCharaId: bossActor ? LightModel : 0));
        if (actor != null) owned.Add(actor);
        return actor;
    }
    private SimEnemy? SpawnMirror(int octant, uint show)
    {
        owned.Add(world.SpawnMapEffect((byte)(octant + 1), show, MirrorHide));
        var mirror = Spawn(FrozenMirror, MirrorMirrorPattern.Point(octant, 20));
        mirror?.Face(Vector3.Zero);
        return mirror;
    }
    private void Effect(uint action, Vector3 position, SimCharacter target, float rotation)
    {
        var helper = Spawn(FruConstants.BNpcBaseId.Helper, position);
        helper?.SetRotation(rotation);
        helper?.Cast(action, targetLocation: position, target: target, castSeconds: 0);
        if (helper != null) world.Events.Add(3, helper.Despawn);
    }
    private void Kick(SimEnemy? source, uint action, string cause)
    {
        if (source == null) return;
        foreach (var member in Members())
        {
            var distance = Vector3.Distance(member.Position, source.Position);
            if (distance >= KickInner && distance <= KickOuter) Hit(member, action, cause, true);
        }
    }
    private void Proteans(SimEnemy? source)
    {
        if (source == null) return;
        var origin = source.Position;
        var members = Members();
        foreach (var target in members.OrderBy(m => Vector3.DistanceSquared(m.Position, origin)).Take(4))
        {
            var offset = target.Position - origin;
            var direction = offset.LengthSquared() > 0.001f ? Vector3.Normalize(offset) : Vector3.UnitZ;
            Effect(House, origin, target, MathF.Atan2(direction.X, direction.Z));
            foreach (var member in members)
            {
                var toMember = member.Position - origin;
                if (toMember.LengthSquared() < 0.001f || Vector3.Dot(Vector3.Normalize(toMember), direction) >= MathF.Cos(MathF.PI / 12))
                    Hit(member, House, "House of Light cone bait / overlap", member != target);
            }
        }
    }
    private void Banish()
    {
        var members = Members();
        // UNVERIFIED for this mechanic: pair targets follow Light Rampant's Banish (all supports or all DPS).
        var targets = pattern.Pairs
            ? MirrorMirrorPattern.Roles.Where(r => r.IsDps() != pattern.SupportPairs).Select(r => world.Party.Get(r))
                .Where(t => t.IsAlive()).Select(t => t!).ToArray()
            : members;
        var centers = targets.Select(t => t.Position).ToArray();
        var action = pattern.Pairs ? BanishPairsHit : BanishSpreadHit;
        for (var i = 0; i < targets.Length; i++)
        {
            Effect(action, centers[i], targets[i], 0);
            var group = members.Where(m => Vector3.Distance(m.Position, centers[i]) <= BanishRadius).ToArray();
            if (group.Length != (pattern.Pairs ? 2 : 1))
                foreach (var member in group) Hit(member, action, pattern.Pairs ? "Banish III: pair up" : "Banish III Divided: spread", true);
        }
        foreach (var member in members)
        {
            var covering = centers.Count(c => Vector3.Distance(member.Position, c) <= BanishRadius);
            if (covering != 1) Hit(member, action, covering == 0 ? "Banish III: no partner" : "Banish III overlap", true);
            else Hit(member, action, "Banish III", false);
        }
    }
    public void Tick(float delta, float elapsed)
    {
        if (!running || boss is not { IsActive: true }) return;
        foreach (var member in Members())
            if (member.Position.Length() > FruConstants.Geometry.ArenaRadius) Hit(member, ScytheKick, "arena boundary", true);
    }
    private void Finish()
    {
        running = false;
        foreach (var role in MirrorMirrorPattern.Roles)
            if (world.Party.Get(role) is { } bot && world.Party.IsBotDriven(bot)) bot.StopMoving();
        foreach (var obj in owned) obj.Despawn();
    }
}
