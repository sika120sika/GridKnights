using System.Threading.Tasks;
using Godot;
using GridKnights.Units;

namespace GridKnights.Commands;

public class MoveCommand : IUnitCommand
{
    private readonly Unit _unit;
    private readonly Vector2I _to;
    private readonly GridMap _grid;

    public MoveCommand(Unit unit, Vector2I to, GridMap grid)
    {
        _unit = unit;
        _to = to;
        _grid = grid;
    }

    public async Task ExecuteAsync()
    {
        await _grid.MoveUnitAsync(_unit, _to);
        _unit.ActionState = UnitActionState.Moved;
        _unit.RefreshDisplay();
    }
}
