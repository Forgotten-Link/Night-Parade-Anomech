namespace AnoMech.Scenarios.Fru.FallOfFaith;

internal static class FallOfFaithConstants
{
    public const uint Fatebreaker = 17819, FatebreakerName = 9707, Image = 17820, ImageName = 9708;
    public const uint FallOfFaithFire = 40137, FallOfFaithLightning = 40140;
    public const uint ChargeFire = 40138, ChargeLightning = 40141;
    public const uint Sinblaze = 40156, Sinsmite = 40142, BowShock = 40143;
    public const ushort FireTether = 249, LightningTether = 287;
    public const ushort FloatingFetters = 2304, MagicVulnerability = 2941;
    public const float CastTime = 8.7f, FettersTime = 3.96f, VulnerabilityTime = 3.96f;
    // Fatebreaker and his images stop a hitbox short of the tethered player.
    public const float ChargeStop = 5.5f;
    public const float ConeHalfAngle = 0.5236f;

    // Offsets from the first tether, one per tether; Bow Shock follows Sinsmite by 0.31s.
    public const float Start = 10;
    public static readonly float[] TetherAt = [-0.09f, 3.08f, 5.57f, 8.11f];
    public static readonly float[] ImageShowAt = [0, 2.0f, 4.5f, 7.0f];
    public static readonly float[] CastAt = [0, 3.12f, 5.62f, 8.16f];
    public static readonly float[] FettersAt = [9.67f, 12.70f, 15.20f, 17.70f];
    public static readonly float[] ChargeAt = [11.72f, 14.76f, 17.25f, 19.74f];
    public static readonly float[] HitAt = [13.06f, 16.09f, 18.59f, 21.08f];
    public const float BowShockDelay = 0.31f, Length = 24;
}
