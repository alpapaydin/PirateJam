using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelEditor : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private VisualTreeAsset levelSelection;
    private DropdownField levelSelector;

    void Start()
    {
        OpenLevelSelection();
    }

    void CreateLevel(string levelName) { }
    void DeleteLevel(string levelName) { }
    void OpenLevelSelection() 
    {
        document.visualTreeAsset = levelSelection;
        var root = document.rootVisualElement;
        levelSelector = root.Q<DropdownField>("LevelSelector");

        var levels = Resources.LoadAll<TextAsset>("Levels");
        var levelNames = new List<string>();

        foreach (var level in levels)
        {
            levelNames.Add(level.name);
        }

        levelSelector.choices = levelNames;
        if (levelNames.Count > 0)
        {
            levelSelector.value = levelNames[0];
        }
    }
    void OpenLevelEditing(string levelName) { }
    //add/remove ship
    //add/remove passengers, tunnels, invalid cells, then refresh
    //set time limit
    //validate levels
}