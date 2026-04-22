using System.Collections.Generic;
using Godot;

namespace GridKnights.HUD;

public partial class GridHighlight : Node2D
{
    private static readonly Color ColorMove   = new(0.3f, 0.7f, 1.0f, 0.45f);
    private static readonly Color ColorAttack = new(1.0f, 0.35f, 0.2f, 0.45f);

    private HashSet<Vector2I> _moveCells   = new();
    private List<Vector2I>    _attackCells = new();

    public void ShowMovementRange(HashSet<Vector2I> cells)
    {
        _moveCells = cells;
        QueueRedraw();
    }

    public void ShowAttackRange(List<Vector2I> cells)
    {
        _attackCells = cells;
        QueueRedraw();
    }

    public void ClearAll()
    {
        _moveCells.Clear();
        _attackCells.Clear();
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var cell in _moveCells)
        {
            DrawRect(GridMap.GetCellRect(cell), ColorMove);
        }
        foreach (var cell in _attackCells)
        {
            DrawRect(GridMap.GetCellRect(cell), ColorAttack);
        }
    }
}
