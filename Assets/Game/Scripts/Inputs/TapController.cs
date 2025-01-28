using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class TapController : MonoBehaviour
{
    [SerializeField] private float tapThreshold = 0.2f;
    private float touchStartTime;
    private bool isTouching = false;
    public UnityEvent OnTapped;
    private UIDocument[] uiDocuments;
    private List<VisualElement> pickedElements = new List<VisualElement>();

    private void Awake()
    {
        if (OnTapped == null)
            OnTapped = new UnityEvent();
        uiDocuments = FindObjectsOfType<UIDocument>();
    }

    private void Update()
    {
        if (IsPointerOverAnyUI())
        {
            isTouching = false;
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            HandleTouchStart(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HandleTouchEnd(Input.mousePosition);
        }
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchStart(touch.position);
                    break;
                case TouchPhase.Ended:
                    HandleTouchEnd(touch.position);
                    break;
                case TouchPhase.Canceled:
                    isTouching = false;
                    break;
            }
        }
    }

    private bool IsPointerOverAnyUI()
    {
        if (uiDocuments == null || uiDocuments.Length == 0)
            return false;

        Vector2 pointerPosition = Input.mousePosition;
        if (Input.touchCount > 0)
        {
            pointerPosition = Input.GetTouch(0).position;
        }
        foreach (var document in uiDocuments)
        {
            if (document == null || document.rootVisualElement == null || document.rootVisualElement.panel == null)
                continue;
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(document.rootVisualElement.panel, pointerPosition);
            pickedElements.Clear();
            document.rootVisualElement.panel.PickAll(panelPosition, pickedElements);

            if (pickedElements.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleTouchStart(Vector2 screenPosition)
    {
        if (IsPointerOverAnyUI()) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                touchStartTime = Time.time;
                isTouching = true;
            }
        }
    }

    private void HandleTouchEnd(Vector2 screenPosition)
    {
        if (!isTouching || IsPointerOverAnyUI())
        {
            isTouching = false;
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                float touchDuration = Time.time - touchStartTime;
                if (touchDuration <= tapThreshold)
                {
                    OnTapped?.Invoke();
                }
            }
        }
        isTouching = false;
    }
}