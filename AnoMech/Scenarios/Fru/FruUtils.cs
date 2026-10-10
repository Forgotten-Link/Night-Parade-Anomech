using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Scenarios.Fru;

public static class FruUtils
{
    public static IReadOnlyList<Waymark> FruWaymarks { get; } =
    [
        new(WaymarkSlot.A,     new Vector3(     0f, 0f,   -10f)),
        new(WaymarkSlot.B,     new Vector3(    10f, 0f,      0f)),
        new(WaymarkSlot.C,     new Vector3(     0f, 0f,    10f)),
        new(WaymarkSlot.D,     new Vector3(   -10f, 0f,      0f)),
        new(WaymarkSlot.One,   new Vector3( -7.07f, 0f, -7.07f)),
        new(WaymarkSlot.Two,   new Vector3(  7.07f, 0f, -7.07f)),
        new(WaymarkSlot.Three, new Vector3(  7.07f, 0f,  7.07f)),
        new(WaymarkSlot.Four,  new Vector3( -7.07f, 0f,  7.07f)),
    ];

    // The FRU patterns roll from a System.Random; seeding it from the run's Rng keeps a run
    // replayable from its seed.
    public static Random Random(SimWorld world)
    {
#pragma warning disable RS0030
        return new Random(world.Rng.Next(int.MaxValue));
#pragma warning restore RS0030
    }

    // The meshless helper gets no draw object from the engine unless it is asked for, and casts
    // nothing visible without one.
    public static SpawnVisibility SpawnVisibilityOf(uint bnpcBaseId)
        => bnpcBaseId == FruConstants.BNpcBaseId.Helper ? SpawnVisibility.InvisibleHelper : SpawnVisibility.Visible;

    // The FRU scenarios resolve their mechanics themselves and only need a cast's visuals:
    // castSeconds is the bar's length (null = the sheet's, 0 = instant), targetLocation where the
    // effect lands, target who it is delivered to.
    public static void Cast(this SimEnemy enemy, uint actionId, Vector3? targetLocation = null,
        float? castSeconds = null, SimCharacter? target = null, float animationLock = 0.6f)
    {
        if (enemy.PacketSpawnPending)
        {
            enemy.WhenSpawned(() => enemy.Cast(actionId, targetLocation, castSeconds, target, animationLock));
            return;
        }
        if (enemy.IsDefeated) return;
        if (targetLocation is { } location)
        {
            enemy.Face(location);
            // A ground-aimed instant that still names a target: EnemyAction's CastTarget carries
            // one or the other, so the effect goes out directly.
            if (castSeconds is <= 0f && target is not null)
            {
                GameObjectId? deliverTo = Natives.BattleCharas.IsInCharacterManager(target.GameObjectId.ObjectId)
                    ? target.GameObjectId
                    : null;
                enemy.Casting.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
                    rotation: enemy.Rotation, position: location, animationTargetId: deliverTo, actionTargetId: deliverTo);
                return;
            }
        }
        // EnemyAction counts its cast time to the effect, which lands ActionEffectOffset after the bar.
        float? total = castSeconds switch
        {
            null => null,
            <= 0f => 0f,
            { } seconds => seconds + CastSpec.ActionEffectOffset,
        };
        var action = new EnemyAction(actionId) { Cast = new() { AnimationLock = animationLock, CastSecondsOverride = total } };
        enemy.Cast(action, targetLocation is { } at ? at : target);
    }
}
