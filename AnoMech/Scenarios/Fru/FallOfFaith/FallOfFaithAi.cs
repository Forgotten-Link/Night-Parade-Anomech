using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FallOfFaith.FallOfFaithConstants;

namespace AnoMech.Scenarios.Fru.FallOfFaith;

internal sealed class FallOfFaithAi(string name, PartyRole[] conga) : IScenarioAi
{
    public string Name => name;
    public string Group => "NA";

    internal void Run(FallOfFaithPattern pattern, SimWorld world, Func<bool> active)
    {
        void Move(float time, Func<PartyRole, Vector3> position, Func<PartyRole, bool>? include = null)
            => world.Events.Add(time, () => {
                if (!active()) return;
                foreach (var role in FallOfFaithPattern.Roles)
                    if ((include == null || include(role)) && world.Party.Get(role) is SimPartyNpc bot && bot.IsAlive()
                        && !Rooted(role))
                        bot.MoveTo(position(role), 6);
            });
        bool Rooted(PartyRole role)
        {
            var tether = pattern.TetherIndex(role);
            if (tether < 0) return false;
            var since = world.Events.Elapsed - (Start + FettersAt[tether]);
            return since >= 0 && since < FettersTime;
        }
        Vector3 FirstOnSide(PartyRole r) => pattern.Layout(conga, r, pattern.SideOf(conga, r) < 0 ? 0 : 1);
        Vector3 SecondOnSide(PartyRole r) => pattern.Layout(conga, r, pattern.SideOf(conga, r) < 0 ? 2 : 3);

        Move(1, r => FallOfFaithPattern.CongaSpot(conga, r));
        for (var tether = 0; tether < 4; tether++)
        {
            var tethered = pattern.Tethered[tether];
            Move(Start + TetherAt[tether] + 0.4f, FirstOnSide, r => r == tethered);
        }
        Move(Start + TetherAt[3] + 2.0f, FirstOnSide);
        Move(Start + HitAt[0] + 0.65f, SecondOnSide, r => pattern.SideOf(conga, r) < 0);
        Move(Start + HitAt[1] + 0.65f, SecondOnSide, r => pattern.SideOf(conga, r) > 0);
        Move(Start + HitAt[3] + 1.5f, r => FallOfFaithPattern.CongaSpot(conga, r));
    }
}
