using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private SceneAsset gameScene;
    [SerializeField] private SceneAsset editorScene;
    [SerializeField] private SceneAsset startScene;

    private const string LEVEL_KEY = "CurrentLevel";
    public int CurrentLevel => PlayerPrefs.GetInt(LEVEL_KEY, 1);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameScene.name);
    }

    public void OpenLevelEditor()
    {
        SceneManager.LoadScene(editorScene.name);
    }

    public void OpenMainMenu()
    {
        SceneManager.LoadScene(startScene.name);
    }

    public void CompleteLevel(int levelNumber)
    {
        PlayerPrefs.SetInt(LEVEL_KEY, levelNumber + 1);
        PlayerPrefs.Save();
    }

    public void ResetProgress()
    {
        CompleteLevel(0);
    }
}

public static class Game
{
    public static GameManager Manager => GameManager.Instance;
    public static SoundController Sound => SoundController.Instance;
}
