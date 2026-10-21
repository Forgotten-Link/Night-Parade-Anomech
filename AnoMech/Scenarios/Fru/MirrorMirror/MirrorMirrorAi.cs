using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.MirrorMirror.MirrorMirrorConstants;

namespace AnoMech.Scenarios.Fru.MirrorMirror;

internal sealed class MirrorMirrorAi : IScenarioAi
{
    public string Name => "NA · Melee Boss / Ranged Blue";
    public string Group => "NA";
    internal void Run(MirrorMirrorPattern pattern, SimWorld world, Func<bool> active, Func<Vector3> boss)
    {
        void Move(float time, Func<PartyRole, Vector3> position, Func<PartyRole, bool>? who = null)
            => world.Events.Add(time, () => {
                if (!active()) return;
                foreach (var role in MirrorMirrorPattern.Roles)
                    if ((who == null || who(role)) && world.Party.Get(role) is { } bot && bot.IsAlive() && world.Party.IsBotDriven(bot))
                        bot.MoveTo(position(role), RunSpeed);
            });
        Move(MirrorsTime + 0.6f, role => pattern.ProteanSpot(role, pattern.BossSpot), role => !MirrorMirrorPattern.Melee(role));
        for (var time = MirrorsTime + 2; time < KickTime - 6; time += 2)
            Move(time, role => pattern.ProteanSpot(role, boss()), MirrorMirrorPattern.Melee);
        Move(KickTime - 5, role => pattern.ProteanSpot(role, boss()), MirrorMirrorPattern.Melee);
        Move(BossHouseTime + 0.1f, pattern.RedSpot);
        Move(BanishCastTime + 0.2f, pattern.BanishSpot);
    }
}
