namespace GridKnights.Units;

public interface IEnemyBrain
{
    void TakeTurn(EnemyUnit self, GridMap grid);
}
