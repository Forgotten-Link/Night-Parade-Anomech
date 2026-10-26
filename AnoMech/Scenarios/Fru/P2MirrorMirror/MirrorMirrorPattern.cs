using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.P2MirrorMirror;

// Octants count clockwise from north. The two red mirrors are always 90 degrees apart.
// Angles are clockwise degrees; the lineups read left to right facing the wall.
internal sealed class MirrorMirrorPattern(int blue, int firstRed, bool pairs, bool supportPairs)
{
    internal static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();
    private static readonly PartyRole[] RangedLineup =
        [PartyRole.RegenHealer, PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.ShieldHealer];
    private static readonly PartyRole[] MeleeLineup =
        [PartyRole.MainTank, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.OffTank];
    private static readonly float[] BlueBaits = [60, 22, -22, -60];
    private static readonly float[] BossBaits = [-45, -135, 135, 45];
    private static readonly float[] RedBaitsOtherLeft = [70, -5, -38, -71];
    private static readonly float[] RedBaitsOtherRight = [71, 38, 5, -70];
    private static readonly float[] RangedSpreads = [-135, -45, 45, 135];
    private static readonly float[] MeleeSpreads = [-45, -135, 135, 45];
    public int Blue { get; } = blue % 8;
    public int[] Reds { get; } = [firstRed % 8, (firstRed + 2) % 8];
    public bool Pairs { get; } = pairs;
    public bool SupportPairs { get; } = supportPairs;

    public static MirrorMirrorPattern Random(Random random)
    {
        var blue = random.Next(8);
        // The reds never sit on the blue, and are never centred opposite it.
        int[] firstRedOffsets = [1, 2, 4, 5, 7];
        var firstRed = blue + firstRedOffsets[random.Next(firstRedOffsets.Length)];
        return new(blue, firstRed, random.Next(2) == 0, random.Next(2) == 0);
    }

    public static Vector3 Point(float octant, float radius)
        => new(MathF.Sin(octant * MathF.PI / 4) * radius, 0, -MathF.Cos(octant * MathF.PI / 4) * radius);
    private static Vector3 Offset(float octant, float degrees, float radius) => Point(octant + degrees / 45, radius);
    public Vector3 BluePosition => Point(Blue, 20);
    public Vector3 RedPosition(int index) => Point(Reds[index], 20);
    public Vector3 BossSpot => Point(Blue + 4, 15);

    public static bool Melee(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank
        or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;
    private static int Lineup(PartyRole role) => Array.IndexOf(Melee(role) ? MeleeLineup : RangedLineup, role);

    private int RangedRed
    {
        get
        {
            var clockwise = Reds.Select(r => (r - Blue + 8) % 8).ToArray();
            var distance = clockwise.Select(d => Math.Min(d, 8 - d)).ToArray();
            return distance[0] != distance[1] ? distance[0] < distance[1] ? 0 : 1 : clockwise[0] < clockwise[1] ? 0 : 1;
        }
    }
    public int RedOf(PartyRole role) => Melee(role) ? 1 - RangedRed : RangedRed;

    public Vector3 ProteanSpot(PartyRole role, Vector3 boss) => Melee(role)
        ? boss + Offset(Blue + 4, BossBaits[Lineup(role)], 2.5f)
        : BluePosition + Offset(Blue + 4, BlueBaits[Lineup(role)], 3);
    // The second red sits clockwise of the first, so its group has the other party on its left.
    public Vector3 RedSpot(PartyRole role)
    {
        var red = RedOf(role);
        var baits = red == 1 ? RedBaitsOtherLeft : RedBaitsOtherRight;
        return RedPosition(red) + Offset(Reds[red] + 4, baits[Lineup(role)], 3);
    }
    public Vector3 BanishSpot(PartyRole role)
    {
        var red = Reds[RedOf(role)];
        if (Pairs) return Point(red, 12) + Offset(red, Lineup(role) < 2 ? -90 : 90, 4.5f);
        return Point(red, 13) + Offset(red, (Melee(role) ? MeleeSpreads : RangedSpreads)[Lineup(role)], 6);
    }
}
