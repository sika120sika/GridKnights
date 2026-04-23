using Godot;
using GridKnights.Units;

namespace GridKnights.HUD;

public partial class HUD : CanvasLayer
{
    [Export] public GameManager GameManager { get; set; } = null!;

    private Label _turnLabel = null!;
    private Button _endTurnButton = null!;
    private UnitInfoPanel _unitInfoPanel = null!;
    private Label _helpLabel = null!;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        // ターン表示（左上）
        _turnLabel = new Label
        {
            Text = "プレイヤーターン",
            Position = new Vector2(8, 8),
            CustomMinimumSize = new Vector2(300, 30),
        };
        _turnLabel.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_turnLabel);

        // ユニット情報パネル（左上、ターン表示の下）
        _unitInfoPanel = new UnitInfoPanel
        {
            Position = new Vector2(8, 44),
            CustomMinimumSize = new Vector2(220, 100),
        };
        AddChild(_unitInfoPanel);

        // ターン終了ボタン（右下）
        _endTurnButton = new Button
        {
            Text = "ターン終了",
            CustomMinimumSize = new Vector2(120, 40),
            Position = new Vector2(1024 - 140, 768 - 56),
        };
        _endTurnButton.Pressed += OnEndTurnPressed;
        AddChild(_endTurnButton);

        // 操作説明（下部）
        _helpLabel = new Label
        {
            Text = "クリックでユニット選択 → 移動先 → 攻撃対象",
            Position = new Vector2(8, 768 - 28),
            CustomMinimumSize = new Vector2(600, 24),
        };
        _helpLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        AddChild(_helpLabel);
    }

    private void OnEndTurnPressed()
    {
        GameManager?.EndPlayerTurn();
    }

    public void UpdateTurnLabel(TurnPhase phase)
    {
        _turnLabel.Text = phase == TurnPhase.PlayerTurn ? "【プレイヤーターン】" : "【敵ターン】";
        _turnLabel.AddThemeColorOverride("font_color",
            phase == TurnPhase.PlayerTurn ? new Color(0.4f, 0.8f, 1.0f) : new Color(1.0f, 0.4f, 0.4f));
    }

    public void SetEndTurnButtonVisible(bool visible)
    {
        _endTurnButton.Visible = visible;
    }

    public void UpdateUnitInfo(Unit unit)
    {
        _unitInfoPanel.ShowUnit(unit);
    }

    public void UpdateEnemyInfo(Unit unit)
    {
        _unitInfoPanel.ShowUnit(unit, showState: false);
    }

    public void ClearUnitInfo()
    {
        _unitInfoPanel.Clear();
    }
}
