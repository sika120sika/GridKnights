using Godot;
using GridKnights.Units;

namespace GridKnights.HUD;

public partial class UnitInfoPanel : Control
{
    private Label _nameLabel = null!;
    private Label _hpLabel = null!;
    private Label _statsLabel = null!;
    private Label _stateLabel = null!;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        var bg = new PanelContainer();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        bg.AddChild(margin);

        var vbox = new VBoxContainer();
        margin.AddChild(vbox);

        _nameLabel  = new Label { Text = "—" };
        _hpLabel    = new Label { Text = "" };
        _statsLabel = new Label { Text = "" };
        _stateLabel = new Label { Text = "" };

        vbox.AddChild(_nameLabel);
        vbox.AddChild(_hpLabel);
        vbox.AddChild(_statsLabel);
        vbox.AddChild(_stateLabel);
    }

    public void ShowUnit(Unit unit, bool showState = true)
    {
        _nameLabel.Text  = Unit.GetDisplayName(unit.UnitType);
        _hpLabel.Text    = $"HP: {unit.Hp} / {unit.MaxHp}";
        _statsLabel.Text = $"攻撃:{unit.Attack}  移動:{unit.MoveRange}  射程:{unit.AttackRange}";
        _stateLabel.Text = showState ? unit.ActionState switch
        {
            UnitActionState.Idle     => "行動可能",
            UnitActionState.Moved    => "移動済（攻撃可）",
            UnitActionState.Attacked => "攻撃済",
            UnitActionState.Done     => "行動完了",
            _                        => "",
        } : "";
        Visible = true;
    }

    public void Clear()
    {
        _nameLabel.Text  = "—";
        _hpLabel.Text    = "";
        _statsLabel.Text = "";
        _stateLabel.Text = "";
    }
}
