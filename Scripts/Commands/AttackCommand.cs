using System.Threading.Tasks;
using GridKnights.HUD;
using GridKnights.Units;

namespace GridKnights.Commands;

public class AttackCommand : IUnitCommand
{
    private readonly Unit _attacker;
    private readonly Unit _target;

    public AttackCommand(Unit attacker, Unit target)
    {
        _attacker = attacker;
        _target = target;
    }

    public async Task ExecuteAsync()
    {
        var origin = _attacker.Position;
        var targetPos = GridMap.GridToWorld(_target.GridPosition);

        await _attacker.LungeForwardAsync(targetPos);
        int actualDamage = _target.TakeDamage(_attacker.Attack);
        DamagePopup.Spawn(_attacker.GetParent(), targetPos, actualDamage);
        if (_target.IsAlive) await _target.ShakeAsync();
        await _attacker.LungeReturnAsync(origin);

        _attacker.ActionState = UnitActionState.Done;
        _attacker.RefreshDisplay();
    }
}
