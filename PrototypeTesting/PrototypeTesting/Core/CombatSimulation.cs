using PrototypeTesting.Core.Ecs;
using PrototypeTesting.Models;

namespace PrototypeTesting.Core;

public class CombatSimulation
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
        AppGlobal.World = world;
        world.Arena.Width = width;
        world.Arena.Height = height;
        world.Arena.DodgeBoost = config.DodgeBoost;

        var controlledActorId = world.CreateEntityId();
        var controlledActor = new Actor(controlledActorId)
        {
            Role = ActorRole.Controlled
        };
        controlledActor.AddComponent(new TransformComponent(controlledActorId)
        {
            X = width * 0.18,
            Y = height * 0.52,
            Size = 24,
            FacingX = 1,
            FacingY = 0
        });
        controlledActor.AddComponent(new MovementStatsComponent(controlledActorId)
        {
            Speed = 210
        });
        controlledActor.AddComponent(new HealthComponent(controlledActorId)
        {
            Health = config.ControlledHealth,
            MaxHealth = config.ControlledHealth
        });
        controlledActor.AddComponent(new CombatStateComponent(controlledActorId));
        world.AddActor(controlledActor);

        foreach (var (point, index) in config.SpawnPoints.Select((point, index) => (point, index)))
        {
            var actorId = world.CreateEntityId();
            var actor = new Actor(actorId)
            {
                Role = ActorRole.Opponent
            };
            actor.AddComponent(new TransformComponent(actorId)
            {
                X = point.X,
                Y = point.Y,
                Size = 0
            });
            actor.AddComponent(new MovementStatsComponent(actorId)
            {
                Speed = config.OpponentSpeed + (index * 1.5)
            });
            actor.AddComponent(new HealthComponent(actorId)
            {
                Health = config.OpponentHealth,
                MaxHealth = config.OpponentHealth
            });
            actor.AddComponent(new CombatStateComponent(actorId));
            actor.AddComponent(new CollisionComponent(actorId)
            {
                Radius = 18 + ((index % 2) * 3)
            });
            world.AddActor(actor);
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
        var controlledActor = world.GetActor(State.ControlledActorId);
        world.Time.DeltaSeconds = deltaSeconds;
        world.Time.ElapsedSeconds += deltaSeconds;

        _commandResolutionSystem.Update(TrackEvent);
        _actorMovementSystem.Update(controlledActor, inputState);
        _opponentChaseSystem.Update(controlledActor);
        _collisionDetectionSystem.Update(controlledActor);
        _damageApplySystem.Update(TrackEvent);
        _battleOutcomeSystem.Update(TrackEvent);
    }

    private void TrackEvent(string eventType, Dictionary<string, object?> payload)
    {
        _ = _telemetry.TrackEventAsync(eventType, payload);
    }
}

public class CombatSimulationState
{
    public required EcsWorld World { get; init; }

    public required long ControlledActorId { get; init; }
}

public class PrototypeConfig
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
