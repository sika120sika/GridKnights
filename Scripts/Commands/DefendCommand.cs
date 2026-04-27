using System.Threading.Tasks;
using GridKnights.Units;

namespace GridKnights.Commands;

public class DefendCommand : IUnitCommand
{
    private readonly Unit _unit;

    public DefendCommand(Unit unit)
    {
        _unit = unit;
    }

    public Task ExecuteAsync()
    {
        _unit.IsDefending = true;
        _unit.ActionState = UnitActionState.Done;
        _unit.RefreshDisplay();
        return Task.CompletedTask;
    }
}
