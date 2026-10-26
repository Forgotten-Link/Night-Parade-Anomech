using System;
using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.P1FallOfFaith;
using AnoMech.Scenarios.Fru.P1Towers;
using AnoMech.Scenarios.Fru.P2DiamondDust;
using AnoMech.Scenarios.Fru.P2LightRampant;
using AnoMech.Scenarios.Fru.P2MirrorMirror;
using AnoMech.Scenarios.Fru.P3Apocalypse;
using AnoMech.Scenarios.Fru.P3UltimateRelativity;
using AnoMech.Scenarios.Fru.P4CrystallizeTime;
using AnoMech.Scenarios.Fru.P4DarklitDragonsong;
using AnoMech.Scenarios.Fru.P5FulgentBlade;
using AnoMech.Scenarios.Fru.P5ParadiseRegained;

namespace AnoMech.Scenarios.Fru;

public sealed class FruAllScenario(IReadOnlyList<IScenario> scenarios) : IScenarioSequence
{
    // All starts at P2, so the P1 mechanics stay standalone entries. The sequence shares the
    // catalog's instances, keeping each scenario's settings.
    public static IReadOnlyList<IScenario> CreateCatalog()
    {
        IScenario[] fromP2 =
        [
            new FruP2DiamondDustScenario(), new FruMirrorMirrorScenario(), new FruP2LightRampantScenario(),
            new FruP3UltimateRelativityScenario(), new FruP3ApocalypseScenario(),
            new FruP4DarklitDragonsongScenario(), new FruP4CrystallizeTimeScenario(),
            new FruP5FulgentBladeScenario(), new FruP5ParadiseRegainedScenario(),
        ];
        return [new FruAllScenario(fromP2), new FruFallOfFaithScenario(), new FruTowersScenario(), .. fromP2];
    }

    public string Name => "All";
    // Only places the menu entry first; each step runs in its own phase.
    public IPhase Phase => FruZone.P1;
    public IReadOnlyList<IScenario> Scenarios { get; } = scenarios;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new NaAi()];

    public void Run(SimWorld world, int? selectedAi) => throw new InvalidOperationException("Game runs a sequence's steps, never the sequence.");

    private sealed class NaAi : IScenarioAi
    {
        public string Name => "NA";
        public string Group => "NA";
    }
}
