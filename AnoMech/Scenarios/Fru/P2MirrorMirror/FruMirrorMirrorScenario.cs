using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P2MirrorMirror.MirrorMirrorConstants;

namespace AnoMech.Scenarios.Fru.P2MirrorMirror;

public sealed class FruMirrorMirrorScenario : IScenario
{
    public string Name => "Mirror, Mirror";
    public bool SupportsMultiplayer => true;
    public float Duration => 37f;
    public IPhase Phase => FruZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new MirrorMirrorAi()];
    private SimWorld world = null!;
    private MirrorMirrorPattern pattern = null!;
    private SimEnemy? boss, blue;
    private readonly SimEnemy?[] reds = new SimEnemy?[2];
    private readonly List<ISimObject> owned = [];
    private readonly List<byte> shownMirrors = [];
    private bool running;

    public void Run(SimWorld worldParam, int? selectedAi) => Run(worldParam, MirrorMirrorPattern.Random(FruUtils.Random(worldParam)));
    internal void Run(SimWorld worldParam, MirrorMirrorPattern chosen)
    {
        world = worldParam; pattern = chosen;
        owned.Clear(); shownMirrors.Clear();
        boss = blue = reds[0] = reds[1] = null;
        running = true;
        At(0.5f, () => {
            boss = Spawn(BNpcBaseId.UsurperP2, Vector3.Zero, bossActor: true);
            if (boss == null) { Finish(); return; }
            boss.SetRotation(MathF.PI);
        });
        At(2, () => boss?.Cast(ActionId.MirrorMirror));
        At(MirrorsTime, () => {
            blue = SpawnMirror(pattern.Blue, MapEffect.BlueMirrorShow);
            for (var i = 0; i < 2; i++) reds[i] = SpawnMirror(pattern.Reds[i], MapEffect.RedMirrorShow);
        });
        At(BossFreeTime, () => FollowTank(humanOnly: true));
        At(BotDragTime, () => { if (!HumanTank()) boss?.MoveTo(pattern.BossSpot, RunSpeed); });
        At(KickTime - 6, () => { boss?.Follow(); boss?.Cast(ActionId.ScytheKick); });
        At(KickTime - 6, () => boss?.VoiceLine(VoiceLineId.ScytheKick));
        At(KickTime, () => {
            Kick(boss, ActionId.ScytheKick);
            foreach (var red in pattern.Reds) world.Map.AddEffect(MapEffect.RedMirrorArm, MirrorSlot(red));
        });
        At(KickTime + 0.1f, () => {
            blue?.Cast(ActionId.ReflectedScytheKickInstant, castSeconds: 0);
            Kick(blue, ActionId.ReflectedScytheKickInstant, "blue mirror");
            foreach (var red in reds) red?.Cast(ActionId.ReflectedScytheKick);
        });
        At(BlueHouseTime, () => Proteans(blue));
        At(KickTime + 0.1f, () => FollowTank(humanOnly: false));
        At(BossHouseTime, () => Proteans(boss));
        At(RedKickTime, () => { foreach (var red in reds) Kick(red, ActionId.ReflectedScytheKick, "red mirror"); });
        At(RedHouseTime, () => { foreach (var red in reds) Proteans(red); });
        At(BanishCastTime, () => { boss?.Follow(); boss?.Cast(pattern.Pairs ? ActionId.BanishPairsCast : ActionId.BanishSpreadCast); });
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
    private static void Hit(SimCharacter member, uint action, string? cause, bool lethal)
    { if (lethal && member.IsAlive()) member.Die(action, cause); }
    private static byte MirrorSlot(int octant) => (byte)(MapEffect.FirstReflectingMirror + octant);
    private SimEnemy? Spawn(uint id, Vector3 position, bool bossActor = false)
    {
        var actor = world.SpawnEnemy(new EnemySpawnConfig(id,
            NameId: bossActor ? BNpcNameId.Usurper : id == BNpcBaseId.ReflectingMirror ? BNpcNameId.ReflectingMirror : 0, Level: Level,
            Targetable: bossActor, EnemyList: bossActor ? EnemyListMode.OnlyWhenVisible : EnemyListMode.Never,
            Visibility: bossActor ? SpawnVisibility.Visible : SpawnVisibility.InvisibleHelper, Placement: new(position, 0),
            ModelCharaId: bossActor ? ModelCharaId.UsurperLight : 0));
        if (actor != null) owned.Add(actor);
        return actor;
    }
    private SimEnemy? SpawnMirror(int octant, uint show)
    {
        world.Map.AddEffect(show, MirrorSlot(octant));
        shownMirrors.Add(MirrorSlot(octant));
        var mirror = Spawn(BNpcBaseId.ReflectingMirror, MirrorMirrorPattern.Point(octant, 20));
        mirror?.Face(Vector3.Zero);
        return mirror;
    }
    private void Effect(uint action, Vector3 position, SimCharacter target, float rotation)
    {
        var helper = Spawn(BNpcBaseId.Helper, position);
        helper?.SetRotation(rotation);
        helper?.Cast(action, targetLocation: position, target: target, castSeconds: 0);
        if (helper != null) world.Events.Add(3, helper.Despawn);
    }
    private void Kick(SimEnemy? source, uint action, string? cause = null)
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
            Effect(ActionId.HouseOfLightDiamondDust, origin, target, MathF.Atan2(direction.X, direction.Z));
            foreach (var member in members)
            {
                var toMember = member.Position - origin;
                if (toMember.LengthSquared() < 0.001f || Vector3.Dot(Vector3.Normalize(toMember), direction) >= MathF.Cos(MathF.PI / 12))
                    Hit(member, ActionId.HouseOfLightDiamondDust, "cone overlap", member != target);
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
        var action = pattern.Pairs ? ActionId.BanishPairs : ActionId.BanishSpread;
        for (var i = 0; i < targets.Length; i++)
        {
            Effect(action, centers[i], targets[i], 0);
            var group = members.Where(m => Vector3.Distance(m.Position, centers[i]) <= BanishRadius).ToArray();
            if (group.Length != (pattern.Pairs ? 2 : 1))
                foreach (var member in group) Hit(member, action, pattern.Pairs ? "pair up" : "spread", true);
        }
        foreach (var member in members)
        {
            var covering = centers.Count(c => Vector3.Distance(member.Position, c) <= BanishRadius);
            if (covering != 1) Hit(member, action, covering == 0 ? "no partner" : "overlap", true);
        }
    }
    private void Finish()
    {
        running = false;
        foreach (var role in MirrorMirrorPattern.Roles)
            if (world.Party.Get(role) is { } bot && world.Party.IsBotDriven(bot)) bot.StopMoving();
        foreach (var obj in owned) obj.Despawn();
        foreach (var slot in shownMirrors) world.Map.AddEffect(MapEffect.ReflectingMirrorHide, slot);
        shownMirrors.Clear();
    }
}
