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

    private bool _isAnimating; // アニメーション中は入力を無視

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
        CallDeferred(MethodName.InitHud);
    }

    private void InitHud()
    {
        Hud.UpdateTurnLabel(_phase);
        Hud.SetEndTurnButtonVisible(true);
    }

    // --- マップ構築 ---

    private void BuildMap()
    {
        var data = MapGenerator.Generate();

        for (int x = 0; x < GridMap.GridSize; x++)
            for (int y = 0; y < GridMap.GridSize; y++)
                GridMap.SetTile(new Vector2I(x, y), data.Tiles[x, y]);

        foreach (var info in data.PlayerUnits)
        {
            var unit = (PlayerUnit)CreateUnit(info.Type, info.Team);
            GridMap.PlaceUnit(unit, info.Cell);
            unit.Defeated += OnUnitDefeated;
            _playerUnits.Add(unit);
        }

        foreach (var info in data.EnemyUnits)
        {
            var unit = (EnemyUnit)CreateUnit(info.Type, info.Team);
            GridMap.PlaceUnit(unit, info.Cell);
            unit.Defeated += OnUnitDefeated;
            _enemyUnits.Add(unit);
        }
    }

    private Unit CreateUnit(UnitType type, Team team)
    {
        Unit unit = team == Team.Player ? new PlayerUnit() : new EnemyUnit();
        unit.Team = team;
        unit.UnitType = type;
        unit.Name = Unit.GetDisplayName(type);
        var stats = Unit.GetStats(type);
        unit.MaxHp = stats.MaxHp;
        unit.Attack = stats.Attack;
        unit.MoveRange = stats.MoveRange;
        unit.AttackRange = stats.AttackRange;
        return unit;
    }

    // --- セルクリック処理 ---

    private void OnCellClicked(Vector2I cell)
    {
        if (_gameEnded || _isAnimating || _phase != TurnPhase.PlayerTurn) return;

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
        var clicked = GridMap.GetUnit(cell);

        // 敵ユニットをクリックした場合は情報表示のみ
        if (clicked is EnemyUnit enemy && enemy.IsAlive)
        {
            Hud.UpdateEnemyInfo(enemy);
            return;
        }

        var unit = clicked as PlayerUnit;
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

    // メソッドシグネチャ変更
    private async void HandleSelectMove(Vector2I cell)
    {
        if (_selectedUnit == null) return;

        // 攻撃対象セルをクリック（変更なし）
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

        // ★ 移動先をクリック：アニメーション追加
        if (_reachableCells.Contains(cell))
        {
            _isAnimating = true;
            Highlight.ClearAll(); // 移動中はハイライトを消す

            await GridMap.MoveUnitAsync(_selectedUnit, cell);

            _isAnimating = false;
            _selectedUnit.ActionState = UnitActionState.Moved;
            _selectedUnit.RefreshDisplay();

            _attackTargets = Pathfinder.GetAttackTargets(GridMap, cell, _selectedUnit.AttackRange, Team.Player);
            Highlight.ShowAttackRange(_attackTargets);
            Hud.UpdateUnitInfo(_selectedUnit);
            _inputState = PlayerInputState.SelectAttack;
            return;
        }

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

    // EndPlayerTurn 内の呼び出しも変更が必要なので注意
    public void EndPlayerTurn()
    {
        if (_gameEnded || _phase != TurnPhase.PlayerTurn) return;
        CancelSelection();
        _phase = TurnPhase.EnemyTurn;
        Hud.UpdateTurnLabel(_phase);
        Hud.SetEndTurnButtonVisible(false);
        EmitSignal(SignalName.TurnChanged, (int)_phase);
        ExecuteEnemyTurnAsync(); // ★ async版を呼ぶ（awaitしない）
    }

    private async void ExecuteEnemyTurnAsync()
    {
        _isAnimating = true;

        foreach (var enemy in _enemyUnits.ToList()) // ToList()でイテレート中の削除に備える
        {
            if (!enemy.IsAlive) continue;
            await enemy.ExecuteTurnAsync(GridMap); // ★ EnemyUnit側も変更が必要
        }

        _isAnimating = false;
        CheckWinLoss();
        if (_gameEnded) return;

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
        if (unit is PlayerUnit pu) _playerUnits.Remove(pu);
        else if (unit is EnemyUnit eu) _enemyUnits.Remove(eu);
        CheckWinLoss();
    }

    private void CheckWinLoss()
    {
        if (_gameEnded) return;
        if (_enemyUnits.Count == 0)
        {
            _gameEnded = true;
            EmitSignal(SignalName.GameCleared);
            ResultScreen.ShowClear();
        }
        else if (_playerUnits.Count == 0)
        {
            _gameEnded = true;
            EmitSignal(SignalName.GameFailed);
            ResultScreen.ShowFailed();
        }
    }
}
