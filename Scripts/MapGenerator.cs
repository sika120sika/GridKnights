using System;
using System.Collections.Generic;
using Godot;

namespace GridKnights;

public static class MapGenerator
{
    private const float ObstacleRatio = 0.20f;
    private const int MinEnemyCount = 2;
    private const int MaxEnemyCount = 6;
    private const int MinAverageDistance = 3;
    private const int MaxUnitPlacementAttempts = 20;

    private static readonly UnitType[] PlayerTypes = { UnitType.Swordsman, UnitType.Archer, UnitType.Mage };
    private static readonly UnitType[] EnemyTypes = {
        UnitType.Goblin, UnitType.Orc, UnitType.SkeletonArcher, UnitType.DarkWizard
    };

    public static MapGenerationData Generate(Random? rng = null)
    {
        rng ??= new Random();

        // ① ユニットを先に配置し、距離制約を満たすまでリトライ
        var (playerUnits, enemyUnits, unitCells) = PlaceUnits(rng);

        // ② 経路を壊さない範囲で障害物を埋める（フォールバック不要）
        var tiles = PlaceObstacles(rng, playerUnits, enemyUnits, unitCells);

        return new MapGenerationData(tiles, playerUnits, enemyUnits);
    }

    // 距離制約を満たすユニット配置を返す。MaxUnitPlacementAttempts 回以内に見つからなければ最後の結果を使う
    private static (List<UnitInfo> players, List<UnitInfo> enemies, HashSet<Vector2I> cells) PlaceUnits(Random rng)
    {
        for (int i = 0; i < MaxUnitPlacementAttempts; i++)
        {
            var result = TryPlaceUnits(rng);
            if (SatisfiesDistanceConstraint(result.players, result.enemies))
                return result;
        }
        return TryPlaceUnits(rng);
    }

    private static (List<UnitInfo> players, List<UnitInfo> enemies, HashSet<Vector2I> cells) TryPlaceUnits(Random rng)
    {
        var occupied = new HashSet<Vector2I>();
        var players = new List<UnitInfo>();
        var enemies = new List<UnitInfo>();

        foreach (var type in PlayerTypes)
        {
            var cell = RandomEmptyInRegion(rng, occupied, 0, GridMap.GridSize / 2 - 1);
            players.Add(new UnitInfo(type, Team.Player, cell));
            occupied.Add(cell);
        }

        int enemyCount = rng.Next(MinEnemyCount, MaxEnemyCount + 1);
        for (int i = 0; i < enemyCount; i++)
        {
            var type = EnemyTypes[rng.Next(EnemyTypes.Length)];
            var cell = RandomEmptyInRegion(rng, occupied, GridMap.GridSize / 2, GridMap.GridSize - 1);
            enemies.Add(new UnitInfo(type, Team.Enemy, cell));
            occupied.Add(cell);
        }

        return (players, enemies, occupied);
    }

    private static bool SatisfiesDistanceConstraint(List<UnitInfo> players, List<UnitInfo> enemies)
    {
        int total = 0, count = 0;
        foreach (var p in players)
            foreach (var e in enemies)
            {
                total += Mathf.Abs(p.Cell.X - e.Cell.X) + Mathf.Abs(p.Cell.Y - e.Cell.Y);
                count++;
            }
        return count > 0 && (float)total / count >= MinAverageDistance;
    }

    // 候補セルをシャッフルし、1つずつ仮置き→経路チェック→NGならロールバック
    private static TileType[,] PlaceObstacles(
        Random rng, List<UnitInfo> players, List<UnitInfo> enemies, HashSet<Vector2I> unitCells)
    {
        var tiles = new TileType[GridMap.GridSize, GridMap.GridSize];
        int target = (int)(GridMap.GridSize * GridMap.GridSize * ObstacleRatio);

        var candidates = new List<Vector2I>();
        for (int x = 0; x < GridMap.GridSize; x++)
            for (int y = 0; y < GridMap.GridSize; y++)
            {
                var cell = new Vector2I(x, y);
                if (!unitCells.Contains(cell))
                    candidates.Add(cell);
            }
        Shuffle(rng, candidates);

        int placed = 0;
        var map = new TilePassabilityMap(tiles);
        foreach (var cell in candidates)
        {
            if (placed >= target) break;

            tiles[cell.X, cell.Y] = TileType.Obstacle;
            if (AllPathsExist(map, players, enemies))
                placed++;
            else
                tiles[cell.X, cell.Y] = TileType.Empty;
        }

        return tiles;
    }

    // 障害物のみで判定（ユニット位置は除外）。ユニットは動くので配置時点の位置で経路を塞がない
    // プレイヤー全員が敵に到達でき、かつ敵全員がプレイヤーに到達できることを確認する
    private static bool AllPathsExist(TilePassabilityMap map, List<UnitInfo> players, List<UnitInfo> enemies)
    {
        foreach (var player in players)
        {
            bool hasPath = false;
            foreach (var enemy in enemies)
            {
                if (Pathfinder.HasPath(map, player.Cell, enemy.Cell, Team.Player))
                {
                    hasPath = true;
                    break;
                }
            }
            if (!hasPath) return false;
        }

        // 敵ユニットも全員プレイヤーへの経路を持つことを確認（孤立した敵の生成を防ぐ）
        foreach (var enemy in enemies)
        {
            bool hasPath = false;
            foreach (var player in players)
            {
                if (Pathfinder.HasPath(map, enemy.Cell, player.Cell, Team.Enemy))
                {
                    hasPath = true;
                    break;
                }
            }
            if (!hasPath) return false;
        }

        return true;
    }

    private sealed class TilePassabilityMap : IPassabilityMap
    {
        private readonly TileType[,] _tiles;
        public TilePassabilityMap(TileType[,] tiles) => _tiles = tiles;
        public bool IsPassable(Vector2I cell, Team _) => _tiles[cell.X, cell.Y] != TileType.Obstacle;
    }

    private static void Shuffle<T>(Random rng, List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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
