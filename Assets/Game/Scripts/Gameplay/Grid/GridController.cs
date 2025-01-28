using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class GridController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap gridMap;
    [SerializeField] protected GameObject validCellPrefab;
    [SerializeField] protected GameObject invalidCellPrefab;
    [SerializeField] private GameObject passengerPrefab;
    [SerializeField] private GameObject tunnelPrefab;

    [Header("Settings")]
    [SerializeField] private float cellHeight = 0.1f;

    [Header("Parent Objects")]
    [SerializeField] protected Transform cellContainer;
    [SerializeField] private Transform passengerContainer;
    public LevelController LevelController => levelController;
    public GameObject PassengerPrefab => passengerPrefab;
    public event System.Action OnGridUpdated;
    public event System.Action<Passenger> OnPassengerReachedBench;
    public int ColorShift { get; set; } = 0;

    private LevelController levelController;
    protected Dictionary<Vector2Int, Tunnel> tunnels = new Dictionary<Vector2Int, Tunnel>();
    protected Dictionary<Vector2Int, GameObject> cellObjects = new Dictionary<Vector2Int, GameObject>();
    protected Dictionary<Vector2Int, Passenger> passengers = new Dictionary<Vector2Int, Passenger>();
    protected HashSet<Vector2Int> invalidCells = new HashSet<Vector2Int>();
    protected Vector2Int gridSize;

    private void Awake()
    {
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (gridMap == null)
            Debug.LogError("Tilemap not assigned!");
        if (validCellPrefab == null)
            Debug.LogError("Valid cell prefab not assigned!");
        if (passengerPrefab == null)
            Debug.LogError("Passenger prefab not assigned!");

        if (cellContainer == null)
        {
            cellContainer = new GameObject("CellContainer").transform;
            cellContainer.parent = transform;
        }
        if (passengerContainer == null)
        {
            passengerContainer = new GameObject("PassengerContainer").transform;
            passengerContainer.parent = transform;
        }
    }

    public void InitializeGrid(LevelData levelData, LevelController controller)
    {
        levelController = controller;
        if (levelController == null)
        {
            Debug.LogError("LevelController reference not provided!");
            return;
        }
        ConstructGrid(levelData);
    }

    public virtual void ConstructGrid(LevelData levelData)
    {
        foreach (var cell in cellObjects.Values)
            if (cell != null) Destroy(cell);
        cellObjects.Clear();

        foreach (var passenger in passengers.Values)
            if (passenger != null) Destroy(passenger.gameObject);
        passengers.Clear();

        foreach (var tunnel in tunnels.Values)
            if (tunnel != null) Destroy(tunnel.gameObject);
        tunnels.Clear();

        gridSize = levelData.gridSize.ToVector2Int();
        invalidCells = new HashSet<Vector2Int>(levelData.invalidCells.Select(pos => new Vector2Int(pos.x, pos.y)));

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                bool isValid = !invalidCells.Contains(pos);
                CreateCell(pos, isValid);
            }
        }
        var positioningManager = GetComponent<GridPositioningManager>();
        if (positioningManager != null)
        {
            positioningManager.InitializeGridPosition(gridSize);
        }

        // Spawn passengers
        foreach (var passengerData in levelData.passengers)
        {
            SpawnPassenger(passengerData);
        }

        foreach (var tunnelData in levelData.tunnels)
        {
            SpawnTunnel(tunnelData);
        }
        if (this is not EditorGridController)
        {
            TriggerTunnelSpawns();
        }
        OnGridUpdated?.Invoke();
    }

    public virtual LevelData GetLevelData()
    {
        LevelData data = new LevelData();
        data.gridSize = new SerializableVector2Int(gridSize.x, gridSize.y);
        data.invalidCells = invalidCells.ToArray();
        data.passengers = passengers.Values.Select(p => p.GetPassengerData()).ToArray();
        data.tunnels = tunnels.Values.Select(t => t.GetTunnelData()).ToArray();
        return data;
    }

    private void OnDestroy()
    {
        var positioningManager = GetComponent<GridPositioningManager>();
    }

    protected void CreateCell(Vector2Int gridPos, bool isValid = true)
    {
        Vector3 worldPos = GetWorldPosition(gridPos);
        GameObject spawnPrefab = isValid ? validCellPrefab : invalidCellPrefab;
        GameObject cellObject = Instantiate(spawnPrefab, worldPos, Quaternion.identity, cellContainer);
        cellObject.name = $"Cell_{gridPos.x}_{gridPos.y}";
        CellBase cell = cellObject.GetComponent<CellBase>();
        cell.InitializeCell(gridPos, this);
        cellObjects[gridPos] = cellObject;
    }

    public void SpawnPassenger(PassengerData data)
    {
        Vector2Int gridPos = new Vector2Int(data.x, data.y);

        if (!IsValidCell(gridPos))
        {
            Debug.LogWarning($"Trying to spawn passenger at invalid position: {gridPos}");
            return;
        }

        Vector3 worldPos = GetWorldPosition(gridPos);
        worldPos.y += cellHeight;

        GameObject passengerObj = Instantiate(passengerPrefab, worldPos, Quaternion.identity, passengerContainer);
        Passenger passenger = passengerObj.GetComponent<Passenger>();

        if (passenger != null)
        {
            passenger.Initialize(data, this);
            passengers[gridPos] = passenger;
            GridTile gridTile = GetGridTileAt(gridPos);
            gridTile.GridObject = passenger;
        }
    }
    public void RegisterPassenger(Vector2Int position, Passenger passenger)
    {
        if (passengers.ContainsKey(position))
        {
            Debug.LogWarning($"Overwriting passenger at position {position}");
        }
        passengers[position] = passenger;
    }

    public GameObject SpawnPassengerObject(Vector3 position)
    {
        return Instantiate(passengerPrefab, position, Quaternion.identity, passengerContainer);
    }

    protected void SpawnTunnel(TunnelData data)
    {
        Vector2Int gridPos = new Vector2Int(data.x, data.y);

        if (!IsValidCell(gridPos))
        {
            Debug.LogWarning($"Trying to spawn tunnel at invalid position: {gridPos}");
            return;
        }

        Vector3 worldPos = GetWorldPosition(gridPos);
        GameObject tunnelObj = Instantiate(tunnelPrefab, worldPos, Quaternion.identity, transform);
        Tunnel tunnel = tunnelObj.GetComponent<Tunnel>();

        if (tunnel != null)
        {
            tunnel.Initialize(data, this);
            tunnels[gridPos] = tunnel;
            GridTile gridTile = GetGridTileAt(gridPos);
            gridTile.GridObject = tunnel;
        }
    }

    public void TriggerTunnelSpawns()
    {
        foreach (var tunnel in tunnels.Values)
        {
            tunnel.TrySpawnNextPassenger();
        }
    }

    private Vector3 GetLocalPosition(Vector2Int gridPosition)
    {
        return new Vector3(
            gridPosition.x + 0.5f,
            cellHeight / 2f,
            -(gridPosition.y + 0.5f)
        );
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        Vector3 localPos = GetLocalPosition(gridPosition);
        return transform.TransformPoint(localPos);
    }

    public bool IsValidCell(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridSize.x &&
               position.y >= 0 && position.y < gridSize.y &&
               !invalidCells.Contains(position);
    }

    public bool HasPassenger(Vector2Int position) => passengers.ContainsKey(position);
    public bool HasTunnel(Vector2Int position) => tunnels.ContainsKey(position);

    private readonly Vector2Int[] directions = new Vector2Int[]
    {
    new Vector2Int(0, 1),
    new Vector2Int(0, -1),
    new Vector2Int(1, 0),
    new Vector2Int(-1, 0)
    };
    
    public bool IsConnectedToFirstRow(Vector2Int position)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(position);
        visited.Add(position);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current.y == 0)
                return true;
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next) || !IsWalkable(next))
                    continue;
                queue.Enqueue(next);
                visited.Add(next);
            }
        }
        return false;
    }

    public List<Vector2Int> GetPathToFirstRow(Vector2Int start)
    {
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);
        bool foundPath = false;
        Vector2Int endPos = Vector2Int.zero;
        while (queue.Count > 0)
        {
            Vector2Int currentV = queue.Dequeue();
            if (currentV.y == 0)
            {
                foundPath = true;
                endPos = currentV;
                break;
            }
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = currentV + dir;
                if (visited.Contains(next) || !IsWalkable(next))
                    continue;

                queue.Enqueue(next);
                visited.Add(next);
                cameFrom[next] = currentV;
            }
        }
        if (!foundPath)
            return null;
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = endPos;
        while (!current.Equals(start))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Add(start);
        path.Reverse();

        return path;
    }

    private bool IsWalkable(Vector2Int position)
    {
        return IsValidCell(position) && !HasPassenger(position) && !HasTunnel(position);
    }

    public void MoveOutPassenger(Vector2Int from)
    {
        passengers.Remove(from);
        TriggerTunnelSpawns();
        OnGridUpdated?.Invoke();
    }

    public void MovePassengerToBench(Passenger passenger)
    {
        OnPassengerReachedBench?.Invoke(passenger);
    }

    public CellBase GetCellAt(Vector2Int position)
    {
        if (cellObjects.TryGetValue(position, out GameObject cellObject) && cellObject != null)
        {
            return cellObject.GetComponent<CellBase>();
        }
        return null;
    }

    public GridTile GetGridTileAt(Vector2Int position)
    {
        if (cellObjects.TryGetValue(position, out GameObject cellObject) && cellObject != null)
        {
            return cellObject.GetComponent<GridTile>();
        }
        return null;
    }
}