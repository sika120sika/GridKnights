using Godot;

namespace GridKnights.Units;

public partial class PlayerUnit : Unit
{
    private static readonly Color ColorBody     = new(0.25f, 0.45f, 0.95f);
    private static readonly Color ColorOutline  = new(0.8f, 0.9f, 1.0f);
    private static readonly Color ColorDone     = new(0.4f, 0.4f, 0.5f);

    private const float Radius = 22f;

    public override void _Draw()
    {
        var bodyColor = ActionState == UnitActionState.Done ? ColorDone : ColorBody;
        DrawCircle(Vector2.Zero, Radius, bodyColor);
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, ColorOutline, 2f);

        // ユニット種別の略称表示
        var label = UnitType switch
        {
            UnitType.Swordsman => "剣",
            UnitType.Archer    => "弓",
            UnitType.Mage      => "魔",
            _                  => "?",
        };
        DrawString(ThemeDB.FallbackFont, new Vector2(-8, 7), label, HorizontalAlignment.Left, -1, 14, Colors.White);
        DrawHpBar();
        if (IsDefending)
            DrawDefendOverlay();
    }

}
