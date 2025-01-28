using TMPro;
using UnityEngine;

public class StartScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonText;

    private void Start()
    {
        SetLevelText();
    }

    private void SetLevelText()
    {
        buttonText.text = "Level " + Game.Manager.CurrentLevel.ToString();
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
