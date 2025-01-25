using UnityEngine;

public enum PassengerColor
{
    Red,
    Green,
    Blue,
    Purple,
    Cyan,
}

public enum GameState
{
    Playing,
    Failed,
    Won,
    Paused
}

[System.Serializable]
public class LevelData
{
    public int levelNumber;
    public SerializableVector2Int gridSize;
    public Vector2Int[] invalidCells;
    public PassengerData[] passengers;
    public BusData[] busSequence;
    public float timeLimit = 60f;
    public int benchSlots = 5;
}

[System.Serializable]
public struct SerializableVector2Int
{
    public int x;
    public int y;

    public SerializableVector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, y);
    }

    public static SerializableVector2Int FromVector2Int(Vector2Int v)
    {
        return new SerializableVector2Int(v.x, v.y);
    }
}

[System.Serializable]
public class CellData
{
    public int x;
    public int y;
    public bool isValid;
}

[System.Serializable]
public class PassengerData
{
    public int x;
    public int y;
    public PassengerColor color;
}

[System.Serializable]
public class BusData
{
    public PassengerColor color;
    public int arrivalOrder;
    public int capacity = 3;
}
