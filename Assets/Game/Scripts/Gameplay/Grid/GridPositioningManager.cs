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

    public event System.Action OnGridPositioned;

    private void PositionGrid()
    {
        PositionAndScaleGrid();
        isInitialized = true;
        OnGridPositioned?.Invoke();
    }

    private void Awake()
    {
        gridController = GetComponent<GridController>();
        if (gameCamera == null)
            gameCamera = Camera.main;
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
        gridController.gameObject.transform.position = Vector3.zero;
        gridSize = newGridSize;
        PositionGrid();
    }

    private void PositionAndScaleGrid()
    {
        if (gameCamera == null || gridSize == Vector2Int.zero) return;
        float margin = (screenMarginPercentage / 100f) * targetWorldSize;
        float maxGridDimension = Mathf.Max(gridSize.x, gridSize.y);
        float desiredScale = (targetWorldSize - (margin * 2)) / maxGridDimension;
        transform.localScale = new Vector3(desiredScale, desiredScale, desiredScale);
        float totalWidth = gridSize.x;
        float totalDepth = gridSize.y;
        centeredPosition = new Vector3(
            -(totalWidth * desiredScale) / 2,
            finalY,
            (totalDepth * desiredScale) / 2
        );
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
        Vector3 topCenterOffset = new Vector3(
            (gridSize.x * cellScale) / 2,
            0,
            0
        );
        Vector3 targetPosition = alignmentTarget.position + alignmentOffset;
        Vector3 finalPosition = targetPosition - topCenterOffset;
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
}