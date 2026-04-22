using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GridKnights.HUD;
using GridKnights.Units;

namespace GridKnights;

/// <summary>
/// ゲーム全体のターン制御・操作ステート管理・勝敗判定を担う。
/// </summary>
public partial class GameManager : Node
{
    [Export] public GridMap GridMap { get; set; } = null!;
    [Export] public HUD.HUD Hud { get; set; } = null!;
    [Export] public GridHighlight Highlight { get; set; } = null!;
    [Export] public GameResultScreen ResultScreen { get; set; } = null!;

    [Signal] public delegate void TurnChangedEventHandler(int phase);
    [Signal] public delegate void GameClearedEventHandler();
    [Signal] public delegate void GameFailedEventHandler();

    private TurnPhase _phase = TurnPhase.PlayerTurn;
    private PlayerInputState _inputState = PlayerInputState.SelectUnit;

    private List<PlayerUnit> _playerUnits = new();
    private List<EnemyUnit> _enemyUnits = new();

    private PlayerUnit? _selectedUnit;
    private HashSet<Vector2I> _reachableCells = new();
    private List<Vector2I> _attackTargets = new();

    private bool _gameEnded;

    private enum PlayerInputState
    {
        SelectUnit,
        SelectMove,
        SelectAttack,
    }

    public override void _Ready()
    {
        GridMap.CellClicked += OnCellClicked;
        BuildMap();
        EmitSignal(SignalName.TurnChanged, (int)_phase);
        Hud.UpdateTurnLabel(_phase);
        Hud.SetEndTurnButtonVisible(true);
    }

    // --- マップ構築 ---

    private void BuildMap()
    {
        var result = MapGenerator.Generate(CreateUnit);

        // GameManager管理下にGridMapを追加する
        // (Main.tscnでGridMapはGameManagerの兄弟なのでAddChildは不要だが、生成されたユニットをGridMapへ登録)
        _playerUnits = result.PlayerUnits.OfType<PlayerUnit>().ToList();
        _enemyUnits = result.EnemyUnits.OfType<EnemyUnit>().ToList();

        foreach (var unit in _playerUnits)
        {
            unit.Defeated += OnUnitDefeated;
        }
        foreach (var unit in _enemyUnits)
        {
            unit.Defeated += OnUnitDefeated;
        }
    }

    private Unit CreateUnit(UnitType type, Team team)
    {
        Unit unit;
        if (team == Team.Player)
        {
            var pu = new PlayerUnit();
            unit = pu;
        }
        else
        {
            var eu = new EnemyUnit();
            unit = eu;
        }
        unit.Team = team;
        unit.UnitType = type;
        unit.Name = Unit.GetDisplayName(type);
        return unit;
    }

    // --- セルクリック処理 ---

    private void OnCellClicked(Vector2I cell)
    {
        if (_gameEnded || _phase != TurnPhase.PlayerTurn) return;

        switch (_inputState)
        {
            case PlayerInputState.SelectUnit:
                HandleSelectUnit(cell);
                break;
            case PlayerInputState.SelectMove:
                HandleSelectMove(cell);
                break;
            case PlayerInputState.SelectAttack:
                HandleSelectAttack(cell);
                break;
        }
    }

    private void HandleSelectUnit(Vector2I cell)
    {
        var unit = GridMap.GetUnit(cell) as PlayerUnit;
        if (unit == null || !unit.IsAlive || unit.ActionState == UnitActionState.Done) return;

        _selectedUnit = unit;
        Hud.UpdateUnitInfo(unit);

        if (unit.CanMove)
        {
            _reachableCells = Pathfinder.GetReachableCells(GridMap, cell, unit.MoveRange, Team.Player);
            _attackTargets = Pathfinder.GetAttackTargets(GridMap, cell, unit.AttackRange, Team.Player);
            Highlight.ShowMovementRange(_reachableCells);
            Highlight.ShowAttackRange(_attackTargets);
            _inputState = PlayerInputState.SelectMove;
        }
        else if (unit.CanAttack)
        {
            _reachableCells = new HashSet<Vector2I>();
            _attackTargets = Pathfinder.GetAttackTargets(GridMap, cell, unit.AttackRange, Team.Player);
            Highlight.ClearAll();
            Highlight.ShowAttackRange(_attackTargets);
            _inputState = PlayerInputState.SelectAttack;
        }
    }

    private void HandleSelectMove(Vector2I cell)
    {
        if (_selectedUnit == null) return;

        // 攻撃対象セルをクリック
        if (_attackTargets.Contains(cell))
        {
            var target = GridMap.GetUnit(cell);
            if (target != null && target.Team == Team.Enemy && target.IsAlive)
            {
                target.TakeDamage(_selectedUnit.Attack);
                _selectedUnit.ActionState = UnitActionState.Done;
                FinishUnitAction();
                return;
            }
        }

        // 移動先をクリック
        if (_reachableCells.Contains(cell))
        {
            GridMap.MoveUnit(_selectedUnit, cell);
            _selectedUnit.ActionState = UnitActionState.Moved;
            _selectedUnit.RefreshDisplay();

            // 移動後に攻撃範囲を更新
            _attackTargets = Pathfinder.GetAttackTargets(GridMap, cell, _selectedUnit.AttackRange, Team.Player);
            Highlight.ClearAll();
            Highlight.ShowAttackRange(_attackTargets);
            Hud.UpdateUnitInfo(_selectedUnit);
            _inputState = PlayerInputState.SelectAttack;
            return;
        }

        // 別ユニット選択
        CancelSelection();
        HandleSelectUnit(cell);
    }

    private void HandleSelectAttack(Vector2I cell)
    {
        if (_selectedUnit == null) return;

        if (_attackTargets.Contains(cell))
        {
            var target = GridMap.GetUnit(cell);
            if (target != null && target.Team == Team.Enemy && target.IsAlive)
            {
                target.TakeDamage(_selectedUnit.Attack);
                _selectedUnit.ActionState = UnitActionState.Done;
                _selectedUnit.RefreshDisplay();
                FinishUnitAction();
                return;
            }
        }

        // 攻撃キャンセルして選択に戻る
        CancelSelection();
    }

    private void CancelSelection()
    {
        _selectedUnit = null;
        _reachableCells.Clear();
        _attackTargets.Clear();
        Highlight.ClearAll();
        _inputState = PlayerInputState.SelectUnit;
        Hud.ClearUnitInfo();
    }

    private void FinishUnitAction()
    {
        CancelSelection();
        CheckWinLoss();
    }

    // --- ターン終了 ---

    public void EndPlayerTurn()
    {
        if (_gameEnded || _phase != TurnPhase.PlayerTurn) return;
        CancelSelection();
        _phase = TurnPhase.EnemyTurn;
        Hud.UpdateTurnLabel(_phase);
        Hud.SetEndTurnButtonVisible(false);
        EmitSignal(SignalName.TurnChanged, (int)_phase);
        ExecuteEnemyTurn();
    }

    private void ExecuteEnemyTurn()
    {
        foreach (var enemy in _enemyUnits)
        {
            if (!enemy.IsAlive) continue;
            enemy.ExecuteTurn(GridMap);
        }

        CheckWinLoss();
        if (_gameEnded) return;

        // プレイヤーターンに戻す
        foreach (var u in _playerUnits) u.ResetAction();
        foreach (var u in _playerUnits) u.RefreshDisplay();
        foreach (var u in _enemyUnits) u.ResetAction();
        foreach (var u in _enemyUnits) u.RefreshDisplay();

        _phase = TurnPhase.PlayerTurn;
        Hud.UpdateTurnLabel(_phase);
        Hud.SetEndTurnButtonVisible(true);
        EmitSignal(SignalName.TurnChanged, (int)_phase);
    }

    // --- 勝敗判定 ---

    private void OnUnitDefeated(Unit unit)
    {
        GridMap.RemoveUnit(unit);
        CheckWinLoss();
    }

    private void CheckWinLoss()
    {
        if (_gameEnded) return;
        if (_enemyUnits.All(e => !e.IsAlive))
        {
            _gameEnded = true;
            EmitSignal(SignalName.GameCleared);
            ResultScreen.ShowClear();
        }
        else if (_playerUnits.All(p => !p.IsAlive))
        {
            _gameEnded = true;
            EmitSignal(SignalName.GameFailed);
            ResultScreen.ShowFailed();
        }
    }
}
