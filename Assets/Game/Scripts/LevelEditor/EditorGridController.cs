using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class EditorGridController : GridController
{
    public System.Action<Vector2Int> OnTileSelected;
    private Dictionary<Vector2Int, PassengerData> editorPassengers = new Dictionary<Vector2Int, PassengerData>();
    private Dictionary<Vector2Int, TunnelData> editorTunnels = new Dictionary<Vector2Int, TunnelData>();
    private HashSet<Vector2Int> editorInvalidCells = new HashSet<Vector2Int>();
    private LevelData currentLevelData;
    public bool IsInEditMode = false;

    public override LevelData GetLevelData()
    {
        LevelData data = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(currentLevelData));
        data.invalidCells = editorInvalidCells.ToArray();
        data.passengers = editorPassengers.Values.ToArray();
        data.tunnels = editorTunnels.Values.ToArray();
        return data;
    }
    public override void ConstructGrid(LevelData levelData)
    {
        editorPassengers.Clear();
        editorTunnels.Clear();
        editorInvalidCells.Clear();
        foreach (var cell in cellObjects.Values)
            if (cell != null) Destroy(cell);
        cellObjects.Clear();

        foreach (var passenger in passengers.Values)
            if (passenger != null) Destroy(passenger.gameObject);
        passengers.Clear();

        foreach (var tunnel in tunnels.Values)
            if (tunnel != null) Destroy(tunnel.gameObject);
        tunnels.Clear();
        currentLevelData = levelData;
        editorInvalidCells = new HashSet<Vector2Int>(
            levelData.invalidCells.Select(pos => new Vector2Int(pos.x, pos.y)));

        editorPassengers = levelData.passengers.ToDictionary(
            p => new Vector2Int(p.x, p.y), p => p);

        editorTunnels = levelData.tunnels.ToDictionary(
            t => new Vector2Int(t.x, t.y), t => t);

        base.ConstructGrid(levelData);
    }

    public void SetTileType(Vector2Int position, TileType type)
    {
        editorPassengers.Remove(position);
        editorTunnels.Remove(position);
        editorInvalidCells.Remove(position);
        switch (type)
        {
            case TileType.Invalid:
                editorInvalidCells.Add(position);
                break;

            case TileType.Empty:
                break;

            case TileType.Passenger:
                var passengerData = new PassengerData
                {
                    x = position.x,
                    y = position.y,
                    color = PassengerColor.Red,
                    isHidden = false
                };
                editorPassengers[position] = passengerData;
                break;

            case TileType.Tunnel:
                var tunnelData = new TunnelData
                {
                    x = position.x,
                    y = position.y,
                    orientation = 0,
                    passengers = new TunnelPassenger[]
                    {
                    new TunnelPassenger { color = PassengerColor.Blue },
                    new TunnelPassenger { color = PassengerColor.Red }
                    }
                };
                editorTunnels[position] = tunnelData;
                break;
        }
        ConstructGrid(GetLevelData());
        CellBase cell = GetCellAt(position);
        if (cell != null)
        {
            cell.CellSelected();
        }
    }

    public void SetPassengerHidden(Vector2Int position, bool isHidden)
    {
        if (editorPassengers.TryGetValue(position, out PassengerData passenger))
        {
            passenger.isHidden = isHidden;
            ConstructGrid(GetLevelData());
        }
    }

    public void SetPassengerColor(Vector2Int position, PassengerColor color)
    {
        if (editorPassengers.TryGetValue(position, out PassengerData passenger))
        {
            passenger.color = color;
            ConstructGrid(GetLevelData());
        }
    }

    public void SetTunnelOrientation(Vector2Int position, int orientation)
    {
        if (editorTunnels.TryGetValue(position, out TunnelData tunnel))
        {
            tunnel.orientation = orientation;
            ConstructGrid(GetLevelData());
        }
    }

    public void SetTunnelPassengers(Vector2Int position, TunnelPassenger[] passengers)
    {
        if (editorTunnels.TryGetValue(position, out TunnelData tunnel))
        {
            tunnel.passengers = passengers;
            ConstructGrid(GetLevelData());
        }
    }

    public void OnTileClicked(Vector2Int position)
    {
        OnTileSelected?.Invoke(position);
    }
}