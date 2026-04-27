using System.Threading.Tasks;

namespace GridKnights.Commands;

public interface IUnitCommand
{
    Task ExecuteAsync();
}
