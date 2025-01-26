using System.Collections;
using System.Collections.Generic;
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

    public Transform AssignPassengerToSlot(Passenger passenger)
    {
        foreach (BenchSlot slot in slots)
        {
            if (!slot.IsOccupied)
            {
                slot.AssignPassenger(passenger);
                return slot.transform;
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

    public void ClearSlot(int slotId)
    {
        if (slotId >= 0 && slotId < slots.Count)
        {
            slots[slotId].ClearSlot();
        }
    }

    public bool IsSlotAvailable(int slotId)
    {
        return slotId >= 0 && slotId < slots.Count && !slots[slotId].IsOccupied;
    }
}
