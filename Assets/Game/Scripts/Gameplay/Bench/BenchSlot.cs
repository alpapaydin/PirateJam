using UnityEngine;

public class BenchSlot : MonoBehaviour
{
    private int slotId = -1;
    private bool isOccupied = false;
    private Passenger passenger = null;

    public int SlotId => slotId;
    public bool IsOccupied => isOccupied;

    public void Initialize(int id)
    {
        slotId = id;
    }

    public void AssignPassenger(Passenger newPassenger)
    {
        passenger = newPassenger;
        isOccupied = true;
    }

    public void ClearSlot()
    {
        passenger = null;
        isOccupied = false;
    }
}