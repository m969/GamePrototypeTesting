namespace PrototypeTesting.Core.Ecs;

internal sealed class EcsWorld
{
    private long _nextEntityId = 1;

    public ArenaComponent Arena { get; } = new();

    public TimeComponent Time { get; } = new();

    public BattleStatsComponent BattleStats { get; } = new();

    public BattleStateComponent BattleState { get; } = new();

    public CombatCommandBufferComponent CommandBuffer { get; } = new();

    public CombatEventBufferComponent EventBuffer { get; } = new();

    public Dictionary<long, Actor> Actors { get; } = [];

    public Dictionary<long, TransformComponent> Transforms { get; } = [];

    public Dictionary<long, MovementStatsComponent> MovementStats { get; } = [];

    public Dictionary<long, HealthComponent> Healths { get; } = [];

    public Dictionary<long, CombatStateComponent> CombatStates { get; } = [];

    public Dictionary<long, CollisionComponent> Collisions { get; } = [];

    public long CreateEntityId() => _nextEntityId++;

    public void AddActor(Actor actor)
    {
        Actors[actor.Id] = actor;
    }

    public void AddChild(long parentId, EcsEntity child)
    {
        var parent = GetActor(parentId);
        parent.AddChild(child);
    }

    public TComponent AddComponent<TComponent>(long entityId, Action<TComponent>? configure = null)
        where TComponent : EcsComponent
    {
        var component = CreateComponent<TComponent>(entityId);
        configure?.Invoke(component);

        var storage = GetStorage<TComponent>();
        storage[entityId] = component;

        if (TryGetActor(entityId, out var actor))
        {
            actor.AddComponent(component);
        }

        return component;
    }

    public IEnumerable<Actor> QueryActors() => Actors.Values;

    public IEnumerable<Actor> QueryControlledActors() => QueryActors(actor => actor.Role == ActorRole.Controlled);

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

    public bool TryGetActor(long entityId, out Actor actor) => Actors.TryGetValue(entityId, out actor!);

    public IEnumerable<long> Query<TComponent>() where TComponent : EcsComponent => GetStorage<TComponent>().Keys;

    public IEnumerable<long> Query<TComponent>(Func<TComponent, bool> predicate) where TComponent : EcsComponent
    {
        foreach (var pair in GetStorage<TComponent>())
        {
            if (predicate(pair.Value))
            {
                yield return pair.Key;
            }
        }
    }

    public bool Has<TComponent>(long entityId) where TComponent : EcsComponent
    {
        if (TryGetActor(entityId, out var actor) && actor.HasComponent<TComponent>())
        {
            return true;
        }

        return GetStorage<TComponent>().ContainsKey(entityId);
    }

    public TComponent Get<TComponent>(long entityId) where TComponent : EcsComponent
    {
        if (TryGetActor(entityId, out var actor) && actor.TryGetComponent<TComponent>(out var component))
        {
            return component;
        }

        return GetStorage<TComponent>()[entityId];
    }

    public bool TryGet<TComponent>(long entityId, out TComponent component) where TComponent : EcsComponent
    {
        if (TryGetActor(entityId, out var actor) && actor.TryGetComponent<TComponent>(out component))
        {
            return true;
        }

        if (GetStorage<TComponent>().TryGetValue(entityId, out var found))
        {
            component = found;
            return true;
        }

        component = null!;
        return false;
    }

    private static TComponent CreateComponent<TComponent>(long entityId) where TComponent : EcsComponent
    {
        var created = Activator.CreateInstance(typeof(TComponent), entityId);
        if (created is not TComponent component)
        {
            throw new InvalidOperationException($"Unable to create component {typeof(TComponent).Name} for entity {entityId}.");
        }

        return component;
    }

    private Dictionary<long, TComponent> GetStorage<TComponent>() where TComponent : EcsComponent
    {
        if (typeof(TComponent) == typeof(TransformComponent)) return (Dictionary<long, TComponent>)(object)Transforms;
        if (typeof(TComponent) == typeof(MovementStatsComponent)) return (Dictionary<long, TComponent>)(object)MovementStats;
        if (typeof(TComponent) == typeof(HealthComponent)) return (Dictionary<long, TComponent>)(object)Healths;
        if (typeof(TComponent) == typeof(CombatStateComponent)) return (Dictionary<long, TComponent>)(object)CombatStates;
        if (typeof(TComponent) == typeof(CollisionComponent)) return (Dictionary<long, TComponent>)(object)Collisions;

        throw new InvalidOperationException($"Unsupported component storage: {typeof(TComponent).Name}");
    }
}
