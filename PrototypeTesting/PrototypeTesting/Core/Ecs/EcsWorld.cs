namespace PrototypeTesting.Core.Ecs;

public static class AppGlobal
{
    public static EcsWorld World { get; set; } = null!;
}

public sealed class EcsWorld : EcsNode
{
    private long _nextEntityId = 1;

    public EcsWorld() : base(0)
    {

    }

    public Dictionary<long, Actor> Actors { get; } = [];

    public long CreateEntityId() => _nextEntityId++;

    public void AddActor(Actor actor)
    {
        Actors[actor.Id] = actor;
        AddChild(actor);
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
