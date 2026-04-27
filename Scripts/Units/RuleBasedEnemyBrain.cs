using System.Collections.Generic;
using Godot;
using System.Threading.Tasks;
using GridKnights.Commands;

namespace GridKnights.Units;

public class RuleBasedEnemyBrain : IEnemyBrain
{
    public async Task TakeTurnAsync(EnemyUnit self, GridMap grid)
    {
        // 攻撃範囲内にプレイヤーがいれば即攻撃して終了
        if (self.CanAttack)
        {
            var targets = Pathfinder.GetAttackTargets(grid, self.GridPosition, self.AttackRange, self.Team);
            if (targets.Count > 0)
            {
                var targetUnit = grid.GetUnit(PickNearest(self.GridPosition, targets));
                if (targetUnit != null)
                    await new AttackCommand(self, targetUnit).ExecuteAsync();
                self.ActionState = UnitActionState.Done;
                return;
            }
        }

        // 最近傍プレイヤーへ向かって移動
        if (self.CanMove)
        {
            var nearestPlayer = FindNearestPlayer(self, grid);
            if (nearestPlayer != null)
            {
                var reachable = Pathfinder.GetReachableCells(grid, self.GridPosition, self.MoveRange, self.Team);
                var path = Pathfinder.FindPath(grid, self.GridPosition, nearestPlayer.GridPosition, self.Team);

                Vector2I? bestCell = null;
                foreach (var cell in path)
                    if (reachable.Contains(cell)) bestCell = cell;

                if (bestCell.HasValue && bestCell.Value != self.GridPosition)
                    await new MoveCommand(self, bestCell.Value, grid).ExecuteAsync();
            }
        }

        // 移動後に攻撃できるか再チェック
        if (self.CanAttack)
        {
            var targets = Pathfinder.GetAttackTargets(grid, self.GridPosition, self.AttackRange, self.Team);
            if (targets.Count > 0)
            {
                var targetUnit = grid.GetUnit(PickNearest(self.GridPosition, targets));
                if (targetUnit != null)
                    await new AttackCommand(self, targetUnit).ExecuteAsync();
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
