using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Passenger : GridObject
{
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private float animDuration = 0.5f;
    [SerializeField] private float canMoveCueThickness = 0.0007f;
    [SerializeField] private float cannotMoveCueThickness = 0.0004f;
    [SerializeField] private Animator animator;
    private GridController controller;
    private PassengerColor passengerColor;
    private Vector2Int gridPosition;
    private bool isActivated;
    private bool canMove;
    private bool isMoving;
    private bool isHidden;
    private TapController tapController;
    private BenchSlot assignedBenchSlot;

    public PassengerColor Color => passengerColor;
    public Vector2Int GridPosition => gridPosition;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        tapController = GetComponent<TapController>();
        if (tapController == null)
        {
            tapController = gameObject.AddComponent<TapController>();
        }
        tapController.OnTapped.AddListener(HandleTap);
    }
    private void OnDestroy()
    {
        if (tapController != null)
        {
            tapController.OnTapped.RemoveListener(HandleTap);
            controller.OnGridUpdated -= UpdateVisuals;
        }
    }

    public void Initialize(PassengerData data, GridController ctrl)
    {
        controller = ctrl;
        controller.OnGridUpdated += UpdateVisuals;
        passengerColor = data.color;
        isHidden = data.isHidden;
        gridPosition = new Vector2Int(data.x, data.y);
        UpdateVisuals();
        if (controller is EditorGridController)
        {
            BoxCollider collider = gameObject.GetComponent<BoxCollider>();
            collider.enabled = false;
        }
    }
    public PassengerData GetPassengerData()
    {
        return new PassengerData
        {
            x = gridPosition.x,
            y = gridPosition.y,
            color = passengerColor,
            isHidden = isHidden
        };
    }

    public void UpdateVisuals()
    {
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }
        if (controller != null)
        {
            Material[] materials = skinnedMeshRenderer.materials;
            Color originalColor = GetColorFromType(passengerColor);
            Color hiddenColor = (controller is EditorGridController) ? UnityEngine.Color.Lerp(originalColor,UnityEngine.Color.black, 0.7f) : UnityEngine.Color.black;
            materials[0].color = isHidden ? hiddenColor : GetColorFromType(passengerColor);
            bool movementEnabled = controller.IsConnectedToFirstRow(gridPosition);

            if (canMove == movementEnabled) return;
            if (controller is EditorGridController) return;
            canMove = movementEnabled;
            if (canMove)
            {
                materials[0].SetFloat("_OutlineWidth", canMoveCueThickness);
                if (isHidden)
                {
                    isHidden = false;
                    UpdateVisuals();
                }
            }
            else
            {
                materials[0].SetFloat("_OutlineWidth", cannotMoveCueThickness);

            }
        }
    }

    private Color GetColorFromType(PassengerColor type)
    {
        return ColorUtility.GetColorFromType(type, controller.ColorShift);
    }

    private void HandleTap()
    {
        if (controller.LevelController.GameState != GameState.Playing)
            return;
        if (!TryActivate())
        {
            animator.SetTrigger("wave");
            SoundController.Instance.PlaySound("error");
        }
        else
            SoundController.Instance.PlaySound("pop");
    }

    private bool TryActivate()
    {
        if (!CanActivate())
            return false;

        List<Vector2Int> pathToFirstRow = controller.GetPathToFirstRow(gridPosition);
        if (pathToFirstRow == null || pathToFirstRow.Count == 0)
            return false;
        if (controller.LevelController.TryAssignToShip(this))
        {
            isActivated = true;
            controller.MoveOutPassenger(gridPosition);
            StartCoroutine(FollowPathAndExecute(pathToFirstRow, GoBoardShip));
            return true;
        }
        assignedBenchSlot = controller.LevelController.TryAssignToBenchSlot(this);
        if (assignedBenchSlot != null)
        {
            isActivated = true;
            controller.MoveOutPassenger(gridPosition);
            StartCoroutine(FollowPathAndExecute(pathToFirstRow, GoToBench));
            return true;
        }
        return false;
    }

    private IEnumerator FollowPathAndExecute(List<Vector2Int> path, Func<IEnumerator> finalAction)
    {
        isMoving = true;
        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int nextPos = path[i];
            MoveToCell(nextPos);
            yield return StartCoroutine(AnimateMovement(controller.GetWorldPosition(nextPos), animDuration));
        }
        if (finalAction == GoToBench)
        {
            controller.MovePassengerToBench(this);
        }
        yield return StartCoroutine(finalAction());

        isMoving = false;
    }

    private IEnumerator GoBoardShip()
    {
        return controller.LevelController.BoardPassenger(this);
    }

    private IEnumerator GoToBench()
    {
        yield return MoveTo(assignedBenchSlot.transform);
        assignedBenchSlot.PassengerArrived(this);
        controller.LevelController.PassengerArrivedToBench(this);
    }

    private bool CanActivate()
    {
        if (isActivated || controller?.LevelController == null || controller.LevelController.GameState != GameState.Playing)
            return false;
        return true;
    }

    public IEnumerator MoveTo(Transform transform)
    {
        return AnimateMovement(transform.position, 1f);
    }

    public IEnumerator JumpTo(Transform transform)
    {
        return AnimateMovement(transform.position, 1f, true);
    }

    public void MoveToCell(Vector2Int newPosition)
    {
        gridPosition = newPosition;
        Vector3 targetWorldPos = controller.GetWorldPosition(newPosition);
        StartCoroutine(AnimateMovement(targetWorldPos, animDuration));
    }

    private IEnumerator AnimateMovement(Vector3 targetPosition, float duration, bool jumpAction = false)
    {
        if (jumpAction)
        {
            animator.SetTrigger("jump");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            animator.SetBool("isRunning", true);
        }

        isMoving = true;
        Vector3 startPosition = transform.position;
        float elapsedTime = 0;

        Vector3 moveDirection = (targetPosition - startPosition);
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            moveDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = targetRotation;
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
        animator.SetBool("isRunning", false);
    }
}