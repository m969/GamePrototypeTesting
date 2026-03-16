namespace PrototypeTesting.Core.Ecs;

public static class AppGlobal
{
    public static EcsWorld World { get; set; }
}

public sealed class EcsWorld
{
    private long _nextEntityId = 1;

    public ArenaComponent Arena { get; } = new();

    public TimeComponent Time { get; } = new();

    public BattleStatsComponent BattleStats { get; } = new();

    public BattleStateComponent BattleState { get; } = new();

    public CombatCommandBufferComponent CommandBuffer { get; } = new();

    public CombatEventBufferComponent EventBuffer { get; } = new();

    public Dictionary<long, Actor> Actors { get; } = [];

    public long CreateEntityId() => _nextEntityId++;

    public void AddActor(Actor actor)
    {
        Actors[actor.Id] = actor;
    }

    public IEnumerable<Actor> QueryOpponentActors() => QueryActors(actor => actor.Role == ActorRole.Opponent);

    public IEnumerable<Actor> QueryActors(Func<Actor, bool> predicate)
    {
        foreach (var actor in Actors.Values)
        {
            if (predicate(actor))
            {
                yield return actor;
            }
        }
    }

    public Actor GetActor(long entityId) => Actors[entityId];
}
