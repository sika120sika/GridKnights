using Godot;

using System.Threading.Tasks;

namespace GridKnights.Units;

public partial class EnemyUnit : Unit
{
    private static readonly Color ColorBody    = new(0.85f, 0.2f, 0.2f);
    private static readonly Color ColorOutline = new(1.0f, 0.7f, 0.7f);
    private static readonly Color ColorDone    = new(0.45f, 0.25f, 0.25f);

    private const float HalfSize = 22f;

    public IEnemyBrain Brain { get; set; } = new RuleBasedEnemyBrain();

    public override void _Draw()
    {
        var bodyColor = ActionState == UnitActionState.Done ? ColorDone : ColorBody;
        var rect = new Rect2(-HalfSize, -HalfSize, HalfSize * 2, HalfSize * 2);
        DrawRect(rect, bodyColor);
        DrawRect(rect, ColorOutline, filled: false, width: 2f);

        var label = UnitType switch
        {
            UnitType.Goblin         => "ゴ",
            UnitType.Orc            => "オ",
            UnitType.SkeletonArcher => "骸",
            UnitType.DarkWizard     => "闇",
            _                       => "?",
        };
        DrawString(ThemeDB.FallbackFont, new Vector2(-8, 7), label, HorizontalAlignment.Left, -1, 14, Colors.White);
    }

    public async Task ExecuteTurnAsync(GridMap grid)
    {
        await Brain.TakeTurnAsync(this, grid);
        QueueRedraw();
    }

    public void RefreshDisplay()
    {
        QueueRedraw();
    }
}
