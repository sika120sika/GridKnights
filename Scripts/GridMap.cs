using Godot;
using GridKnights.Units;

using System.Threading.Tasks;

namespace GridKnights;

public partial class GridMap : Node2D, IPassabilityMap
{
    public const int GridSize = 8;
    public const int CellSize = 64;

    // グリッド左上のオフセット（画面中央付近に配置）
    public static readonly Vector2 GridOrigin = new(16, 16);

    private readonly TileType[,] _tiles = new TileType[GridSize, GridSize];
    private readonly Unit?[,] _units = new Unit?[GridSize, GridSize];

    private Node2D _unitsLayer = null!;

    [Signal] public delegate void CellClickedEventHandler(Vector2I cell);

    public override void _Ready()
    {
        _unitsLayer = GetNodeOrNull<Node2D>("UnitsLayer") ?? CreateUnitsLayer();
    }

    private Node2D CreateUnitsLayer()
    {
        var layer = new Node2D { Name = "UnitsLayer" };
        AddChild(layer);
        return layer;
    }

    public override void _Draw()
    {
        for (int y = 0; y < GridSize; y++)
        {
            for (int x = 0; x < GridSize; x++)
            {
                var rect = GetCellRect(new Vector2I(x, y));
                var fillColor = _tiles[x, y] == TileType.Obstacle
                    ? new Color(0.35f, 0.25f, 0.15f)
                    : new Color(0.18f, 0.32f, 0.18f);
                DrawRect(rect, fillColor);
                DrawRect(rect, new Color(0.1f, 0.1f, 0.1f), filled: false, width: 1f);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            var cell = WorldToGrid(mb.Position - GridOrigin);
            if (IsInBounds(cell))
                EmitSignal(SignalName.CellClicked, cell);
        }
    }

    // --- タイル操作 ---

    public void SetTile(Vector2I cell, TileType type)
    {
        _tiles[cell.X, cell.Y] = type;
        QueueRedraw();
    }

    public TileType GetTile(Vector2I cell) => _tiles[cell.X, cell.Y];

    // --- ユニット操作 ---

    public void PlaceUnit(Unit unit, Vector2I cell)
    {
        _unitsLayer ??= GetNodeOrNull<Node2D>("UnitsLayer") ?? CreateUnitsLayer();
        _units[cell.X, cell.Y] = unit;
        unit.GridPosition = cell;
        unit.Position = GridToWorld(cell);
        if (!_unitsLayer.IsAncestorOf(unit))
            _unitsLayer.AddChild(unit);
    }

    // MoveUnit をデータ更新専用にする（Position設定を削除）
    public void MoveUnit(Unit unit, Vector2I to)
    {
        _units[unit.GridPosition.X, unit.GridPosition.Y] = null;
        _units[to.X, to.Y] = unit;
        unit.GridPosition = to;
        // ★ unit.Position = GridToWorld(to); を削除
    }

    // ★ 追加：アニメーション付き移動
    public async Task MoveUnitAsync(Unit unit, Vector2I to)
    {
        _units[unit.GridPosition.X, unit.GridPosition.Y] = null;
        _units[to.X, to.Y] = unit;
        unit.GridPosition = to;
        await unit.MoveToAsync(GridToWorld(to));
    }

    public void RemoveUnit(Unit unit)
    {
        var pos = unit.GridPosition;
        _units[pos.X, pos.Y] = null;
        unit.QueueFree();
    }

    public Unit? GetUnit(Vector2I cell)
    {
        if (!IsInBounds(cell)) return null;
        return _units[cell.X, cell.Y];
    }

    public TileType[,] GetTiles() => _tiles;
    public Unit?[,] GetUnits() => _units;

    // --- 座標変換 ---

    public static Vector2 GridToWorld(Vector2I cell)
    {
        return GridOrigin + new Vector2(cell.X * CellSize + CellSize / 2f, cell.Y * CellSize + CellSize / 2f);
    }

    public static Vector2I WorldToGrid(Vector2 world)
    {
        return new Vector2I((int)(world.X / CellSize), (int)(world.Y / CellSize));
    }

    public static Rect2 GetCellRect(Vector2I cell)
    {
        return new Rect2(GridOrigin + new Vector2(cell.X * CellSize, cell.Y * CellSize), new Vector2(CellSize, CellSize));
    }

    public static bool IsInBounds(Vector2I cell)
    {
        return cell.X >= 0 && cell.X < GridSize && cell.Y >= 0 && cell.Y < GridSize;
    }

    public bool IsPassable(Vector2I cell, Team movingTeam)
    {
        if (!IsInBounds(cell)) return false;
        if (_tiles[cell.X, cell.Y] == TileType.Obstacle) return false;
        var occupant = _units[cell.X, cell.Y];
        return occupant == null || occupant.Team == movingTeam;
    }
}
