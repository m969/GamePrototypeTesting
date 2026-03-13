namespace PrototypeTesting.Models;

public sealed class EnemySnapshot
{
    public long Id { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Radius { get; set; }

    public int Health { get; set; }

    public int MaxHealth { get; set; }

    public bool IsAlive { get; set; }

    public bool IsHitFlashing { get; set; }
}
