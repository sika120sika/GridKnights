using Godot;

using System.Threading.Tasks;

namespace GridKnights.Units;
public abstract partial class Unit : Node2D
{
    [Export] public UnitType UnitType { get; set; }
    [Export] public Team Team { get; set; }
    [Export] public int MaxHp { get; set; } = 100;
    [Export] public int Attack { get; set; } = 20;
    [Export] public int MoveRange { get; set; } = 3;
    [Export] public int AttackRange { get; set; } = 1;

    public int Hp { get; private set; }
    public bool IsAlive => Hp > 0;
    public UnitActionState ActionState { get; set; } = UnitActionState.Idle;
    public Vector2I GridPosition { get; set; }

    public bool CanMove => ActionState == UnitActionState.Idle;
    public bool CanAttack => ActionState == UnitActionState.Idle || ActionState == UnitActionState.Moved;

    [Signal] public delegate void HpChangedEventHandler(int hp, int maxHp);
    [Signal] public delegate void DefeatedEventHandler(Unit unit);

    public override void _Ready()
    {
        Hp = MaxHp;
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;
        Hp = Mathf.Max(0, Hp - damage);
        EmitSignal(SignalName.HpChanged, Hp, MaxHp);
        if (Hp <= 0)
            EmitSignal(SignalName.Defeated, this);
    }

    public void ResetAction()
    {
        ActionState = UnitActionState.Idle;
    }

    private const float MoveSpeedPxPerSec = 300f; // 1秒に300px移動

    /// <summary>
    /// 指定ワールド座標へTweenでスライド移動し、完了を待機する。
    /// </summary>
    public async Task MoveToAsync(Vector2 targetWorld)
    {
        float distance = Position.DistanceTo(targetWorld);
        float duration = distance / MoveSpeedPxPerSec;

        var tween = CreateTween();
        tween.TweenProperty(this, "position", targetWorld, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        await ToSignal(tween, Tween.SignalName.Finished);
    }
    public static UnitStats GetStats(UnitType type)
    {
        return type switch
        {
            UnitType.Swordsman     => new UnitStats(120, 40, 3, 1),
            UnitType.Archer        => new UnitStats(80,  25, 3, 3),
            UnitType.Mage          => new UnitStats(60,  45, 2, 2),
            UnitType.Goblin        => new UnitStats(50,  15, 4, 1),
            UnitType.Orc           => new UnitStats(130, 40, 2, 1),
            UnitType.SkeletonArcher => new UnitStats(55, 25, 2, 3),
            UnitType.DarkWizard    => new UnitStats(55,  40, 2, 2),
            _                      => new UnitStats(80,  20, 3, 1),
        };
    }

    public static string GetDisplayName(UnitType type)
    {
        return type switch
        {
            UnitType.Swordsman      => "剣士",
            UnitType.Archer         => "弓兵",
            UnitType.Mage           => "魔法使い",
            UnitType.Goblin         => "ゴブリン",
            UnitType.Orc            => "オーク",
            UnitType.SkeletonArcher => "スケルトン弓兵",
            UnitType.DarkWizard     => "ダークウィザード",
            _                       => "不明",
        };
    }
}

public record UnitStats(int MaxHp, int Attack, int MoveRange, int AttackRange);
