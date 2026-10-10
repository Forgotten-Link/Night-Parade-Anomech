using AnoMech.Core.Native.Implementations.Pointers;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace AnoMech.Core.Native.Implementations;

internal sealed unsafe class StaticVfxProxy(VfxObject* vfx) : IStaticVfxProxy
{
    public void Remove() => vfx->CleanupRender();

    // The resource API reserves bit 0, queues slot + 1, and the scheduler subtracts one on
    // dispatch. It stores the request even while its AVFX is loading. Never pass the scene
    // object to the low-level Apricot timeline constructor (crashes at +0x230).
    public bool TryQueueTrigger(uint triggerIndex)
    {
        if (vfx->VfxResourceInstance == null) return false;
        VfxDataPointers.QueueResourceTrigger(vfx->VfxResourceInstance, triggerIndex + 1);
        return true;
    }
}
