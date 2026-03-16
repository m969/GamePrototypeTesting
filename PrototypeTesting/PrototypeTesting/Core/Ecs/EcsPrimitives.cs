namespace PrototypeTesting.Core.Ecs;

public abstract class EcsEntity
{
    protected EcsEntity(long id)
    {
        Id = id;
    }

    public long Id { get; }

    public Dictionary<Type, EcsComponent> Components { get; } = [];

    public Dictionary<long, EcsEntity> Children { get; } = [];

    public void AddComponent<TComponent>(TComponent component) where TComponent : EcsComponent
    {
        Components[component.GetType()] = component;
    }

    public bool HasComponent<TComponent>() where TComponent : EcsComponent =>
        Components.ContainsKey(typeof(TComponent));

    public TComponent GetComponent<TComponent>() where TComponent : EcsComponent =>
        (TComponent)Components[typeof(TComponent)];

    public bool TryGetComponent<TComponent>(out TComponent component) where TComponent : EcsComponent
    {
        if (Components.TryGetValue(typeof(TComponent), out var found))
        {
            component = (TComponent)found;
            return true;
        }

        component = null!;
        return false;
    }

    public void AddChild<TChild>(TChild child) where TChild : EcsEntity
    {
        Children[child.Id] = child;
    }

    public bool HasChild(long childId) => Children.ContainsKey(childId);

    public TChild GetChild<TChild>(long childId) where TChild : EcsEntity =>
        (TChild)Children[childId];

    public bool TryGetChild<TChild>(long childId, out TChild child) where TChild : EcsEntity
    {
        if (Children.TryGetValue(childId, out var found))
        {
            child = (TChild)found;
            return true;
        }

        child = null!;
        return false;
    }
}

public abstract class EcsComponent
{
    protected EcsComponent(long entityId)
    {
        EntityId = entityId;
    }

    public long EntityId { get; }
}

public abstract class EcsNode : EcsEntity
{
    protected EcsNode(long id) : base(id)
    {
    }
}