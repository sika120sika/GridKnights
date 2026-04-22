using System.Collections.Generic;
using Godot;

namespace GridKnights;

public static class Pathfinder
{
    private static readonly Vector2I[] _dirs = {
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
    };

    /// <summary>BFSで移動可能なセルを列挙する。移動元は含まない。</summary>
    public static HashSet<Vector2I> GetReachableCells(GridMap grid, Vector2I start, int moveRange, Team movingTeam)
    {
        var visited = new Dictionary<Vector2I, int> { [start] = 0 };
        var queue = new Queue<Vector2I>();
        queue.Enqueue(start);
        var result = new HashSet<Vector2I>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int cost = visited[current];
            if (cost >= moveRange) continue;

            foreach (var dir in _dirs)
            {
                var next = current + dir;
                if (!GridMap.IsInBounds(next)) continue;
                if (visited.ContainsKey(next)) continue;
                // 障害物はスキップ、敵チームのユニットがいるセルは通過不可
                if (!grid.IsPassable(next, movingTeam)) continue;
                // 敵ユニットが占有するセルは移動先にできない
                var occupant = grid.GetUnit(next);
                if (occupant != null && occupant.Team != movingTeam) continue;

                visited[next] = cost + 1;
                result.Add(next);
                queue.Enqueue(next);
            }
        }

        return result;
    }

    /// <summary>攻撃範囲内（マンハッタン距離）の敵ユニットセルを返す。</summary>
    public static List<Vector2I> GetAttackTargets(GridMap grid, Vector2I pos, int attackRange, Team attackerTeam)
    {
        var result = new List<Vector2I>();
        for (int y = 0; y < GridMap.GridSize; y++)
        {
            for (int x = 0; x < GridMap.GridSize; x++)
            {
                var cell = new Vector2I(x, y);
                int dist = Mathf.Abs(cell.X - pos.X) + Mathf.Abs(cell.Y - pos.Y);
                if (dist < 1 || dist > attackRange) continue;
                var unit = grid.GetUnit(cell);
                if (unit != null && unit.Team != attackerTeam && unit.IsAlive)
                    result.Add(cell);
            }
        }
        return result;
    }

    /// <summary>startからendへの経路が存在するか（BFS）。MapValidator用。</summary>
    public static bool HasPath(GridMap grid, Vector2I start, Vector2I end, Team movingTeam)
    {
        if (start == end) return true;
        var visited = new HashSet<Vector2I> { start };
        var queue = new Queue<Vector2I>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dir in _dirs)
            {
                var next = current + dir;
                if (!GridMap.IsInBounds(next)) continue;
                if (visited.Contains(next)) continue;
                if (next != end && !grid.IsPassable(next, movingTeam)) continue;
                if (next == end) return true;
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return false;
    }

    /// <summary>BFSで最短経路を返す（移動AIに使用）。経路なしの場合は空リスト。</summary>
    public static List<Vector2I> FindPath(GridMap grid, Vector2I start, Vector2I end, Team movingTeam)
    {
        if (start == end) return new List<Vector2I>();
        var prev = new Dictionary<Vector2I, Vector2I> { [start] = start };
        var queue = new Queue<Vector2I>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == end) break;
            foreach (var dir in _dirs)
            {
                var next = current + dir;
                if (!GridMap.IsInBounds(next)) continue;
                if (prev.ContainsKey(next)) continue;
                if (next != end && !grid.IsPassable(next, movingTeam)) continue;
                prev[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!prev.ContainsKey(end)) return new List<Vector2I>();

        var path = new List<Vector2I>();
        var node = end;
        while (node != start)
        {
            path.Add(node);
            node = prev[node];
        }
        path.Reverse();
        return path;
    }
}
