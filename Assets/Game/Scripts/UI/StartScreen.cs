using TMPro;
using UnityEngine;

public class StartScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private GameObject resetButton;

    private void Start()
    {
        SetLevelText();
    }

    private void SetLevelText()
    {
        int currentLevel = Game.Manager.CurrentLevel;
        buttonText.text = Game.Manager.DoesLevelExist(currentLevel) ? "Level " + currentLevel.ToString() : "Completed!";
        if (currentLevel > 1){
            resetButton.SetActive(true);}
        else resetButton.SetActive(false);
    }

    public void LevelButtonClicked()
    {
        Game.Manager.StartGame();
    }

    public void EditorButtonClicked()
    {
        Game.Manager.OpenLevelEditor();
    }

    public void ResetButtonClicked()
    {
        Game.Manager.ResetProgress();
        SetLevelText();
    }

    public void LogoThud() {Game.Sound.PlaySound("thud");}
    public void ButtonPop() { Game.Sound.PlaySound("pop");}
}
