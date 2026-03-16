using PrototypeTesting.Core.Ecs;

namespace PrototypeTesting.Core;

public class InputActionSystem
{
    public void HandleInput(CombatSimulationState state, InputEdge edge, Action<string, Dictionary<string, object?>> trackEvent)
    {
        if (!edge.IsPressed)
        {
            return;
        }

        var world = state.World;
        var elapsedSeconds = world.GetComponent<TimeComponent>().ElapsedSeconds;
        world.GetComponent<BattleStatsComponent>().TotalInputs += 1;
        trackEvent("input", new Dictionary<string, object?>
        {
            ["code"] = edge.Code,
            ["elapsedSeconds"] = Math.Round(elapsedSeconds, 2)
        });

        if (edge.Code == "Space")
        {
            world.GetComponent<CombatCommandBufferComponent>().Commands.Add(new AttackCommand(state.ControlledActorId));
        }
        else if (edge.Code is "ShiftLeft" or "ShiftRight")
        {
            world.GetComponent<CombatCommandBufferComponent>().Commands.Add(new DodgeCommand(state.ControlledActorId));
        }
    }
}

public class CommandResolutionSystem
{
    public void Update(Action<string, Dictionary<string, object?>> trackEvent)
    {
        var world = AppGlobal.World;

        if (world.GetComponent<CombatCommandBufferComponent>().Commands.Count == 0)
        {
            return;
        }

        foreach (var command in world.GetComponent<CombatCommandBufferComponent>().Commands)
        {
            switch (command)
            {
                case AttackCommand attack:
                    ResolveAttack(world, attack.SourceEntityId, trackEvent);
                    break;
                case DodgeCommand dodge:
                    ResolveDodge(world, dodge.SourceEntityId, trackEvent);
                    break;
            }
        }

        world.GetComponent<CombatCommandBufferComponent>().Commands.Clear();
    }

    private static void ResolveAttack(EcsWorld world, long sourceEntityId, Action<string, Dictionary<string, object?>> trackEvent)
    {
        var elapsedSeconds = world.GetComponent<TimeComponent>().ElapsedSeconds;
        var battleStats = world.GetComponent<BattleStatsComponent>();
        var attacker = world.GetActor(sourceEntityId);
        var attackerTransform = attacker.GetComponent<TransformComponent>();
        var attackerCombat = attacker.GetComponent<CombatStateComponent>();

        if (attackerCombat.AttackCooldownUntil > elapsedSeconds)
        {
            return;
        }

        battleStats.Attacks += 1;
        attackerCombat.AttackUntil = elapsedSeconds + 0.18;
        attackerCombat.AttackCooldownUntil = elapsedSeconds + 0.38;

        var attackerCenter = attackerTransform.Center;
        var hitCount = 0;

        foreach (var opponent in world.QueryOpponentActors().ToList())
        {
            var opponentHealth = opponent.GetComponent<HealthComponent>();
            if (!opponentHealth.IsAlive)
            {
                continue;
            }

            var opponentTransform = opponent.GetComponent<TransformComponent>();
            var toOpponent = SimulationMath.Normalize(opponentTransform.X - attackerCenter.X, opponentTransform.Y - attackerCenter.Y);
            var alignment = (toOpponent.X * attackerTransform.FacingX) + (toOpponent.Y * attackerTransform.FacingY);
            var inRange = SimulationMath.Distance(attackerCenter.X, attackerCenter.Y, opponentTransform.X, opponentTransform.Y) <= 96;

            if (!inRange || alignment <= 0.15)
            {
                continue;
            }

            hitCount += 1;
            battleStats.TargetHits += 1;
            world.GetComponent<CombatEventBufferComponent>().Events.Add(new DamageEvent(opponent.Id, 1, elapsedSeconds + 0.12, 0, 0, "attack"));
        }

        trackEvent("attack", new Dictionary<string, object?>
        {
            ["landedHits"] = hitCount,
            ["kills"] = battleStats.Kills
        });
    }

    private static void ResolveDodge(EcsWorld world, long sourceEntityId, Action<string, Dictionary<string, object?>> trackEvent)
    {
        var elapsedSeconds = world.GetComponent<TimeComponent>().ElapsedSeconds;
        var battleStats = world.GetComponent<BattleStatsComponent>();
        var actor = world.GetActor(sourceEntityId);
        var combat = actor.GetComponent<CombatStateComponent>();

        if (combat.DodgeUntil > elapsedSeconds)
        {
            return;
        }

        battleStats.Dodges += 1;
        combat.DodgeUntil = elapsedSeconds + 0.22;
        combat.InvulnerableUntil = elapsedSeconds + 0.24;

        trackEvent("dodge", new Dictionary<string, object?>
        {
            ["count"] = battleStats.Dodges,
            ["elapsedSeconds"] = Math.Round(elapsedSeconds, 2)
        });
    }
}

public class ActorMovementSystem
{
    public void Update(Actor actor, InputState inputState)
    {
        var world = AppGlobal.World;
        var directionX = 0.0;
        var directionY = 0.0;

        if (inputState.IsPressed("KeyW")) directionY -= 1;
        if (inputState.IsPressed("KeyS")) directionY += 1;
        if (inputState.IsPressed("KeyA")) directionX -= 1;
        if (inputState.IsPressed("KeyD")) directionX += 1;

        var actorTransform = actor.GetComponent<TransformComponent>();
        var actorMovement = actor.GetComponent<MovementStatsComponent>();
        var actorCombat = actor.GetComponent<CombatStateComponent>();
        var elapsedSeconds = world.GetComponent<TimeComponent>().ElapsedSeconds;

        if (directionX != 0 || directionY != 0)
        {
            var normalized = SimulationMath.Normalize(directionX, directionY);
            actorTransform.FacingX = normalized.X;
            actorTransform.FacingY = normalized.Y;

            var speedMultiplier = actorCombat.IsDodging(elapsedSeconds)
                ? world.GetComponent<ArenaComponent>().DodgeBoost
                : 1.0;

            var velocity = actorMovement.Speed * speedMultiplier * world.GetComponent<TimeComponent>().DeltaSeconds;
            actorTransform.X += normalized.X * velocity;
            actorTransform.Y += normalized.Y * velocity;
        }

        actorTransform.X = SimulationMath.Clamp(actorTransform.X, 18, world.GetComponent<ArenaComponent>().Width - actorTransform.Size - 18);
        actorTransform.Y = SimulationMath.Clamp(actorTransform.Y, 18, world.GetComponent<ArenaComponent>().Height - actorTransform.Size - 18);
    }
}

public class OpponentChaseSystem
{
    public void Update(Actor controlledActor)
    {
        var world = AppGlobal.World;
        var controlledCenter = controlledActor.GetComponent<TransformComponent>().Center;

        foreach (var opponent in world.QueryOpponentActors())
        {
            var opponentHealth = opponent.GetComponent<HealthComponent>();
            if (!opponentHealth.IsAlive)
            {
                continue;
            }

            var opponentTransform = opponent.GetComponent<TransformComponent>();
            var opponentMovement = opponent.GetComponent<MovementStatsComponent>();
            var direction = SimulationMath.Normalize(controlledCenter.X - opponentTransform.X, controlledCenter.Y - opponentTransform.Y);
            opponentTransform.X += direction.X * opponentMovement.Speed * world.GetComponent<TimeComponent>().DeltaSeconds;
            opponentTransform.Y += direction.Y * opponentMovement.Speed * world.GetComponent<TimeComponent>().DeltaSeconds;
        }
    }
}

public class CollisionDetectionSystem
{
    public void Update(Actor controlledActor)
    {
        var world = AppGlobal.World;
        var elapsedSeconds = world.GetComponent<TimeComponent>().ElapsedSeconds;
        var controlledTransform = controlledActor.GetComponent<TransformComponent>();
        var controlledCombat = controlledActor.GetComponent<CombatStateComponent>();
        var controlledCenter = controlledTransform.Center;

        foreach (var opponent in world.QueryOpponentActors())
        {
            var opponentHealth = opponent.GetComponent<HealthComponent>();
            if (!opponentHealth.IsAlive)
            {
                continue;
            }

            var opponentTransform = opponent.GetComponent<TransformComponent>();
            var opponentCombat = opponent.GetComponent<CombatStateComponent>();
            var collision = opponent.GetComponent<CollisionComponent>();
            var distance = SimulationMath.Distance(controlledCenter.X, controlledCenter.Y, opponentTransform.X, opponentTransform.Y);
            var contactRange = collision.Radius + controlledTransform.Size * 0.35;

            if (distance <= contactRange &&
                elapsedSeconds >= opponentCombat.ContactCooldownUntil &&
                !controlledCombat.IsInvulnerable(elapsedSeconds))
            {
                world.GetComponent<CombatEventBufferComponent>().Events.Add(new DamageEvent(
                    controlledActor.Id,
                    1,
                    elapsedSeconds + 0.16,
                    elapsedSeconds + 0.65,
                    0,
                    "collision"));

                opponentCombat.ContactCooldownUntil = elapsedSeconds + 0.70;
            }
        }
    }
}

public class DamageApplySystem
{
    public void Update(Action<string, Dictionary<string, object?>> trackEvent)
    {
        var world = AppGlobal.World;

        if (world.GetComponent<CombatEventBufferComponent>().Events.Count == 0)
        {
            return;
        }

        var pendingEvents = world.GetComponent<CombatEventBufferComponent>().Events.ToList();
        world.GetComponent<CombatEventBufferComponent>().Events.Clear();

        foreach (var combatEvent in pendingEvents)
        {
            switch (combatEvent)
            {
                case DamageEvent damage:
                    ApplyDamage(world, damage, trackEvent);
                    break;
                case ActorKilledEvent:
                    world.GetComponent<BattleStatsComponent>().Kills += 1;
                    break;
            }
        }
    }

    private static void ApplyDamage(EcsWorld world, DamageEvent damage, Action<string, Dictionary<string, object?>> trackEvent)
    {
        var targetActor = world.GetActor(damage.TargetEntityId);
        var targetHealth = targetActor.GetComponent<HealthComponent>();
        if (!targetHealth.IsAlive)
        {
            return;
        }

        var targetCombat = targetActor.GetComponent<CombatStateComponent>();
        targetHealth.Health -= damage.Amount;

        if (damage.HitFlashUntil > 0)
        {
            targetCombat.HitFlashUntil = Math.Max(targetCombat.HitFlashUntil, damage.HitFlashUntil);
        }

        if (damage.InvulnerableUntil > 0)
        {
            targetCombat.InvulnerableUntil = Math.Max(targetCombat.InvulnerableUntil, damage.InvulnerableUntil);
        }

        if (damage.ContactCooldownUntil > 0)
        {
            targetCombat.ContactCooldownUntil = Math.Max(targetCombat.ContactCooldownUntil, damage.ContactCooldownUntil);
        }

        if (targetActor.Role == ActorRole.Controlled)
        {
            trackEvent("player_hit", new Dictionary<string, object?>
            {
                ["playerHealth"] = Math.Max(targetHealth.Health, 0),
                ["enemiesRemaining"] = world.QueryOpponentActors().Count(actor => actor.GetComponent<HealthComponent>().IsAlive)
            });
        }

        if (targetHealth.IsAlive)
        {
            return;
        }

        targetHealth.Health = 0;

        if (targetActor.Role == ActorRole.Opponent)
        {
            world.GetComponent<CombatEventBufferComponent>().Events.Add(new ActorKilledEvent(targetActor.Id));
            return;
        }

        if (targetActor.Role == ActorRole.Controlled)
        {
            world.GetComponent<BattleStateComponent>().IsBattleOver = true;
            world.GetComponent<BattleStateComponent>().Outcome = "Defeat";
        }
    }
}

public class BattleOutcomeSystem
{
    public void Update(Action<string, Dictionary<string, object?>> trackEvent)
    {
        var world = AppGlobal.World;
        var battleState = world.GetComponent<BattleStateComponent>();
        var battleStats = world.GetComponent<BattleStatsComponent>();
        var elapsedSeconds = world.GetComponent<TimeComponent>().ElapsedSeconds;

        if (battleState.IsBattleOver)
        {
            if (!battleState.EndEventPublished)
            {
                battleState.EndEventPublished = true;
                trackEvent("battle_end", new Dictionary<string, object?>
                {
                    ["outcome"] = battleState.Outcome,
                    ["elapsedSeconds"] = Math.Round(elapsedSeconds, 2),
                    ["kills"] = battleStats.Kills
                });
            }

            return;
        }

        if (world.QueryOpponentActors().All(actor => !actor.GetComponent<HealthComponent>().IsAlive))
        {
            battleStats.WavesCleared = 1;
            battleState.IsBattleOver = true;
            battleState.Outcome = "Victory";
            battleState.EndEventPublished = true;

            trackEvent("battle_end", new Dictionary<string, object?>
            {
                ["outcome"] = battleState.Outcome,
                ["elapsedSeconds"] = Math.Round(elapsedSeconds, 2),
                ["kills"] = battleStats.Kills
            });
        }
    }
}

public static class SimulationMath
{
    public static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    public static SimulationVector Normalize(double x, double y)
    {
        var length = Math.Sqrt((x * x) + (y * y));
        if (length <= double.Epsilon)
        {
            return new SimulationVector(0, 0);
        }

        return new SimulationVector(x / length, y / length);
    }

    public static double Distance(double ax, double ay, double bx, double by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}

