namespace GridKnights;

public enum UnitActionState
{
    Idle,     // 未行動
    Moved,    // 移動済み（攻撃はまだ可能）
    Attacked, // 攻撃済み（行動完了）
    Done,     // 行動完了
}
