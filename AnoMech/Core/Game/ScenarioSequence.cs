using System;
using System.Collections.Generic;
using AnoMech.Scenarios;

namespace AnoMech.Core.Game;

// Steps through an IScenarioSequence, one clean run after another. Game owns starting each step.
internal sealed class ScenarioSequence
{
    // Real time, whatever the event speed.
    private const float BetweenScenarios = 2f;

    public RunScenarioParams Requested { get; }
    public IReadOnlyList<IScenario> Scenarios { get; }
    public int Index { get; private set; }
    public IScenario Current => Scenarios[Index];
    public bool HasNext => Index < Scenarios.Count - 1;
    public bool Waiting { get; private set; }
    private float untilNext;

    public ScenarioSequence(IScenarioSequence sequence, RunScenarioParams requested)
    {
        if (sequence.Scenarios.Count == 0) throw new ArgumentException("A sequence needs at least one scenario.");
        Requested = requested;
        Scenarios = sequence.Scenarios;
    }

    // Only the first step keeps the requested seed: the party was seated from it.
    public RunScenarioParams CurrentParams => Requested with
    {
        Scenario = Current,
        SelectedAi = Requested.SelectedAi is null ? null : NaStrat(Current),
        Seed = Index == 0 ? Requested.Seed : null,
    };

    public void FinishStep()
    {
        Waiting = true;
        untilNext = BetweenScenarios;
    }

    // True once the pause after a finished step is over.
    public bool TickBetweenSteps(float deltaSeconds)
    {
        if (!Waiting) return false;
        untilNext -= deltaSeconds;
        return untilNext <= 0f;
    }

    public void Advance()
    {
        Index++;
        Waiting = false;
    }

    private static int NaStrat(IScenario scenario)
    {
        for (var i = 0; i < scenario.AiStrats.Count; i++)
            if (scenario.AiStrats[i].Group == "NA") return i;
        return 0;
    }
}
