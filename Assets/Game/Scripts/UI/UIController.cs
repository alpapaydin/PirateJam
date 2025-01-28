using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private GameObject winPopupPrefab;
    [SerializeField] private GameObject losePopupPrefab;
    [SerializeField] private GameObject tapToStart;
    private GameObject activePopup;
    private UIDocument document;
    private Label levelText;
    private Label timerText;
    private Vector2 originalScale = new Vector2(0.3f, 0.3f);
    private float minScaleMultiplier = 1f;
    private float maxScaleMultiplier = 2f;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;
        levelText = root.Q<Label>("LevelText");
        timerText = root.Q<Label>("TimerText");
    }

    public void SetLevelText(int currentLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {currentLevel}";
        }
    }
    public void SetTimerText(float timeLeft)
    {
        if (timerText != null)
        {
            timerText.text = timeLeft.ToString("0");
            if (timeLeft <= 10f)
            {
                float currentMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, 1f - (timeLeft / 10f));
                Color currentColor = Color.Lerp(Color.white, Color.red, 1f - (timeLeft / 10f));

                timerText.style.scale = new Scale(originalScale * currentMultiplier);
                timerText.style.color = currentColor;
            }
            else
            {
                timerText.style.scale = originalScale;
            }
        }
    }

    public void LevelWon()
    {
        if (activePopup != null) Destroy(activePopup);
        HideHUD();
        activePopup = Instantiate(winPopupPrefab, gameCanvas.transform);
    }

    public void LevelFailed()
    {
        if (activePopup != null) Destroy(activePopup);
        HideHUD();
        activePopup = Instantiate(losePopupPrefab, gameCanvas.transform);
    }

    public void HideHUD()
    {
        document.rootVisualElement.style.opacity = 0;
    }

    public void RemoveTapToStart()
    {
        Game.Sound.PlaySound("bloop1");
        Destroy(tapToStart);
    }
}