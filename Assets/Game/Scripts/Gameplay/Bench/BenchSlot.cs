using UnityEngine;

public class BenchSlot : MonoBehaviour
{
    private int slotId = -1;
    private bool isOccupied = false;
    private bool isFull = false;
    private Passenger passenger = null;

    public int SlotId => slotId;
    public bool IsFull => isFull;
    public bool IsOccupied => isOccupied;
    public Passenger Passenger => passenger;

    public void Initialize(int id)
    {
        slotId = id;
    }

    public void AssignPassenger(Passenger newPassenger)
    {
        passenger = newPassenger;
        isOccupied = true;
    }

    public void PassengerArrived(Passenger newPassenger)
    {
        isFull = true;
    }

    public void ClearSlot()
    {
        if (passenger != null)
        {
            passenger = null;
        }
        isOccupied = false;
        isFull = false;
    }
}