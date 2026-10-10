namespace AnoMech.Scenarios.Fru.Towers;

internal static class TowersConstants
{
    public const uint Fatebreaker = 17819, FatebreakerName = 9707;
    public const uint PowderMarkTrail = 40168, BurnMark = 40169;
    public const uint BurntStrikeFire = 40129, Blastburn = 40130, BurntStrikeLightning = 40133, Burnout = 40134;
    public const uint UnmitigatedExplosion = 40136;
    // Explosion rows by required soakers (index 1..4).
    public static readonly uint[] FireTower = [0, 40131, 40122, 40123, 40124];
    public static readonly uint[] LightningTower = [0, 40135, 40125, 40126, 40127];
    public const ushort PowderMarkStatus = 4166, PhysicalVulnerability = 2940;

    public const float StrikeHalfWidth = 5, BurnoutHalfWidth = 10, TowerRadius = 4, BurnMarkRadius = 10;
    // A fixed push with no wall to stop it: from too far out it carries you off the arena.
    public const float KnockbackDistance = 15, KnockbackSpeed = 30;

    // Offsets from Burnt Strike's cast start.
    public const float Strike = 12;
    public const float PowderCast = Strike - 10.02f, PowderHit = Strike - 5.03f, PowderDuration = 16.12f;
    public const float StrikeHit = Strike + 6.46f, BlastburnHit = Strike + 8.46f, BurnoutHit = Strike + 8.2f;
    public const float TowerHit = Strike + 10.47f, BurnMarkHit = PowderHit + PowderDuration, Length = Strike + 14;
}
