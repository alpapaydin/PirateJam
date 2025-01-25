using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private GridController grid;
    [SerializeField] private Bench bench;

    private void Awake()
    {
        grid.OnPassengerReachedBench += TryMoveToBench;
    }

    private void TryMoveToBench(Passenger passenger) 
    { 
        Transform benchTransform = bench.AssignPassengerToSlot(passenger);
        passenger.MoveTo(benchTransform);
        print(benchTransform.position);
    }
}
