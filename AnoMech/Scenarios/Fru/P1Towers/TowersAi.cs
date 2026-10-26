using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.P1Towers.TowersConstants;

namespace AnoMech.Scenarios.Fru.P1Towers;

internal sealed class TowersAi : IScenarioAi
{
    public string Name => "NA";
    public string Group => "NA";

    internal void Run(TowersPattern pattern, SimWorld world, Func<bool> active)
    {
        void Move(float time, Func<PartyRole, Vector3> position)
            => world.Events.Add(time, () => {
                if (!active()) return;
                foreach (var role in TowersPattern.Roles)
                    if (world.Party.Get(role) is { } bot && world.Party.IsBotDriven(bot) && bot.IsAlive()) bot.MoveTo(position(role), 6);
            });

        Move(1, r => r == PartyRole.MainTank ? new(0, 0, -6.5f) : r == PartyRole.OffTank ? new Vector3(-2, 0, -6.5f) : new(0, 0, 5));
        Move(Strike + 0.5f, pattern.Fire ? pattern.StrikeDodge : pattern.Soak);
        if (pattern.Fire) Move(StrikeHit + 0.15f, pattern.KnockbackSpot);
        Move(BurnMarkHit + 0.8f, r => r.IsTank() ? pattern.TankSpot(r) : new(0, 0, 5));
    }
}
