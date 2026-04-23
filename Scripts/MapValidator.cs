using System.Collections.Generic;
using Godot;

namespace GridKnights;

public static class MapValidator
{
    private const int MinEnemyCount = 2;
    private const int MaxEnemyCount = 6;
    private const int MinAverageDistance = 3;

    public static bool IsValid(MapGenerationData data)
    {
        if (data.EnemyUnits.Count < MinEnemyCount || data.EnemyUnits.Count > MaxEnemyCount)
            return false;

        var grid = new ValidationGrid(data.Tiles, data.PlayerUnits, data.EnemyUnits);

        foreach (var player in data.PlayerUnits)
        {
            bool hasAnyPath = false;
            foreach (var enemy in data.EnemyUnits)
            {
                if (Pathfinder.HasPath(grid, player.Cell, enemy.Cell, Team.Player))
                {
                    hasAnyPath = true;
                    break;
                }
            }
            if (!hasAnyPath) return false;
        }

        int totalDist = 0;
        int count = 0;
        foreach (var player in data.PlayerUnits)
        {
            foreach (var enemy in data.EnemyUnits)
            {
                totalDist += Mathf.Abs(player.Cell.X - enemy.Cell.X)
                           + Mathf.Abs(player.Cell.Y - enemy.Cell.Y);
                count++;
            }
        }
        float avgDist = count > 0 ? (float)totalDist / count : 0f;
        return avgDist >= MinAverageDistance;
    }

    private sealed class ValidationGrid : IPassabilityMap
    {
        private readonly TileType[,] _tiles;
        private readonly Dictionary<Vector2I, Team> _occupants;

        public ValidationGrid(TileType[,] tiles, List<UnitInfo> players, List<UnitInfo> enemies)
        {
            _tiles = tiles;
            _occupants = new Dictionary<Vector2I, Team>();
            foreach (var u in players) _occupants[u.Cell] = u.Team;
            foreach (var u in enemies) _occupants[u.Cell] = u.Team;
        }

        public bool IsPassable(Vector2I cell, Team movingTeam)
        {
            if (_tiles[cell.X, cell.Y] == TileType.Obstacle) return false;
            return !_occupants.TryGetValue(cell, out var occupantTeam) || occupantTeam == movingTeam;
        }
    }
}
