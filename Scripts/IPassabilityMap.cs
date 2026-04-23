using Godot;

namespace GridKnights;

public interface IPassabilityMap
{
    bool IsPassable(Vector2I cell, Team movingTeam);
}
