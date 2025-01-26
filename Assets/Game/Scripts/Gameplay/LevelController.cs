using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderData;

public class LevelController : MonoBehaviour
{
    [SerializeField] private GridController grid;
    [SerializeField] private Bench bench;
    [SerializeField] private ShipController shipController;

    [SerializeField] private Transform boardAnchor;
    [SerializeField] private Transform shipAnchor;

    private GameState gameState = GameState.Paused;
    private LevelData levelData;
    private List<Passenger> waitingPassengers = new List<Passenger>();

    private void Awake()
    {
        grid.OnPassengerReachedBench += PassengerReachedBench;
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

        grid.InitializeGrid(levelData);
        shipController.InitializeShipSpawner(levelData);
    }

    private void NewShipDocked(Ship ship)
    {
        if (ship.Data.arrivalOrder == 0)
            gameState = GameState.Playing;
            grid.CanInteract = true;
        TryBoardBenchPassengers();
        TryBoardWaitingPassengers();
    }

    private void ShipDeparted(Ship ship)
    {
        if (ship.Data.arrivalOrder == levelData.busSequence.Length-1)
            LevelWon();
    }

    private void PassengerReachedBench(Passenger passenger)
    {
        if (TryBoardPassenger(passenger))
            return;
        if (TryMoveToBench(passenger))
            return;

        Ship dockedShip = shipController.GetDockedShip();
        if (dockedShip != null && !dockedShip.IsFull)
        {
            LevelFailed();
        } else
            waitingPassengers.Add(passenger);
    }

    private void TryBoardBenchPassengers()
    {
        List<BenchSlot> occupiedSlots = bench.GetOccupiedSlots();
        foreach (BenchSlot slot in occupiedSlots)
        {
            if (slot.Passenger == null) continue;
            if (TryBoardPassenger(slot.Passenger))
                slot.ClearSlot();
        }
    }
    private void TryBoardWaitingPassengers()
    {
        foreach (Passenger passenger in waitingPassengers)
        {
            PassengerReachedBench(passenger);
        }
    }

    private bool TryBoardPassenger(Passenger passenger)
    {
        Ship ship = shipController.GetDockedShip();
        if (ship == null || passenger == null || !CheckCanBoardPassenger(passenger, ship))
            return false;
        ship.PassengerAssigned();
        StartCoroutine(BoardPassenger(passenger, ship));
        return true;
    }

    private IEnumerator BoardPassenger(Passenger passenger, Ship ship)
    {
        yield return passenger.MoveTo(boardAnchor);
        yield return passenger.JumpTo(shipAnchor);
        ship.PassengerBoarded();
        Destroy(passenger.gameObject);
        yield return null;
    }

    private bool CheckCanBoardPassenger(Passenger passenger, Ship ship)
    {
        if (!ship.IsFull && passenger.Color == ship.Data.color)
            return true;
        return false;
    }

    private bool TryMoveToBench(Passenger passenger) 
    { 
        Transform benchTransform = bench.AssignPassengerToSlot(passenger);
        if (benchTransform == null)
            return false;
        StartCoroutine(passenger.MoveTo(benchTransform));
        return true;
    }

    private void LevelWon() 
    {
        gameState = GameState.Won;
    }
    private void LevelFailed() 
    {
        gameState = GameState.Failed;
    }
}
