using System;
using AnoMech.Core;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru;

// Damage feedback for mechanics the FRU scenarios resolve themselves: a flytext number sized off
// the target's own max HP, plus the KO via Die() when the caller judged the hit lethal.
public sealed class DamageSolver
{
    public void ApplyDamage(SimCharacter target, float fractionOfMaxHp, uint actionId, string context, bool lethal)
    {
        if (target is not ISimPartyMember) return;
        var name = ActionLookup.Name(actionId);
        if (target.Proxy is { Exists: true } chara)
            chara.ShowFlyText((uint)MathF.Round(fractionOfMaxHp * chara.MaxHealth), name);
        if (!lethal) return;
        target.Die($"Died to {name} ({context})");
    }
}
