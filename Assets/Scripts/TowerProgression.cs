using UnityEngine;

public class TowerProgression : MonoBehaviour
{
    public TowerConfig config;

    public TowerFloor GetCurrentFloor(PlayerData data)
    {
        if (config == null || config.floors == null || config.floors.Count == 0) return null;
        int index = Mathf.Clamp(data.towerCurrentFloor, 0, config.floors.Count - 1);
        return config.floors[index];
    }

    public bool TryAdvanceFloor(PlayerData data)
    {
        if (config == null || config.floors == null || config.floors.Count == 0) return false;
        if (data.towerCurrentFloor >= config.floors.Count - 1) return false;
        data.towerCurrentFloor++;
        if (data.towerCurrentFloor > data.towerHighestFloorCleared)
            data.towerHighestFloorCleared = data.towerCurrentFloor;
        return true;
    }
}


