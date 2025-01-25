using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class GridController : MonoBehaviour
{
    public static GridController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Tilemap gridMap;
    [SerializeField] private GameObject validCellPrefab;
    [SerializeField] private GameObject passengerPrefab;

    [Header("Settings")]
    [SerializeField] private float cellHeight = 0.1f;

    [Header("Parent Objects")]
    [SerializeField] private Transform cellContainer;
    [SerializeField] private Transform passengerContainer;

    public event System.Action OnGridUpdated;
    public event System.Action<Passenger> OnPassengerReachedBench;

    private Dictionary<Vector2Int, GameObject> cellObjects = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, Passenger> passengers = new Dictionary<Vector2Int, Passenger>();
    private HashSet<Vector2Int> invalidCells = new HashSet<Vector2Int>();
    private Vector2Int gridSize;

    private void Start()
    {
        LoadLevel(1);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

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

    public void LoadLevel(int levelNumber)
    {
        TextAsset levelFile = Resources.Load<TextAsset>($"Levels/level_{levelNumber}");
        if (levelFile == null)
        {
            Debug.LogError($"Level {levelNumber} file not found in Resources/Levels folder");
            return;
        }

        LevelData levelData = JsonUtility.FromJson<LevelData>(levelFile.text);
        InitializeGrid(levelData);
    }

    private void InitializeGrid(LevelData levelData)
    {
        // Clear existing objects
        foreach (var cell in cellObjects.Values)
            if (cell != null) Destroy(cell);
        cellObjects.Clear();

        foreach (var passenger in passengers.Values)
            if (passenger != null) Destroy(passenger.gameObject);
        passengers.Clear();

        // Set up grid data
        gridSize = levelData.gridSize.ToVector2Int();
        invalidCells = new HashSet<Vector2Int>(levelData.invalidCells.Select(pos => new Vector2Int(pos.x, pos.y)));

        // Create valid cells
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!invalidCells.Contains(pos))
                {
                    CreateCell(pos);
                }
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
    }

    private void CreateCell(Vector2Int gridPos)
    {
        Vector3 worldPos = GetWorldPosition(gridPos);
        GameObject cellObject = Instantiate(validCellPrefab, worldPos, Quaternion.identity, cellContainer);
        cellObject.name = $"Cell_{gridPos.x}_{gridPos.y}";
        cellObjects[gridPos] = cellObject;
    }

    private void SpawnPassenger(PassengerData data)
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
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        // Simple grid-to-world conversion based on local position
        return transform.position + new Vector3(
            gridPosition.x + 0.5f,
            cellHeight / 2f,
            -(gridPosition.y + 0.5f)
        );
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - transform.position;
        return new Vector2Int(
            Mathf.FloorToInt(localPos.x),
            -Mathf.FloorToInt(localPos.z)
        );
    }

    public bool IsValidCell(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridSize.x &&
               position.y >= 0 && position.y < gridSize.y &&
               !invalidCells.Contains(position);
    }

    public bool HasPassenger(Vector2Int position) => passengers.ContainsKey(position);

    public Passenger GetPassenger(Vector2Int position) =>
        passengers.TryGetValue(position, out Passenger passenger) ? passenger : null;

    private readonly Vector2Int[] directions = new Vector2Int[]
{
    new Vector2Int(0, 1),  // up
    new Vector2Int(0, -1), // down
    new Vector2Int(1, 0),  // right
    new Vector2Int(-1, 0)  // left
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

            // If we reached the first row (y = 0), return true
            if (current.y == 0)
                return true;

            // Check all adjacent cells
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;

                // Skip if we've already visited this cell or it's not walkable
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

            // If we reached the first row
            if (currentV.y == 0)
            {
                foundPath = true;
                endPos = currentV;
                break;
            }

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = currentV + dir;

                // Skip if we've already visited this cell or it's not walkable
                if (visited.Contains(next) || !IsWalkable(next))
                    continue;

                queue.Enqueue(next);
                visited.Add(next);
                cameFrom[next] = currentV;
            }
        }

        if (!foundPath)
            return null;

        // Reconstruct path
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
        return IsValidCell(position) && !HasPassenger(position);
    }

    public bool TryMovePassenger(Passenger passenger, Vector2Int from, Vector2Int to)
    {
        if (!IsValidCell(to) || !passengers.ContainsKey(from))
            return false;
        passenger.MoveToCell(to);
        return true;
    }

    public void MoveOutPassenger(Vector2Int from)
    {
        passengers.Remove(from);
        OnGridUpdated?.Invoke();
    }

    public void MovePassengerToBench(Passenger passenger)
    {
        OnPassengerReachedBench?.Invoke(passenger);
    }
}