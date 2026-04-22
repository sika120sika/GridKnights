using System.Collections.Generic;
using Godot;

namespace GridKnights.Units;

public class RuleBasedEnemyBrain : IEnemyBrain
{
    public void TakeTurn(EnemyUnit self, GridMap grid)
    {
        // 攻撃可能な敵がいれば攻撃
        if (self.CanAttack)
        {
            var targets = Pathfinder.GetAttackTargets(grid, self.GridPosition, self.AttackRange, self.Team);
            if (targets.Count > 0)
            {
                var target = PickNearest(self.GridPosition, targets);
                var targetUnit = grid.GetUnit(target);
                targetUnit?.TakeDamage(self.Attack);
                self.ActionState = UnitActionState.Done;
                return;
            }
        }

        // 移動: 最近傍の敵プレイヤーユニットに向かって進む
        if (self.CanMove)
        {
            var nearestPlayer = FindNearestPlayer(self, grid);
            if (nearestPlayer != null)
            {
                var reachable = Pathfinder.GetReachableCells(grid, self.GridPosition, self.MoveRange, self.Team);
                var path = Pathfinder.FindPath(grid, self.GridPosition, nearestPlayer.GridPosition, self.Team);

                Vector2I? bestCell = null;
                int bestDist = int.MaxValue;

                foreach (var cell in reachable)
                {
                    // 移動先に敵チームのユニットがいなければOK（友軍はOK）
                    var occupant = grid.GetUnit(cell);
                    if (occupant != null && occupant.Team != self.Team) continue;

                    int dist = Mathf.Abs(cell.X - nearestPlayer.GridPosition.X)
                             + Mathf.Abs(cell.Y - nearestPlayer.GridPosition.Y);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestCell = cell;
                    }
                }

                if (bestCell.HasValue && bestCell.Value != self.GridPosition)
                {
                    grid.MoveUnit(self, bestCell.Value);
                    self.ActionState = UnitActionState.Moved;
                }
            }
        }

        // 移動後に攻撃できるか再チェック
        if (self.CanAttack)
        {
            var targets = Pathfinder.GetAttackTargets(grid, self.GridPosition, self.AttackRange, self.Team);
            if (targets.Count > 0)
            {
                var target = PickNearest(self.GridPosition, targets);
                var targetUnit = grid.GetUnit(target);
                targetUnit?.TakeDamage(self.Attack);
            }
        }

        self.ActionState = UnitActionState.Done;
    }

    private static Unit? FindNearestPlayer(EnemyUnit self, GridMap grid)
    {
        Unit? nearest = null;
        int bestDist = int.MaxValue;
        for (int y = 0; y < GridMap.GridSize; y++)
        {
            for (int x = 0; x < GridMap.GridSize; x++)
            {
                var unit = grid.GetUnit(new Vector2I(x, y));
                if (unit == null || unit.Team != Team.Player || !unit.IsAlive) continue;
                int dist = Mathf.Abs(x - self.GridPosition.X) + Mathf.Abs(y - self.GridPosition.Y);
                if (dist < bestDist) { bestDist = dist; nearest = unit; }
            }
        }
        return nearest;
    }

    private static Vector2I PickNearest(Vector2I origin, List<Vector2I> targets)
    {
        Vector2I best = targets[0];
        int bestDist = int.MaxValue;
        foreach (var t in targets)
        {
            int d = Mathf.Abs(t.X - origin.X) + Mathf.Abs(t.Y - origin.Y);
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }
}
