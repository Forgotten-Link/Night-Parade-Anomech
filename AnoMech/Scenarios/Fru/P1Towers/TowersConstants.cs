namespace AnoMech.Scenarios.Fru.P1Towers;

internal static class TowersConstants
{
    public const float StrikeHalfWidth = 5, BurnoutHalfWidth = 10, TowerRadius = 4, BurnMarkRadius = 10;
    // A fixed push with no wall to stop it: from too far out it carries you off the arena.
    public const float KnockbackDistance = 15, KnockbackSpeed = 30;

    // Offsets from Burnt Strike's cast start.
    public const float Strike = 12;
    public const float PowderCast = Strike - 10.02f, PowderHit = Strike - 5.03f, PowderDuration = 16.12f;
    public const float StrikeHit = Strike + 6.46f, BlastburnHit = Strike + 8.46f, BurnoutHit = Strike + 8.2f;
    public const float TowerHit = Strike + 10.47f, BurnMarkHit = PowderHit + PowderDuration, Length = Strike + 14;
}
