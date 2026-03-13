using PrototypeTesting.Core.Ecs;
using PrototypeTesting.Models;

namespace PrototypeTesting.Core;

internal sealed class CombatSimulation
{
    private readonly TelemetryService _telemetry;
    private readonly InputActionSystem _inputActionSystem = new();
    private readonly CommandResolutionSystem _commandResolutionSystem = new();
    private readonly ActorMovementSystem _actorMovementSystem = new();
    private readonly OpponentChaseSystem _opponentChaseSystem = new();
    private readonly CollisionDetectionSystem _collisionDetectionSystem = new();
    private readonly DamageApplySystem _damageApplySystem = new();
    private readonly BattleOutcomeSystem _battleOutcomeSystem = new();

    private CombatSimulation(CombatSimulationState state, TelemetryService telemetry)
    {
        State = state;
        _telemetry = telemetry;
    }

    public CombatSimulationState State { get; }

    public static CombatSimulation Create(PrototypeDefinition prototype, TelemetryService telemetry)
    {
        const double width = 960;
        const double height = 540;
        var config = PrototypeConfig.Create(prototype.Id, width, height);
        var world = new EcsWorld();
        world.Arena.Width = width;
        world.Arena.Height = height;
        world.Arena.DodgeBoost = config.DodgeBoost;

        var controlledActorId = world.CreateEntityId();
        world.AddActor(new Actor(controlledActorId)
        {
            Role = ActorRole.Controlled
        });
        world.AddComponent<TransformComponent>(controlledActorId, component =>
        {
            component.X = width * 0.18;
            component.Y = height * 0.52;
            component.Size = 24;
            component.FacingX = 1;
            component.FacingY = 0;
        });
        world.AddComponent<MovementStatsComponent>(controlledActorId, component =>
        {
            component.Speed = 210;
        });
        world.AddComponent<HealthComponent>(controlledActorId, component =>
        {
            component.Health = config.ControlledHealth;
            component.MaxHealth = config.ControlledHealth;
        });
        world.AddComponent<CombatStateComponent>(controlledActorId);

        foreach (var (point, index) in config.SpawnPoints.Select((point, index) => (point, index)))
        {
            var actorId = world.CreateEntityId();
            world.AddActor(new Actor(actorId)
            {
                Role = ActorRole.Opponent
            });
            world.AddComponent<TransformComponent>(actorId, component =>
            {
                component.X = point.X;
                component.Y = point.Y;
                component.Size = 0;
            });
            world.AddComponent<MovementStatsComponent>(actorId, component =>
            {
                component.Speed = config.OpponentSpeed + (index * 1.5);
            });
            world.AddComponent<HealthComponent>(actorId, component =>
            {
                component.Health = config.OpponentHealth;
                component.MaxHealth = config.OpponentHealth;
            });
            world.AddComponent<CombatStateComponent>(actorId);
            world.AddComponent<CollisionComponent>(actorId, component =>
            {
                component.Radius = 18 + ((index % 2) * 3);
            });
        }

        var state = new CombatSimulationState
        {
            World = world,
            ControlledActorId = controlledActorId
        };

        return new CombatSimulation(state, telemetry);
    }

    public void HandleInput(InputEdge edge, InputState inputState)
    {
        _inputActionSystem.HandleInput(State, edge, TrackEvent);
    }

    public void Update(double deltaSeconds, InputState inputState)
    {
        if (State.World.BattleState.IsBattleOver)
        {
            return;
        }

        var world = State.World;
        world.Time.DeltaSeconds = deltaSeconds;
        world.Time.ElapsedSeconds += deltaSeconds;

        _commandResolutionSystem.Update(State, TrackEvent);
        _actorMovementSystem.Update(State, inputState);
        _opponentChaseSystem.Update(State);
        _collisionDetectionSystem.Update(State);
        _damageApplySystem.Update(State, TrackEvent);
        _battleOutcomeSystem.Update(State, TrackEvent);
    }

    private void TrackEvent(string eventType, Dictionary<string, object?> payload)
    {
        _ = _telemetry.TrackEventAsync(eventType, payload);
    }
}

internal sealed class CombatSimulationState
{
    public required EcsWorld World { get; init; }

    public required long ControlledActorId { get; init; }
}

internal sealed class PrototypeConfig
{
    public required int OpponentCount { get; init; }

    public required double OpponentSpeed { get; init; }

    public required int OpponentHealth { get; init; }

    public required int ControlledHealth { get; init; }

    public required double DodgeBoost { get; init; }

    public required IReadOnlyList<SimulationVector> SpawnPoints { get; init; }

    public static PrototypeConfig Create(string prototypeId, double width, double height)
    {
        var config = prototypeId switch
        {
            "movement-lab" => new PrototypeConfigTemplate(7, 95, 1, 5, 3.4),
            "choice-room" => new PrototypeConfigTemplate(9, 88, 2, 4, 2.9),
            _ => new PrototypeConfigTemplate(6, 82, 2, 5, 2.8)
        };

        var spawnPoints = new List<SimulationVector>();
        var centerX = width / 2;
        var centerY = height / 2;
        var ring = Math.Min(width, height) * 0.34;

        for (var index = 0; index < config.OpponentCount; index += 1)
        {
            var angle = (Math.PI * 2 * index) / config.OpponentCount;
            spawnPoints.Add(new SimulationVector(
                centerX + (Math.Cos(angle) * ring),
                centerY + (Math.Sin(angle) * ring)));
        }

        return new PrototypeConfig
        {
            OpponentCount = config.OpponentCount,
            OpponentSpeed = config.OpponentSpeed,
            OpponentHealth = config.OpponentHealth,
            ControlledHealth = config.ControlledHealth,
            DodgeBoost = config.DodgeBoost,
            SpawnPoints = spawnPoints
        };
    }

    private readonly record struct PrototypeConfigTemplate(int OpponentCount, double OpponentSpeed, int OpponentHealth, int ControlledHealth, double DodgeBoost);
}
