namespace AnoMech.Scenarios.Fru.MirrorMirror;

internal static class MirrorMirrorConstants
{
    public const uint Usurper = 17823, UsurperName = 12809, LightModel = 4375;
    public const uint FrozenMirror = 17825, FrozenMirrorName = 9317;
    public const uint MirrorCast = 40179, ScytheKick = 40203, ReflectedKickInstant = 40204, ReflectedKick = 40205,
        House = 40206, BanishPairs = 40220, BanishSpread = 40221, BanishPairsHit = 40222, BanishSpreadHit = 40223;
    // ContentDirectorManagedSG 181: mirror slot = octant + 1.
    public const uint BlueShow = 0x00020001, RedShow = 0x02000100, RedArm = 0x08000001, MirrorHide = 0x00080004;
    public const float KickInner = 4, KickOuter = 20, BanishRadius = 5, RunSpeed = 6;
    public const float BossFreeTime = 5, MirrorsTime = 7, BotDragTime = 8.5f, KickTime = 19.1f, BlueHouseTime = 19.8f, BossHouseTime = 19.9f,
        RedKickTime = 29.15f, RedHouseTime = 29.8f, BanishCastTime = 30.3f, BanishTime = 35.35f;
}
