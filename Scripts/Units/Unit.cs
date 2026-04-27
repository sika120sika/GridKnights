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
    public bool IsDefending { get; set; }
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

    // 実際に受けたダメージ量を返す（防御中は50%カット）
    public int TakeDamage(int damage)
    {
        if (!IsAlive) return 0;
        int actualDamage = IsDefending ? damage / 2 : damage;
        Hp = Mathf.Max(0, Hp - actualDamage);
        EmitSignal(SignalName.HpChanged, Hp, MaxHp);
        QueueRedraw();
        if (Hp <= 0)
            EmitSignal(SignalName.Defeated, this);
        return actualDamage;
    }

    public void ResetAction()
    {
        ActionState = UnitActionState.Idle;
    }

    public void RefreshDisplay()
    {
        QueueRedraw();
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

    protected void DrawHpBar()
    {
        const float barW = 44f;
        const float barH = 5f;
        float barX = -barW / 2f;
        const float barY = 26f;

        DrawRect(new Rect2(barX, barY, barW, barH), new Color(0.15f, 0.15f, 0.15f));

        float ratio = MaxHp > 0 ? (float)Hp / MaxHp : 0f;
        if (ratio > 0f)
        {
            var fillColor = ratio > 0.6f
                ? new Color(0.2f, 0.8f, 0.2f)
                : ratio > 0.3f
                    ? new Color(0.9f, 0.8f, 0.1f)
                    : new Color(0.9f, 0.2f, 0.2f);
            DrawRect(new Rect2(barX, barY, barW * ratio, barH), fillColor);
        }
    }

    // 防御中の盾アイコンをユニット右上に描画する
    protected void DrawDefendOverlay()
    {
        const float cx = 13f;
        const float cy = -19f;
        const float hw = 6f;   // 半幅
        const float ht = 7f;   // 上部高さ
        const float hb = 8f;   // 下部（三角）高さ

        var fill    = new Color(0.25f, 0.55f, 1.0f, 0.92f);
        var outline = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        var divide  = new Color(1.0f, 1.0f, 1.0f, 0.7f);

        var verts = new Vector2[]
        {
            new(cx - hw, cy - ht),   // 左上
            new(cx + hw, cy - ht),   // 右上
            new(cx + hw, cy),        // 右中
            new(cx,      cy + hb),   // 下先端
            new(cx - hw, cy),        // 左中
        };

        var colors = new Color[] { fill, fill, fill, fill, fill };
        DrawPolygon(verts, colors);

        // アウトライン
        DrawPolyline(new[] { verts[0], verts[1], verts[2], verts[3], verts[4], verts[0] }, outline, 1.5f);

        // 縦の中央線（盾紋様）
        DrawLine(new Vector2(cx, cy - ht), new Vector2(cx, cy + hb), divide, 1f);
        // 横の仕切り線
        DrawLine(new Vector2(cx - hw, cy), new Vector2(cx + hw, cy), divide, 1f);
    }

    private const float LungeRatio = 0.35f;
    private const float LungeMaxPx = 28f;

    public async Task LungeForwardAsync(Vector2 targetWorld)
    {
        var dir = (targetWorld - Position).Normalized();
        float dist = Mathf.Min(Position.DistanceTo(targetWorld) * LungeRatio, LungeMaxPx);
        var tween = CreateTween();
        tween.TweenProperty(this, "position", Position + dir * dist, 0.10f)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task LungeReturnAsync(Vector2 origin)
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "position", origin, 0.12f)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task ShakeAsync()
    {
        float ox = Position.X;
        var tween = CreateTween();
        foreach (var off in new float[] { -7f, 6f, -5f, 4f, -2f, 1f, 0f })
            tween.TweenProperty(this, "position:x", ox + off, 0.04f);
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
