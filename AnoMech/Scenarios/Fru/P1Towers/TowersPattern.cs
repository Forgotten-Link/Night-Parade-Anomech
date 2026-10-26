using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.P1Towers;

// NA strat from the community P1 slides: H1, Caster and H2 take one tower each north to south;
// M1, M2 and Phys Ranged fill what is left, also north to south. Tanks wait on the far side.
internal sealed class TowersPattern
{
    public static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();
    private static readonly PartyRole[] Fixed = [PartyRole.RegenHealer, PartyRole.CasterDps, PartyRole.ShieldHealer];
    private static readonly PartyRole[] Fillers = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps];
    // Every north/middle/south split of the six soakers with at least one per tower.
    private static readonly int[][] Layouts =
        (from n in Enumerable.Range(1, 4) from m in Enumerable.Range(1, 4) where n + m < 6 && 6 - n - m <= 4 select new[] { n, m, 6 - n - m }).ToArray();

    public int Side { get; }
    public bool Fire { get; }
    // Required soakers for the north, middle and south towers.
    public int[] Counts { get; }

    public TowersPattern(int side, bool fire, int[] counts)
    {
        if (side is not (-1 or 1) || counts.Length != 3 || counts.Sum() != 6 || counts.Any(c => c is < 1 or > 4))
            throw new ArgumentException("Invalid tower pattern.");
        Side = side; Fire = fire; Counts = counts;
    }

    public static TowersPattern Random(Random random)
    {
        return new(random.Next(2) == 0 ? -1 : 1, random.Next(2) == 0, Layouts[random.Next(Layouts.Length)]);
    }

    public Vector3 Tower(int index) => index switch
    {
        0 => new(13.11f * Side, 0, -9.18f),
        1 => new(16 * Side, 0, 0),
        _ => new(13.11f * Side, 0, 9.18f),
    };

    public int TowerOf(PartyRole role)
    {
        var fixedIndex = Array.IndexOf(Fixed, role);
        if (fixedIndex >= 0) return fixedIndex;
        var filler = Array.IndexOf(Fillers, role);
        if (filler < 0) return -1;
        var open = Enumerable.Range(0, 3).SelectMany(t => Enumerable.Repeat(t, Counts[t] - 1)).ToArray();
        return open[filler];
    }

    public Vector3 TankSpot(PartyRole role) => new(-11 * Side, 0, role == PartyRole.MainTank ? -5.5f : 5.5f);

    // Clear of Burnt Strike at the tower's height, then back toward the line so Blastburn
    // carries the soaker into the tower.
    public Vector3 StrikeDodge(PartyRole role) => role.IsTank()
        ? new(-6 * Side, 0, TankSpot(role).Z)
        : new(6.5f * Side, 0, Tower(TowerOf(role)).Z);
    public Vector3 KnockbackSpot(PartyRole role) => role.IsTank()
        ? new(-2 * Side, 0, TankSpot(role).Z)
        : new(0.8f * Side, 0, Tower(TowerOf(role)).Z);
    public Vector3 Soak(PartyRole role) => role.IsTank() ? TankSpot(role) : Tower(TowerOf(role));
}
