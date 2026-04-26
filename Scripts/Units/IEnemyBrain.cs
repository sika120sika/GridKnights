using System.Threading.Tasks;

namespace GridKnights.Units;

public interface IEnemyBrain
{
    Task TakeTurnAsync(EnemyUnit self, GridMap grid);
}
