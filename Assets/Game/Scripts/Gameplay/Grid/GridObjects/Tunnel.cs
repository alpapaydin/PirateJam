using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class Tunnel : GridObject
{
    [SerializeField] private TextMeshPro countText;
    private GridController gridController;
    private Vector2Int gridPosition;
    private Vector2Int exitDirection;
    private Queue<TunnelPassenger> passengerQueue = new Queue<TunnelPassenger>();
    private bool isSpawning = false;

    public void Initialize(TunnelData data, GridController controller)
    {
        gridController = controller;
        gridPosition = new Vector2Int(data.x, data.y);
        switch (data.orientation)
        {
            case 0:
                exitDirection = new Vector2Int(0, -1);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case 1:
                exitDirection = new Vector2Int(1, 0);
                transform.rotation = Quaternion.Euler(0, 90, 0);
                break;
            case 2:
                exitDirection = new Vector2Int(0, 1);
                transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
            case 3:
                exitDirection = new Vector2Int(-1, 0);
                transform.rotation = Quaternion.Euler(0, 270, 0);
                break;
        }
        if (data.passengers != null)
        {
            foreach (var passenger in data.passengers)
            {
                passengerQueue.Enqueue(passenger);
            }
            UpdateCountText(passengerQueue.Count);
        }
    }

    public enum Orientation
    {
        Up, Down, Left, Right
    }

    public TunnelData GetTunnelData()
    {
        return new TunnelData
        {
            x = gridPosition.x,
            y = gridPosition.y,
            orientation = GetOrientationFromDirection(exitDirection),
            passengers = passengerQueue.ToArray()
        };
    }

    private int GetOrientationFromDirection(Vector2Int direction)
    {
        if (direction == new Vector2Int(0, -1)) return 0;
        if (direction == new Vector2Int(1, 0)) return 1;
        if (direction == new Vector2Int(0, 1)) return 2;
        if (direction == new Vector2Int(-1, 0)) return 3;
        return 0;
    }

    public bool TrySpawnNextPassenger()
    {
        if (isSpawning || passengerQueue.Count == 0)
            return false;
        Vector2Int exitPosition = gridPosition + exitDirection;
        if (!gridController.IsValidCell(exitPosition) || gridController.HasPassenger(exitPosition))
            return false;
        StartCoroutine(SpawnPassengerSequence(exitPosition));
        return true;
    }

    private IEnumerator SpawnPassengerSequence(Vector2Int exitPosition)
    {
        isSpawning = true;
        TunnelPassenger nextPassenger = passengerQueue.Dequeue();
        UpdateCountText(passengerQueue.Count);
        Vector3 spawnWorldPos = gridController.GetWorldPosition(gridPosition);
        spawnWorldPos.y += 0.1f;
        GameObject passengerObj = gridController.SpawnPassengerObject(spawnWorldPos);
        passengerObj.transform.rotation = transform.rotation;
        Passenger passenger = passengerObj.GetComponent<Passenger>();
        if (passenger != null)
        {
            PassengerData passengerData = new PassengerData
            {
                x = exitPosition.x,
                y = exitPosition.y,
                color = nextPassenger.color,
                isHidden = false
            };

            passenger.Initialize(passengerData, gridController);
            gridController.RegisterPassenger(exitPosition, passenger);
            passenger.MoveToCell(exitPosition);
            while (passenger.IsMoving)
            {
                yield return null;
            }
        }

        isSpawning = false;
    }

    private void UpdateCountText(int count)
    {
        if (count == 0)
        {
            countText.gameObject.SetActive(false);
        }
        countText.text = count.ToString();
    }

    public int RemainingPassengers => passengerQueue.Count;
    public bool IsSpawning => isSpawning;
}