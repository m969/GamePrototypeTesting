using PrototypeTesting.Core.Ecs;
using PrototypeTesting.Models;

namespace PrototypeTesting.Core;

internal sealed class SnapshotMapper
{
    public GameSnapshot Map(CombatSimulationState state)
    {
        var world = state.World;
        var controlledActor = world.GetActor(state.ControlledActorId);
        var controlledTransform = controlledActor.GetComponent<TransformComponent>();
        var controlledHealth = controlledActor.GetComponent<HealthComponent>();
        var controlledCombat = controlledActor.GetComponent<CombatStateComponent>();

        return new GameSnapshot
        {
            ElapsedSeconds = world.Time.ElapsedSeconds,
            PlayerX = controlledTransform.X,
            PlayerY = controlledTransform.Y,
            PlayerFacingX = controlledTransform.FacingX,
            PlayerFacingY = controlledTransform.FacingY,
            Attacks = world.BattleStats.Attacks,
            Dodges = world.BattleStats.Dodges,
            TargetHits = world.BattleStats.TargetHits,
            TotalInputs = world.BattleStats.TotalInputs,
            PlayerHealth = controlledHealth.Health,
            MaxPlayerHealth = controlledHealth.MaxHealth,
            EnemiesRemaining = world.QueryOpponentActors().Count(actor => actor.GetComponent<HealthComponent>().IsAlive),
            Kills = world.BattleStats.Kills,
            WavesCleared = world.BattleStats.WavesCleared,
            IsBattleOver = world.BattleState.IsBattleOver,
            Outcome = world.BattleState.Outcome,
            IsAttackActive = controlledCombat.IsAttacking(world.Time.ElapsedSeconds),
            IsPlayerDodging = controlledCombat.IsDodging(world.Time.ElapsedSeconds),
            IsPlayerHitFlashing = controlledCombat.IsHitFlashing(world.Time.ElapsedSeconds),
            Enemies = world.QueryOpponentActors().Select(actor =>
            {
                var opponentHealth = actor.GetComponent<HealthComponent>();
                var opponentCombat = actor.GetComponent<CombatStateComponent>();
                var collision = actor.GetComponent<CollisionComponent>();
                var transform = actor.GetComponent<TransformComponent>();
                return new EnemySnapshot
                {
                    Id = actor.Id,
                    X = transform.X,
                    Y = transform.Y,
                    Radius = collision.Radius,
                    Health = Math.Max(opponentHealth.Health, 0),
                    MaxHealth = opponentHealth.MaxHealth,
                    IsAlive = opponentHealth.IsAlive,
                    IsHitFlashing = opponentCombat.HitFlashUntil > world.Time.ElapsedSeconds
                };
            }).ToList()
        };
    }
}
