using Godot;

namespace GridKnights.HUD;

public partial class DamagePopup : Node2D
{
    private int _damage;

    public static void Spawn(Node parent, Vector2 worldPosition, int damage)
    {
        var popup = new DamagePopup { _damage = damage };
        popup.Position = worldPosition + new Vector2(0, -20);
        parent.AddChild(popup);
        popup.Animate();
    }

    private async void Animate()
    {
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "position:y", Position.Y - 36, 0.65f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "modulate:a", 0f, 0.4f)
             .SetDelay(0.25f);
        await ToSignal(tween, Tween.SignalName.Finished);
        QueueFree();
    }

    public override void _Draw()
    {
        var text = _damage.ToString();
        var font = ThemeDB.FallbackFont;
        const int fontSize = 22;
        foreach (var off in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            DrawString(font, off, text, HorizontalAlignment.Center, -1, fontSize, new Color(0f, 0f, 0f, 0.85f));
        DrawString(font, Vector2.Zero, text, HorizontalAlignment.Center, -1, fontSize, new Color(1f, 0.92f, 0.2f));
    }
}
