using PrototypeTesting.Core.Ecs;
using System.Diagnostics;
using PrototypeTesting.Models;

namespace PrototypeTesting.Core;

public sealed class GameLoopService(TelemetryService telemetry)
{
    private CancellationTokenSource? _loopCancellation;
    private Task? _loopTask;
    private CombatSimulation? _simulation;
    private readonly InputState _inputState = new();
    private readonly SnapshotMapper _snapshotMapper = new();

    public event Action<GameSnapshot>? SnapshotChanged;

    public GameSnapshot CurrentSnapshot { get; private set; } = GameSnapshot.Empty;

    public async Task StartAsync(PrototypeDefinition prototype)
    {
        await StopAsync();

        _inputState.Reset();
        _simulation = CombatSimulation.Create(prototype, telemetry);
        PublishSnapshot();

        _loopCancellation = new CancellationTokenSource();
        _loopTask = RunLoopAsync(_loopCancellation.Token);
    }

    public async Task StopAsync()
    {
        if (_loopCancellation is null)
        {
            return;
        }

        _loopCancellation.Cancel();

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _loopCancellation.Dispose();
        _loopCancellation = null;
        _loopTask = null;
        _inputState.Reset();
    }

    public void SetInputState(string code, bool isPressed)
    {
        if (_simulation is null || _simulation.State.World.GetComponent<BattleStateComponent>().IsBattleOver)
        {
            return;
        }

        if (!_inputState.Apply(code, isPressed, out var edge))
        {
            return;
        }

        _simulation.HandleInput(edge, _inputState);
        PublishSnapshot();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var previous = stopwatch.Elapsed;

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(33, cancellationToken);

            var simulation = _simulation;
            if (simulation is null)
            {
                break;
            }

            var now = stopwatch.Elapsed;
            var deltaSeconds = Math.Min((now - previous).TotalSeconds, 0.05);
            previous = now;

            simulation.Update(deltaSeconds, _inputState);
            PublishSnapshot();

            if (simulation.State.World.GetComponent<BattleStateComponent>().IsBattleOver)
            {
                break;
            }
        }
    }

    private void PublishSnapshot()
    {
        if (_simulation is null)
        {
            return;
        }

        CurrentSnapshot = _snapshotMapper.Map(_simulation.State);
        SnapshotChanged?.Invoke(CurrentSnapshot);
    }
}

