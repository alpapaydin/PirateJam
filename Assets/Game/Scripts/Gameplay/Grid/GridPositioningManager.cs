using UnityEngine;
using System.Collections;

[RequireComponent(typeof(GridController))]
public class GridPositioningManager : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float cameraTiltAngle = 45f;

    [Header("Grid Settings")]
    [SerializeField] private float screenMarginPercentage = 10f;
    [SerializeField] private float targetWorldSize = 10f;

    [Header("Alignment Settings")]
    [SerializeField] private Transform alignmentTarget;
    [SerializeField] private bool alignToTarget = true;
    [SerializeField] private Vector3 alignmentOffset = Vector3.zero;
    [SerializeField] private float finalY = 0f;

    private GridController gridController;
    private Vector2Int gridSize;
    private bool isInitialized;
    private Vector3 centeredPosition;


    private void Awake()
    {
        gridController = GetComponent<GridController>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameCamera == null)
            Debug.LogError("No camera found! Please assign a camera in the inspector.");

        SetupCamera();
    }

    private void SetupCamera()
    {
        if (gameCamera == null) return;
        gameCamera.transform.position = new Vector3(0, cameraDistance * Mathf.Sin(cameraTiltAngle * Mathf.Deg2Rad), -cameraDistance * Mathf.Cos(cameraTiltAngle * Mathf.Deg2Rad));
        gameCamera.transform.LookAt(Vector3.zero);
    }

    public void InitializeGridPosition(Vector2Int newGridSize)
    {
        gridSize = newGridSize;
        StartCoroutine(PositionGridNextFrame());
    }

    private IEnumerator PositionGridNextFrame()
    {
        yield return new WaitForEndOfFrame();
        PositionAndScaleGrid();
        isInitialized = true;
    }

    private void PositionAndScaleGrid()
    {
        if (gameCamera == null || gridSize == Vector2Int.zero) return;

        // Calculate and apply scale
        float margin = (screenMarginPercentage / 100f) * targetWorldSize;
        float maxGridDimension = Mathf.Max(gridSize.x, gridSize.y);
        float desiredScale = (targetWorldSize - (margin * 2)) / maxGridDimension;
        transform.localScale = new Vector3(desiredScale, desiredScale, desiredScale);

        // Calculate grid dimensions
        float totalWidth = gridSize.x;
        float totalDepth = gridSize.y;

        // Store centered position (accounting for 0.5f cell offset)
        centeredPosition = new Vector3(
            -(totalWidth * desiredScale) / 2,
            finalY,
            (totalDepth * desiredScale) / 2
        );

        // Apply position
        if (alignToTarget && alignmentTarget != null)
        {
            AlignWithTarget();
        }
        else
        {
            transform.position = centeredPosition;
        }
    }

    private void AlignWithTarget()
    {
        if (alignmentTarget == null) return;

        float cellScale = transform.localScale.x;

        // Calculate top-center point offset (accounting for 0.5f cell offset in GridController)
        Vector3 topCenterOffset = new Vector3(
            (gridSize.x * cellScale) / 2,    // Half width for center X
            0,                               // Same Y
            0                                // Top Z position (0 since we want top edge)
        );

        // Get target position with offset
        Vector3 targetPosition = alignmentTarget.position + alignmentOffset;

        // Calculate final position by subtracting the top-center offset
        Vector3 finalPosition = targetPosition - topCenterOffset;

        // Apply position while maintaining Y coordinate
        transform.position = new Vector3(
            finalPosition.x,
            centeredPosition.y,
            finalPosition.z
        );
    }

    public void SetAlignmentTarget(Transform target, bool align = true)
    {
        alignmentTarget = target;
        alignToTarget = align;
        if (isInitialized)
        {
            PositionAndScaleGrid();
        }
    }

    private void OnValidate()
    {
        if (isInitialized && gameCamera != null)
        {
            SetupCamera();
            PositionAndScaleGrid();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw grid bounds
        Vector3 size = new Vector3(gridSize.x * transform.localScale.x, 0.1f, gridSize.y * transform.localScale.z);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, size);

        // Draw top-center point and alignment visualization
        if (alignmentTarget != null && alignToTarget)
        {
            // Draw target point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(alignmentTarget.position + alignmentOffset, 0.2f);

            // Draw top-center point of grid
            Vector3 topCenter = transform.position + new Vector3(
                (gridSize.x * transform.localScale.x) / 2,
                0,
                0
            );
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(topCenter, 0.2f);

            // Draw line between points
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(topCenter, alignmentTarget.position + alignmentOffset);
        }
    }
#endif
}