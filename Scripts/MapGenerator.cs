using System;
using System.Collections.Generic;
using Godot;

namespace GridKnights;

public static class MapGenerator
{
    private const int MaxAttempts = 10;
    private const float ObstacleRatio = 0.20f;

    private static readonly UnitType[] PlayerTypes = { UnitType.Swordsman, UnitType.Archer, UnitType.Mage };
    private static readonly UnitType[] EnemyTypes = {
        UnitType.Goblin, UnitType.Orc, UnitType.SkeletonArcher, UnitType.DarkWizard
    };

    public static MapGenerationData Generate(Random? rng = null)
    {
        rng ??= new Random();

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var data = TryGenerate(rng);
            if (MapValidator.IsValid(data))
                return data;
        }

        return TryGenerate(rng);
    }

    private static MapGenerationData TryGenerate(Random rng)
    {
        var tiles = new TileType[GridMap.GridSize, GridMap.GridSize];
        var occupied = new HashSet<Vector2I>();

        int obstacleCount = (int)(GridMap.GridSize * GridMap.GridSize * ObstacleRatio);
        for (int i = 0; i < obstacleCount; i++)
        {
            var cell = RandomEmpty(rng, occupied);
            tiles[cell.X, cell.Y] = TileType.Obstacle;
            occupied.Add(cell);
        }

        var playerUnits = new List<UnitInfo>();
        foreach (var type in PlayerTypes)
        {
            var cell = RandomEmptyInRegion(rng, occupied, 0, GridMap.GridSize / 2 - 1);
            playerUnits.Add(new UnitInfo(type, Team.Player, cell));
            occupied.Add(cell);
        }

        int enemyCount = rng.Next(2, 7);
        var enemyUnits = new List<UnitInfo>();
        for (int i = 0; i < enemyCount; i++)
        {
            var type = EnemyTypes[rng.Next(EnemyTypes.Length)];
            var cell = RandomEmptyInRegion(rng, occupied, GridMap.GridSize / 2, GridMap.GridSize - 1);
            enemyUnits.Add(new UnitInfo(type, Team.Enemy, cell));
            occupied.Add(cell);
        }

        return new MapGenerationData(tiles, playerUnits, enemyUnits);
    }

    private static Vector2I RandomEmpty(Random rng, HashSet<Vector2I> occupied)
        => RandomEmptyInRegion(rng, occupied, 0, GridMap.GridSize - 1);

    private static Vector2I RandomEmptyInRegion(Random rng, HashSet<Vector2I> occupied, int xMin, int xMax)
    {
        const int maxTry = 200;
        for (int i = 0; i < maxTry; i++)
        {
            var cell = new Vector2I(rng.Next(xMin, xMax + 1), rng.Next(GridMap.GridSize));
            if (!occupied.Contains(cell))
                return cell;
        }
        for (int x = xMin; x <= xMax; x++)
            for (int y = 0; y < GridMap.GridSize; y++)
            {
                var cell = new Vector2I(x, y);
                if (!occupied.Contains(cell)) return cell;
            }
        return new Vector2I(xMin, 0);
    }
}

public record UnitInfo(UnitType Type, Team Team, Vector2I Cell);
public record MapGenerationData(TileType[,] Tiles, List<UnitInfo> PlayerUnits, List<UnitInfo> EnemyUnits);
