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
    private TapController tapController;

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

    public void Initialize(PassengerData data, GridController ctrl)
    {
        controller = ctrl;
        controller.OnGridUpdated += UpdateWalkableCue;
        passengerColor = data.color;
        gridPosition = new Vector2Int(data.x, data.y);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.materials.Length > 0)
        {
            Material[] materials = skinnedMeshRenderer.materials;
            materials[0].color = GetColorFromType(passengerColor);
            skinnedMeshRenderer.materials = materials;
            UpdateWalkableCue();
        }
        else
        {
            Debug.LogError("SkinnedMeshRenderer or materials not found on passenger!");
        }
    }

    public void UpdateWalkableCue()
    {
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }
        if (controller != null)
        {
            Material[] materials = skinnedMeshRenderer.materials;
            materials[0].color = GetColorFromType(passengerColor);
            bool movementEnabled = controller.IsConnectedToFirstRow(gridPosition);

            if (canMove == movementEnabled) { return; }
            canMove = movementEnabled;
            if (canMove)
            {
                materials[0].SetFloat("_OutlineWidth", canMoveCueThickness);
            }
            else
            {
                materials[0].SetFloat("_OutlineWidth", cannotMoveCueThickness);

            }
        }
    }

    private Color GetColorFromType(PassengerColor type)
    {
        return ColorUtility.GetColorFromType(type);
    }

    public void HandleTap()
    {
        if (!controller.CanInteract)
            return;

        if (isActivated)
            return;

        List<Vector2Int> pathToFirstRow = controller.GetPathToFirstRow(gridPosition);
        if (pathToFirstRow != null && pathToFirstRow.Count > 0)
        {
            isActivated = true;
            controller.MoveOutPassenger(gridPosition);
            StartCoroutine(FollowPath(pathToFirstRow));
        }
    }

    private IEnumerator FollowPath(List<Vector2Int> path)
    {
        isMoving = true;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int nextPos = path[i];
            MoveToCell(nextPos);
            yield return StartCoroutine(AnimateMovement(controller.GetWorldPosition(nextPos), animDuration));
        }
        controller.MovePassengerToBench(this);
        isMoving = false;
    }

    public void MoveToCell(Vector2Int newPosition)
    {
        gridPosition = newPosition;
        StartCoroutine(AnimateMovement(controller.GetWorldPosition(newPosition), animDuration));
    }
    public IEnumerator MoveTo(Transform transform)
    {
        return AnimateMovement(transform.position, 1f);
    }
    public IEnumerator JumpTo(Transform transform)
    {
        return AnimateMovement(transform.position, 1f, true);
    }

    private IEnumerator AnimateMovement(Vector3 targetPosition, float duration, bool jumpAction = false)
    {
        if (jumpAction) {
            animator.SetTrigger("jump");
            yield return new WaitForSeconds(0.5f);
        } else {
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

    private void OnDestroy()
    {
        if (tapController != null)
        {
            tapController.OnTapped.RemoveListener(HandleTap);
            controller.OnGridUpdated -= UpdateWalkableCue;
        }
    }
}