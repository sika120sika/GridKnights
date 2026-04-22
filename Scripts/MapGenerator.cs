using System;
using System.Collections.Generic;
using Godot;
using GridKnights.Units;

namespace GridKnights;

public static class MapGenerator
{
    private const int MaxAttempts = 10;
    private const float ObstacleRatio = 0.20f;

    private static readonly UnitType[] PlayerTypes = { UnitType.Swordsman, UnitType.Archer, UnitType.Mage };
    private static readonly UnitType[] EnemyTypes = {
        UnitType.Goblin, UnitType.Orc, UnitType.SkeletonArcher, UnitType.DarkWizard
    };

    public static GeneratedMap Generate(
        Func<UnitType, Team, Unit> unitFactory,
        Random? rng = null)
    {
        rng ??= new Random();

        GeneratedMap? lastResult = null;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var result = TryGenerate(unitFactory, rng);
            lastResult = result;

            var (grid, players, enemies) = result;
            if (MapValidator.IsValid(grid, players, enemies))
                return result;

            // 再試行のためにユニットを削除
            foreach (var u in players) u.QueueFree();
            foreach (var u in enemies) u.QueueFree();
        }

        // 10回全落ち → 最後の結果をそのまま使用
        return lastResult!;
    }

    private static GeneratedMap TryGenerate(Func<UnitType, Team, Unit> unitFactory, Random rng)
    {
        var grid = new GridMap();
        // UnitsLayerを追加
        var unitsLayer = new Node2D { Name = "UnitsLayer" };
        grid.AddChild(unitsLayer);

        var occupied = new HashSet<Vector2I>();

        // 障害物配置
        int obstacleCount = (int)(GridMap.GridSize * GridMap.GridSize * ObstacleRatio);
        for (int i = 0; i < obstacleCount; i++)
        {
            var cell = RandomEmpty(rng, occupied);
            grid.SetTile(cell, TileType.Obstacle);
            occupied.Add(cell);
        }

        // 自軍配置（左半分）
        var playerUnits = new List<Unit>();
        foreach (var type in PlayerTypes)
        {
            var cell = RandomEmptyInRegion(rng, occupied, 0, GridMap.GridSize / 2 - 1);
            var unit = unitFactory(type, Team.Player);
            ApplyStats(unit, type);
            grid.PlaceUnit(unit, cell);
            occupied.Add(cell);
            playerUnits.Add(unit);
        }

        // 敵配置（右半分、2〜6体）
        int enemyCount = rng.Next(2, 7);
        var enemyUnits = new List<Unit>();
        for (int i = 0; i < enemyCount; i++)
        {
            var type = EnemyTypes[rng.Next(EnemyTypes.Length)];
            var cell = RandomEmptyInRegion(rng, occupied, GridMap.GridSize / 2, GridMap.GridSize - 1);
            var unit = unitFactory(type, Team.Enemy);
            ApplyStats(unit, type);
            grid.PlaceUnit(unit, cell);
            occupied.Add(cell);
            enemyUnits.Add(unit);
        }

        return new GeneratedMap(grid, playerUnits, enemyUnits);
    }

    private static void ApplyStats(Unit unit, UnitType type)
    {
        var stats = Unit.GetStats(type);
        unit.UnitType = type;
        unit.MaxHp = stats.MaxHp;
        unit.Attack = stats.Attack;
        unit.MoveRange = stats.MoveRange;
        unit.AttackRange = stats.AttackRange;
    }

    private static Vector2I RandomEmpty(Random rng, HashSet<Vector2I> occupied)
    {
        return RandomEmptyInRegion(rng, occupied, 0, GridMap.GridSize - 1);
    }

    private static Vector2I RandomEmptyInRegion(Random rng, HashSet<Vector2I> occupied, int xMin, int xMax)
    {
        const int maxTry = 200;
        for (int i = 0; i < maxTry; i++)
        {
            var cell = new Vector2I(rng.Next(xMin, xMax + 1), rng.Next(GridMap.GridSize));
            if (!occupied.Contains(cell))
                return cell;
        }
        // フォールバック: 全探索
        for (int x = xMin; x <= xMax; x++)
            for (int y = 0; y < GridMap.GridSize; y++)
            {
                var cell = new Vector2I(x, y);
                if (!occupied.Contains(cell)) return cell;
            }
        return new Vector2I(xMin, 0);
    }
}

public record GeneratedMap(GridMap Grid, List<Unit> PlayerUnits, List<Unit> EnemyUnits);
