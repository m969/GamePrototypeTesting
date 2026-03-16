using PrototypeTesting.Core.Ecs;
using PrototypeTesting.Models;

namespace PrototypeTesting.Core;

public class SnapshotMapper
{
    public GameSnapshot Map(CombatSimulationState state)
    {
        var world = state.World;
        var controlledActor = world.GetActor(state.ControlledActorId);
        var controlledTransform = controlledActor.GetComponent<TransformComponent>();
        var controlledHealth = controlledActor.GetComponent<HealthComponent>();
        var controlledCombat = controlledActor.GetComponent<CombatStateComponent>();

        var time = world.GetComponent<TimeComponent>();
        var battleStats = world.GetComponent<BattleStatsComponent>();
        var battleState = world.GetComponent<BattleStateComponent>();

        return new GameSnapshot
        {
            ElapsedSeconds = time.ElapsedSeconds,
            PlayerX = controlledTransform.X,
            PlayerY = controlledTransform.Y,
            PlayerFacingX = controlledTransform.FacingX,
            PlayerFacingY = controlledTransform.FacingY,
            Attacks = battleStats.Attacks,
            Dodges = battleStats.Dodges,
            TargetHits = battleStats.TargetHits,
            TotalInputs = battleStats.TotalInputs,
            PlayerHealth = controlledHealth.Health,
            MaxPlayerHealth = controlledHealth.MaxHealth,
            EnemiesRemaining = world.QueryOpponentActors().Count(actor => actor.GetComponent<HealthComponent>().IsAlive),
            Kills = battleStats.Kills,
            WavesCleared = battleStats.WavesCleared,
            IsBattleOver = battleState.IsBattleOver,
            Outcome = battleState.Outcome,
            IsAttackActive = controlledCombat.IsAttacking(time.ElapsedSeconds),
            IsPlayerDodging = controlledCombat.IsDodging(time.ElapsedSeconds),
            IsPlayerHitFlashing = controlledCombat.IsHitFlashing(time.ElapsedSeconds),
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
                    IsHitFlashing = opponentCombat.HitFlashUntil > time.ElapsedSeconds
                };
            }).ToList()
        };
    }
}

