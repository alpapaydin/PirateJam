using UnityEngine;
using UnityEngine.Events;

public class TapController : MonoBehaviour
{
    [SerializeField] private float tapThreshold = 0.2f;

    private float touchStartTime;
    private bool isTouching = false;

    public UnityEvent OnTapped;

    private void Awake()
    {
        if (OnTapped == null)
            OnTapped = new UnityEvent();
    }

    private void Update()
    {
        // Handle mouse input for Unity Editor and PC
        if (Input.GetMouseButtonDown(0))
        {
            HandleTouchStart(Input.mousePosition);
        }
        if (Input.GetMouseButtonUp(0))
        {
            HandleTouchEnd(Input.mousePosition);
        }

        // Handle touch input for mobile devices
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                HandleTouchStart(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                HandleTouchEnd(touch.position);
            }
        }
    }

    private void HandleTouchStart(Vector2 screenPosition)
    {
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
        if (!isTouching) return;

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