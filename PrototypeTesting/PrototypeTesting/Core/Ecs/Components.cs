namespace PrototypeTesting.Core.Ecs;

internal sealed class TransformComponent(long entityId) : EcsComponent(entityId)
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Size { get; set; }

    public double FacingX { get; set; } = 1;

    public double FacingY { get; set; }

    public SimulationVector Center => new(X + (Size / 2), Y + (Size / 2));
}

internal sealed class Actor(long id) : EcsEntity(id)
{
    public ActorRole Role { get; set; }
}

internal enum ActorRole
{
    Controlled,
    Opponent
}

internal sealed class MovementStatsComponent(long entityId) : EcsComponent(entityId)
{
    public double Speed { get; set; }
}

internal sealed class HealthComponent(long entityId) : EcsComponent(entityId)
{
    public int Health { get; set; }

    public int MaxHealth { get; set; }

    public bool IsAlive => Health > 0;
}

internal sealed class CombatStateComponent(long entityId) : EcsComponent(entityId)
{
    public double AttackUntil { get; set; }

    public double AttackCooldownUntil { get; set; }

    public double DodgeUntil { get; set; }

    public double InvulnerableUntil { get; set; }

    public double HitFlashUntil { get; set; }

    public double ContactCooldownUntil { get; set; }

    public bool IsAttacking(double elapsedSeconds) => AttackUntil > elapsedSeconds;

    public bool IsDodging(double elapsedSeconds) => DodgeUntil > elapsedSeconds;

    public bool IsInvulnerable(double elapsedSeconds) => InvulnerableUntil > elapsedSeconds;

    public bool IsHitFlashing(double elapsedSeconds) => HitFlashUntil > elapsedSeconds;
}

internal sealed class CollisionComponent(long entityId) : EcsComponent(entityId)
{
    public double Radius { get; set; }
}

internal sealed class ArenaComponent() : EcsComponent(0)
{
    public double Width { get; set; }

    public double Height { get; set; }

    public double DodgeBoost { get; set; }
}

internal sealed class TimeComponent() : EcsComponent(0)
{
    public double DeltaSeconds { get; set; }

    public double ElapsedSeconds { get; set; }
}

internal sealed class BattleStatsComponent() : EcsComponent(0)
{
    public int Attacks { get; set; }

    public int Dodges { get; set; }

    public int TargetHits { get; set; }

    public int TotalInputs { get; set; }

    public int Kills { get; set; }

    public int WavesCleared { get; set; }
}

internal sealed class BattleStateComponent() : EcsComponent(0)
{
    public bool IsBattleOver { get; set; }

    public bool EndEventPublished { get; set; }

    public string Outcome { get; set; } = "Running";
}

internal sealed class CombatCommandBufferComponent() : EcsComponent(0)
{
    public List<CombatCommand> Commands { get; } = [];
}

internal sealed class CombatEventBufferComponent() : EcsComponent(0)
{
    public List<CombatEvent> Events { get; } = [];
}

internal abstract record CombatCommand;

internal sealed record AttackCommand(long SourceEntityId) : CombatCommand;

internal sealed record DodgeCommand(long SourceEntityId) : CombatCommand;

internal abstract record CombatEvent;

internal sealed record DamageEvent(long TargetEntityId, int Amount, double HitFlashUntil, double InvulnerableUntil, double ContactCooldownUntil, string Source) : CombatEvent;

internal sealed record ActorKilledEvent(long EntityId) : CombatEvent;

internal readonly record struct SimulationVector(double X, double Y);
