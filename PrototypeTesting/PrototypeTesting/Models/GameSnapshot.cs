namespace PrototypeTesting.Models;

public sealed class GameSnapshot
{
    public static GameSnapshot Empty { get; } = new();

    public double ElapsedSeconds { get; set; }

    public double PlayerX { get; set; }

    public double PlayerY { get; set; }

    public double PlayerFacingX { get; set; } = 1;

    public double PlayerFacingY { get; set; }

    public int Attacks { get; set; }

    public int Dodges { get; set; }

    public int TargetHits { get; set; }

    public int TotalInputs { get; set; }

    public int PlayerHealth { get; set; } = 5;

    public int MaxPlayerHealth { get; set; } = 5;

    public int EnemiesRemaining { get; set; }

    public int Kills { get; set; }

    public int WavesCleared { get; set; }

    public bool IsBattleOver { get; set; }

    public string Outcome { get; set; } = "Running";

    public bool IsAttackActive { get; set; }

    public bool IsPlayerDodging { get; set; }

    public bool IsPlayerHitFlashing { get; set; }

    public List<EnemySnapshot> Enemies { get; set; } = [];
}
