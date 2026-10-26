using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.P1FallOfFaith;

// NA strat from the community P1 slides: tethers 1/3 resolve west and 2/4 east; the untethered
// fill from the conga, first two west and last two east.
internal sealed class FallOfFaithPattern
{
    public static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();
    public static readonly PartyRole[] LightPartyConga =
        [PartyRole.PhysRangedDps, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.MainTank,
         PartyRole.OffTank, PartyRole.MeleeDpsB, PartyRole.ShieldHealer, PartyRole.CasterDps];

    public PartyRole[] Tethered { get; }
    public bool[] Fire { get; }

    public FallOfFaithPattern(PartyRole[] tethered, bool[] fire)
    {
        if (tethered.Length != 4 || tethered.Distinct().Count() != 4 || fire.Length != 4)
            throw new ArgumentException("Four distinct tethered roles and four elements required.");
        Tethered = tethered; Fire = fire;
    }

    public static FallOfFaithPattern Random(Random random) =>
        new(Roles.OrderBy(_ => random.Next()).Take(4).ToArray(), Enumerable.Range(0, 4).Select(_ => random.Next(2) == 0).ToArray());

    public static int Side(int tether) => tether % 2 == 0 ? -1 : 1;
    public int TetherIndex(PartyRole role) => Array.IndexOf(Tethered, role);

    public static Vector3 CongaSpot(PartyRole[] conga, PartyRole role) => new(-7 + 2 * Array.IndexOf(conga, role), 0, 5);
    public static Vector3 ResolveSpot(int side) => new(5.5f * side, 0, 0);

    // Side of a role once all four tethers are out, and which conga end it fills from.
    public int SideOf(PartyRole[] conga, PartyRole role)
    {
        var tether = TetherIndex(role);
        if (tether >= 0) return Side(tether);
        var fillers = conga.Where(r => TetherIndex(r) < 0).ToList();
        return fillers.IndexOf(role) < 2 ? -1 : 1;
    }

    // Where `role` stands while tether `resolving` goes off on its side. Lightning baits:
    // the other tether takes the outer bait, the filler nearest their side's edge north.
    public Vector3 Layout(PartyRole[] conga, PartyRole role, int resolving)
    {
        var side = Side(resolving);
        if (Tethered[resolving] == role) return ResolveSpot(side);
        var otherTether = Tethered[resolving < 2 ? resolving + 2 : resolving - 2];
        var fillers = conga.Where(r => TetherIndex(r) < 0).ToList();
        var sideFillers = side < 0 ? fillers.Take(2).ToList() : fillers.Skip(2).Reverse().ToList();
        if (Fire[resolving])
        {
            if (role == otherTether) return new(7.2f * side, 0, 0);
            return new(8.2f * side, 0, sideFillers.IndexOf(role) == 0 ? -0.5f : 0.5f);
        }
        if (role == otherTether) return new(8.3f * side, 0, 0);
        return new(5.8f * side, 0, sideFillers.IndexOf(role) == 0 ? -3 : 3);
    }
}
