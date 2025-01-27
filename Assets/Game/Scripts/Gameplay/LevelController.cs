using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderData;

public class LevelController : MonoBehaviour
{
    [SerializeField] private GridController grid;
    [SerializeField] private Bench bench;
    [SerializeField] private ShipController shipController;
    [SerializeField] private UIController uiController;

    [SerializeField] private float boardingXOffsetRange = 1f;
    [SerializeField] private Transform boardAnchor;
    [SerializeField] private Transform shipAnchor;
    public GameState GameState => gameState;
    public Bench Bench => bench;
    public ShipController ShipController => shipController;

    private GameState gameState = GameState.Paused;
    private LevelData levelData;
    private float currentTime;
    private Coroutine timerCoroutine;

    private void Awake()
    {
        shipController.OnShipDocked += NewShipDocked;
        shipController.OnShipDeparted += ShipDeparted;
    }

    private void Start()
    {
        LoadLevel(1);
    }

    public void LoadLevel(int levelNumber)
    {
        TextAsset levelFile = Resources.Load<TextAsset>($"Levels/level_{levelNumber}");
        if (levelFile == null)
        {
            Debug.LogError($"Level {levelNumber} file not found in Resources/Levels folder");
            return;
        }
        levelData = JsonUtility.FromJson<LevelData>(levelFile.text);
        currentTime = levelData.timeLimit;
        uiController.SetLevelText(levelNumber);
        uiController.SetTimerText(currentTime);
        grid.InitializeGrid(levelData, this);
        shipController.InitializeShipSpawner(levelData);
    }

    private IEnumerator TimerCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1f); // Cache WaitForSeconds to avoid garbage collection

        while (currentTime > 0 && GameState == GameState.Playing)
        {
            yield return wait;
            currentTime--;
            uiController.SetTimerText(currentTime);

            if (currentTime <= 0)
            {
                LevelFailed();
            }
        }
    }

    public void StartTimer()
    {
        StopTimer();
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private void NewShipDocked(Ship ship)
    {
        if (ship.Data.arrivalOrder == 0)
        { 
            gameState = GameState.Playing;
            StartTimer();
        }
        
        TryBoardBenchPassengers();
        EvaluateLoseCondition();
    }

    private void ShipDeparted(Ship ship)
    {
        if (ship.Data.arrivalOrder == levelData.busSequence.Length-1)
            LevelWon();
    }

    public void PassengerArrivedToBench(Passenger passenger)
    {
        if (bench.IsFull())
            EvaluateLoseCondition();
    }

    private void EvaluateLoseCondition()
    {
        if (!bench.IsFull())
            return;
        Ship dockedShip = shipController.GetDockedShip();
        if (dockedShip == null || dockedShip.IsFull)
            return;
        LevelFailed();
    }

    public BenchSlot TryAssignToBenchSlot(Passenger passenger)
    {
        return bench.AssignPassengerToSlot(passenger);
    }

    public bool TryAssignToShip(Passenger passenger)
    {
        Ship ship = shipController.GetDockedShip();
        if (ship == null || passenger == null || !CheckCanBoardPassenger(passenger, ship))
            return false;
        ship.PassengerAssigned();
        return true;

    }

    private void TryBoardBenchPassengers()
    {
        List<BenchSlot> occupiedSlots = bench.GetOccupiedSlots();
        foreach (BenchSlot slot in occupiedSlots)
        {
            if (slot.Passenger == null) continue;
            if (TryAssignToShip(slot.Passenger))
            {
                StartCoroutine(BoardPassenger(slot.Passenger));
                slot.ClearSlot();
            }
        }
    }

    private Transform CreateOffsetAnchor(Transform baseTransform, float xOffsetRange)
    {
        Vector3 newPosition = baseTransform.position;
        newPosition.x += Random.Range(-xOffsetRange, xOffsetRange);

        GameObject tempAnchor = new GameObject($"TempAnchor_{baseTransform.name}");
        tempAnchor.transform.position = newPosition;

        return tempAnchor.transform;
    }

    public IEnumerator BoardPassenger(Passenger passenger)
    {
        Ship ship = shipController.GetDockedShip();
        if (ship == null || passenger == null)
            yield return null;
        Transform tempAnchor = CreateOffsetAnchor(boardAnchor, boardingXOffsetRange);
        yield return passenger.MoveTo(tempAnchor);
        Destroy(tempAnchor.gameObject);
        SoundController.Instance.PlaySound("pop");
        yield return passenger.JumpTo(shipAnchor);
        ship.PassengerBoarded();
        bench.ClearSlotForPassenger(passenger);
        Destroy(passenger.gameObject);
        yield return null;
    }

    private bool CheckCanBoardPassenger(Passenger passenger, Ship ship)
    {
        if (!ship.IsFull && passenger.Color == ship.Data.color)
            return true;
        return false;
    }

    private void LevelWon() 
    {
        gameState = GameState.Won;
        uiController.LevelWon();
    }
    private void LevelFailed() 
    {
        gameState = GameState.Failed;
        uiController.LevelFailed();
    }
}
