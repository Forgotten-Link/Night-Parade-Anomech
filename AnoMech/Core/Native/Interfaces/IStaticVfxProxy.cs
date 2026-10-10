namespace AnoMech.Core.Native.Interfaces;

// A VFX placed in the world (VfxObject), e.g. an omen.
public interface IStaticVfxProxy
{
    void Remove();

    // Queues one of the AVFX's authored scheduler triggers (zero-based, 0..11). False while the
    // resource instance doesn't exist yet; the caller retries.
    bool TryQueueTrigger(uint triggerIndex);
}
