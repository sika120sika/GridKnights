using Godot;

namespace GridKnights.HUD;

public partial class GameResultScreen : CanvasLayer
{
    private Label _messageLabel = null!;
    private Control _overlay = null!;

    public override void _Ready()
    {
        Layer = 10;
        ProcessMode = ProcessModeEnum.Always;
        BuildUi();
        _overlay.Visible = false;
    }

    private void BuildUi()
    {
        // 半透明の背景
        var bg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.75f),
        };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(bg);

        _overlay = bg;

        // ダイアログパネル（中央 420×240）
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(420, 240),
        };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.Position = new Vector2(-210, -120);
        AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        panel.AddChild(margin);

        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        margin.AddChild(vbox);

        _messageLabel = new Label
        {
            Text                  = "",
            HorizontalAlignment   = HorizontalAlignment.Center,
            AutowrapMode          = TextServer.AutowrapMode.Word,
        };
        _messageLabel.AddThemeFontSizeOverride("font_size", 30);
        vbox.AddChild(_messageLabel);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) });

        var btnRestart = new Button { Text = "もう一度プレイ" };
        btnRestart.Pressed += OnRestartPressed;
        vbox.AddChild(btnRestart);
    }

    public void ShowClear()
    {
        _messageLabel.Text = "勝利！\n敵を全滅させた！";
        _messageLabel.AddThemeColorOverride("font_color", new Color(0.3f, 1f, 0.4f));
        DisplayResult();
    }

    public void ShowFailed()
    {
        _messageLabel.Text = "敗北…\n全軍が壊滅した。";
        _messageLabel.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        DisplayResult();
    }

    private void DisplayResult()
    {
        _overlay.Visible = true;
        GetTree().Paused = true;
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
