using System;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru;

// For the FRU scenarios that resolve their mechanics themselves rather than through EnemyAction
// effects (Fall of Faith, Towers, Mirror, Mirror).
public static class FruUtils
{
    // Their patterns roll from a System.Random; seeding it from the run's Rng keeps a run
    // replayable from its seed.
    public static Random Random(SimWorld world)
    {
#pragma warning disable RS0030
        return new Random(world.Rng.Next(int.MaxValue));
#pragma warning restore RS0030
    }

    // Only the cast's visuals. castSeconds is the bar (null = the sheet's, 0 = instant); a ground
    // location wins over a target.
    public static void Cast(this SimEnemy enemy, uint actionId, Vector3? targetLocation = null,
        float? castSeconds = null, SimCharacter? target = null, float animationLock = 0.6f)
    {
        if (!enemy.IsActive) return;
        var action = new EnemyAction(actionId) { Cast = new() { AnimationLock = animationLock, CastSecondsOverride = castSeconds } };
        enemy.Cast(action, targetLocation is { } location ? location : target);
    }
}
