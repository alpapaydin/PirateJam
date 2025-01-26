using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bench : MonoBehaviour
{
    [SerializeField] private float slotSpacing = 1.5f;
    [SerializeField] private int maxSlots = 5;
    [SerializeField] private GameObject benchSlotPrefab;
    [SerializeField] private Transform slotsParent;

    private List<BenchSlot> slots;

    private void Awake()
    {
        slots = new List<BenchSlot>();
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        float totalWidth = (maxSlots - 1) * slotSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObject = Instantiate(benchSlotPrefab, slotsParent);
            Vector3 position = new Vector3(startX + (i * slotSpacing), 0f, 0f);
            slotObject.transform.localPosition = position;

            BenchSlot slot = slotObject.GetComponent<BenchSlot>();
            slot.Initialize(i);
            slots.Add(slot);
        }
    }

    public BenchSlot AssignPassengerToSlot(Passenger passenger)
    {
        foreach (BenchSlot slot in slots)
        {
            if (!slot.IsOccupied)
            {
                slot.AssignPassenger(passenger);
                return slot;
            }
        }
        return null;
    }

    public List<BenchSlot> GetOccupiedSlots()
    {
        List<BenchSlot> occupiedSlots = new List<BenchSlot>();
        foreach (BenchSlot slot in slots)
        {
            if (slot.IsOccupied)
            {
                occupiedSlots.Add(slot);
            }
        }
        return occupiedSlots;
    }
    public bool IsFull()
    {
        return !slots.Any(slot => !slot.IsFull);
    }
    public void ClearSlotForPassenger(Passenger passenger)
    {
        foreach (BenchSlot slot in slots)
        {
            if (slot.Passenger == passenger)
            {
                slot.ClearSlot();
                break;
            }
        }
    }

}
