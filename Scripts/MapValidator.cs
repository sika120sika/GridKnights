using System.Collections.Generic;
using Godot;
using GridKnights.Units;

namespace GridKnights;

/// <summary>
/// マップ選別条件を担う独立クラス。
/// 将来的にコアロジックシミュレーション選別に差し替えられるよう分離している。
/// </summary>
public static class MapValidator
{
    private const int MinEnemyCount = 2;
    private const int MaxEnemyCount = 6;
    private const int MinAverageDistance = 3;

    public static bool IsValid(GridMap grid, List<Unit> playerUnits, List<Unit> enemyUnits)
    {
        if (enemyUnits.Count < MinEnemyCount || enemyUnits.Count > MaxEnemyCount)
            return false;

        // 自軍全員から敵への経路が少なくとも1本存在する
        foreach (var player in playerUnits)
        {
            bool hasAnyPath = false;
            foreach (var enemy in enemyUnits)
            {
                if (enemy.IsAlive && Pathfinder.HasPath(grid, player.GridPosition, enemy.GridPosition, Team.Player))
                {
                    hasAnyPath = true;
                    break;
                }
            }
            if (!hasAnyPath) return false;
        }

        // 自軍と敵の平均マンハッタン距離 >= MinAverageDistance
        int totalDist = 0;
        int count = 0;
        foreach (var player in playerUnits)
        {
            foreach (var enemy in enemyUnits)
            {
                totalDist += Mathf.Abs(player.GridPosition.X - enemy.GridPosition.X)
                           + Mathf.Abs(player.GridPosition.Y - enemy.GridPosition.Y);
                count++;
            }
        }
        float avgDist = count > 0 ? (float)totalDist / count : 0f;
        return avgDist >= MinAverageDistance;
    }
}
