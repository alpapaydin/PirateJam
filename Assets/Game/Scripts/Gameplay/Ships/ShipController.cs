using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    
    [SerializeField] private GameObject shipPrefab;
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private float shipSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Waypoint spawnPoint;
    [SerializeField] private Waypoint waitPoint;
    [SerializeField] private Waypoint dockPoint;
    [SerializeField] private Waypoint disappearPoint;
    [SerializeField] private List<Transform> pathPoints;


    [System.Serializable]
    public class Waypoint
    {
        public Transform point;
        public float waitAngle;
    }
    public event Action<Ship> OnShipDocked;
    public event Action<Ship> OnShipDeparted;
    public int ColorShift { get; set; } = 0;

    private Queue<ShipData> shipQueue;
    private List<Ship> shipList;
    private bool isInitialized;
    private Coroutine spawnCoroutine;
    private Ship dockedShip;

    private void Awake()
    {
        shipList = new List<Ship>();
        shipQueue = new Queue<ShipData>();
    }

    public Ship GetDockedShip()
    {
        return dockedShip;
    }

    public void InitializeShipSpawner(LevelData data)
    {
        if (isInitialized) return;

        foreach (var ship in shipList)
        {
            if (ship != null) Destroy(ship.gameObject);
        }
        shipList.Clear();
        shipQueue.Clear();

        foreach (var shipData in data.busSequence)
        {
            shipQueue.Enqueue(shipData);
        }

        spawnCoroutine = StartCoroutine(InitialSpawnSequence());
        isInitialized = true;
    }

    private IEnumerator InitialSpawnSequence()
    {
        if (shipQueue.Count > 0)
        {
            SpawnNewShip(shipQueue.Dequeue(), new[] { waitPoint, dockPoint });
            yield return new WaitForSeconds(spawnDelay);
        }

        if (shipQueue.Count > 0)
        {
            SpawnNewShip(shipQueue.Dequeue(), new[] { waitPoint });
        }
    }

    private List<Vector3> GetPathToDestination(Vector3 start, Waypoint[] waypoints)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(start);

        Vector3 lastPoint = start;
        foreach (var waypoint in waypoints)
        {
            List<Vector3> smoothPoints = GetSmoothPathBetweenPoints(lastPoint, waypoint.point.position);
            path.AddRange(smoothPoints);
            lastPoint = waypoint.point.position;
        }

        return path;
    }

    private List<Vector3> GetSmoothPathBetweenPoints(Vector3 start, Vector3 end)
    {
        List<Vector3> smoothPoints = new List<Vector3>();
        var relevantPoints = pathPoints.FindAll(p =>
        {
            Vector3 toEnd = end - start;
            Vector3 toPoint = p.position - start;
            float dot = Vector3.Dot(toEnd.normalized, toPoint.normalized);
            return dot > 0.5f && Vector3.Distance(p.position, start) < Vector3.Distance(end, start);
        });
        relevantPoints.Sort((a, b) =>
            Vector3.Distance(a.position, start).CompareTo(Vector3.Distance(b.position, start)));
        foreach (var point in relevantPoints)
        {
            smoothPoints.Add(point.position);
        }
        smoothPoints.Add(end);
        return smoothPoints;
    }

    private void SpawnNewShip(ShipData shipData, Waypoint[] targetWaypoints)
    {
        GameObject shipObj = Instantiate(shipPrefab, spawnPoint.point.position, Quaternion.Euler(0, spawnPoint.waitAngle, 0));
        Ship ship = shipObj.GetComponent<Ship>();
        ship.Initialize(shipData, this);
        ship.IsDocked = false;
        shipList.Add(ship);
        StartCoroutine(MoveShipAlongPath(ship, targetWaypoints));
    }

    private IEnumerator MoveShipAlongPath(Ship ship, Waypoint[] waypoints)
    {
        List<Vector3> pathPoints = GetPathToDestination(ship.transform.position, waypoints);

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 startPos = pathPoints[i];
            Vector3 endPos = pathPoints[i + 1];
            float journeyLength = Vector3.Distance(startPos, endPos);
            float startTime = Time.time;

            while (true)
            {
                float distanceCovered = (Time.time - startTime) * shipSpeed;
                float fractionOfJourney = distanceCovered / journeyLength;
                if (fractionOfJourney >= 1f) break;
                ship.transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
                Vector3 direction = (endPos - ship.transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    ship.transform.rotation = Quaternion.Slerp(
                        ship.transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }

                yield return null;
            }
        }
        Waypoint finalWaypoint = waypoints[waypoints.Length - 1];
        ship.transform.position = finalWaypoint.point.position;
        float finalAngle = finalWaypoint.waitAngle;
        Quaternion finalRotation = Quaternion.Euler(0, finalAngle, 0);
        while (Quaternion.Angle(ship.transform.rotation, finalRotation) > 0.1f)
        {
            ship.transform.rotation = Quaternion.Slerp(
                ship.transform.rotation,
                finalRotation,
                rotationSpeed * Time.deltaTime
            );
            yield return null;
        }
        ship.transform.rotation = finalRotation;
        ShipArrivedAt(finalWaypoint, ship);
    }

    private void ShipArrivedAt(Waypoint waypoint, Ship ship)
    {
        ship.IsDocked = true;
        if (waypoint == disappearPoint)
        {
            shipList.Remove(ship);
            Destroy(ship.gameObject);
        }
        else if (waypoint == dockPoint)
        {
            dockedShip = ship;
            OnShipDocked?.Invoke(ship);
            ship.StopJet();
        }
    }

    public void ProcessShipQueue()
    {
        bool foundWaitingShip = false;
        OnShipDeparted?.Invoke(dockedShip);
        dockedShip = null;
        for (int i = shipList.Count - 1; i >= 0; i--)
        {
            Ship ship = shipList[i];
            Vector3 shipPos = ship.transform.position;
            if (ship.IsDocked && Vector3.Distance(shipPos, dockPoint.point.position) < 0.1f)
            {
                ship.IsDocked = false;
                StartCoroutine(MoveShipAlongPath(ship, new[] { disappearPoint }));
            }
            else if (ship.IsDocked && Vector3.Distance(shipPos, waitPoint.point.position) < 0.1f)
            {
                ship.IsDocked = false;
                StartCoroutine(MoveShipAlongPath(ship, new[] { dockPoint }));
                foundWaitingShip = true;
            }
        }
        if (foundWaitingShip && shipQueue.Count > 0)
        {
            SpawnNewShip(shipQueue.Dequeue(), new[] { waitPoint });
        }
    }

    public void Cleanup()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        foreach (var ship in shipList)
        {
            if (ship != null)
            {
                Destroy(ship.gameObject);
            }
        }
        shipList.Clear();
        shipQueue.Clear();
        isInitialized = false;
    }
}