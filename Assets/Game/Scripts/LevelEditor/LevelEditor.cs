using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelEditor : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private VisualTreeAsset levelSelection;
    [SerializeField] private VisualTreeAsset levelEditing;
    [SerializeField] private EditorGridController grid;

    //Selection
    private DropdownField levelSelector;
    private Button addButton;
    private Button removeButton;
    private VisualElement confirmationBox;
    private Label confirmationLabel;
    private Button acceptButton;
    private Button backButton;
    private Button editButton;
    private Button backToMenuButton;
    private string levelToDelete;

    // Editing mode fields
    private Button backToSelectionButton;
    private Button saveButton;
    private Button configButton;
    private Label tilePosText;
    private IntegerField timeLimitField;
    private VisualElement tunnelOptions;
    private VisualElement passengerOptions;
    private EnumField passengerColorSelector;
    private Toggle passengerHiddenToggle;
    private EnumField tunnelOrientationSelector;
    private DropdownField tunnelPassengerSelector;
    private EnumField tunnelPassengerColorSelector;
    private Button tunnelAddPassenger;
    private Button tunnelRemovePassenger;
    private Button tunnelSetColorPassenger;
    private int selectedPassengerIndex = 0;
    private VisualElement popupsPanel;
    private VisualElement sizeEditPopup;
    private VisualElement shipEditPopup;
    private Button sizeButton;
    private Button shipsButton;
    private SliderInt widthSlider;
    private SliderInt heightSlider;
    private Button applySizeButton;
    private Button backSizeButton;

    private DropdownField shipSelector;
    private Button removeShipButton;
    private EnumField shipColorSelector;
    private Button addShipButton;
    private Button setShipColorButton;
    private Button applyShipsButton;
    private Button backShipsButton;

    private VisualElement tileOptionsPanel;
    private Vector2Int selectedTilePosition;
    private string currentLevelName;
    private LevelData currentLevel;

    void Start()
    {
        OpenLevelSelection();
    }

    void ShowDeleteConfirmation(string levelName)
    {
        levelToDelete = levelName;
        confirmationBox.style.opacity = 1;
        confirmationLabel.text = $"Are you sure to delete {levelName}?";
    }

    void HideDeleteConfirmation()
    {
        confirmationBox.style.opacity = 0;
        levelToDelete = null;
    }

    void DeleteLevel(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) return;

        // Find the index of the level being deleted
        int currentIndex = levelSelector.choices.IndexOf(levelName);

        string jsonPath = Path.Combine(Application.dataPath, "Resources/Levels", $"{levelName}.json");
        string metaPath = jsonPath + ".meta";
        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }

        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        RefreshLevelList(currentIndex);
        HideDeleteConfirmation();
    }

    void RefreshLevelList(int previousIndex = -1, string selectLevelName = null)
    {
        var levels = Resources.LoadAll<TextAsset>("Levels");
        var levelNames = new List<string>();
        foreach (var level in levels)
        {
            levelNames.Add(level.name);
        }
        levelSelector.choices = levelNames;

        if (levelNames.Count > 0)
        {
            if (!string.IsNullOrEmpty(selectLevelName) && levelNames.Contains(selectLevelName))
            {
                levelSelector.value = selectLevelName;
            }
            else if (previousIndex >= 0)
            {
                int newIndex = previousIndex > 0 ? previousIndex - 1 : levelNames.Count - 1;
                levelSelector.value = levelNames[Mathf.Min(newIndex, levelNames.Count - 1)];
            }
            else
            {
                levelSelector.value = levelNames[0];
            }
        }
    }

    void CreateLevel(string levelName)
    {
        LevelData newLevel = new LevelData();
        string jsonData = JsonUtility.ToJson(newLevel, true);
        string path = Path.Combine(Application.dataPath, "Resources/Levels", $"level_{levelName}.json");
        File.WriteAllText(path, jsonData);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        RefreshLevelList();
        levelSelector.value = $"level_{levelName}";
    }

    void LoadLevel(string levelName)
    {
        TextAsset levelFile = Resources.Load<TextAsset>($"Levels/{levelName}");
        if (levelFile != null)
        {
            currentLevelName = levelName;
            currentLevel = JsonUtility.FromJson<LevelData>(levelFile.text);
            grid.ConstructGrid(currentLevel);
        }
    }

    void SaveCurrentLevel()
    {
        if (string.IsNullOrEmpty(currentLevelName)) return;

        currentLevel = grid.GetLevelData();

        string jsonData = JsonUtility.ToJson(currentLevel, true);
        string path = Path.Combine(Application.dataPath, "Resources/Levels", $"{currentLevelName}.json");
        File.WriteAllText(path, jsonData);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    void OpenLevelSelection(string selectLevelName = null)
    {
        grid.IsInEditMode = false;
        document.visualTreeAsset = levelSelection;
        var root = document.rootVisualElement;
        levelSelector = root.Q<DropdownField>("LevelSelector");
        addButton = root.Q<Button>("AddButton");
        removeButton = root.Q<Button>("RemoveButton");
        confirmationBox = root.Q<VisualElement>("ConfirmationBox");
        confirmationLabel = root.Q<Label>("ConfirmationLabel");
        acceptButton = root.Q<Button>("AcceptButton");
        backButton = root.Q<Button>("BackButton");
        editButton = root.Q<Button>("EditButton");
        backToMenuButton = root.Q<Button>("BackToMenuButton");
        confirmationBox.style.opacity = 0;

        var levels = Resources.LoadAll<TextAsset>("Levels");
        var levelNames = new List<string>();
        foreach (var level in levels)
        {
            levelNames.Add(level.name);
        }
        levelSelector.choices = levelNames;

        if (!string.IsNullOrEmpty(selectLevelName) && levelNames.Contains(selectLevelName))
        {
            levelSelector.value = selectLevelName;
        }
        else if (levelNames.Count > 0)
        {
            levelSelector.value = levelNames[0];
        }

        if (!string.IsNullOrEmpty(levelSelector.value))
        {
            LoadLevel(levelSelector.value);
        }

        addButton.clicked += () =>
        {
            int newLevelNumber = levelSelector.choices.Count + 1;
            CreateLevel(newLevelNumber.ToString());
        };

        removeButton.clicked += () =>
        {
            if (!string.IsNullOrEmpty(levelSelector.value))
            {
                ShowDeleteConfirmation(levelSelector.value);
            }
        };

        acceptButton.clicked += () =>
        {
            if (!string.IsNullOrEmpty(levelToDelete))
            {
                DeleteLevel(levelToDelete);
            }
        };

        backButton.clicked += HideDeleteConfirmation;
        backToMenuButton.clicked += Game.Manager.OpenMainMenu;

        levelSelector.RegisterValueChangedCallback(evt =>
        {
            if (!string.IsNullOrEmpty(evt.newValue))
            {
                LoadLevel(evt.newValue);
            }
        });

        editButton.clicked += () =>
        {
            if (!string.IsNullOrEmpty(levelSelector.value))
            {
                OpenLevelEditing(levelSelector.value);
            }
        };
    }

    void OpenLevelEditing(string levelName)
    {
        document.visualTreeAsset = levelEditing;
        var root = document.rootVisualElement;
        backToSelectionButton = root.Q<Button>("BackToSelectionButton");
        saveButton = root.Q<Button>("SaveButton");
        timeLimitField = root.Q<IntegerField>("TimeLimitField");
        tileOptionsPanel = root.Q<VisualElement>("TileOptionsPanel");
        tunnelOptions = root.Q<VisualElement>("TunnelOptionsPanel");
        passengerOptions = root.Q<VisualElement>("PassengerOptionsPanel");
        tilePosText = root.Q<Label>("TilePosText");
        configButton = root.Q<Button>("ConfigButton");
        passengerColorSelector = root.Q<EnumField>("PassengerColorSelector");
        passengerHiddenToggle = root.Q<Toggle>("isHiddenToggle");
        tunnelOrientationSelector = root.Q<EnumField>("OrientationSelector");
        tunnelPassengerSelector = root.Q<DropdownField>("TunnelPassengers");
        tunnelPassengerColorSelector = root.Q<EnumField>("TPassengerColorSelect");
        tunnelAddPassenger = root.Q<Button>("AddNewTPassenger");
        tunnelRemovePassenger = root.Q<Button>("RemovePassengerButton");
        tunnelSetColorPassenger = root.Q<Button>("SetTPassengerColor");

        popupsPanel = root.Q<VisualElement>("Popups");
        sizeEditPopup = root.Q<VisualElement>("SizeEditPopup");
        shipEditPopup = root.Q<VisualElement>("ShipEditPopup");
        sizeButton = root.Q<Button>("SizeButton");
        widthSlider = root.Q<SliderInt>("WidthSlider");
        heightSlider = root.Q<SliderInt>("HeightSlider");
        applySizeButton = root.Q<Button>("ApplySizeButton");
        backSizeButton = root.Q<Button>("BackSizeButton");
        shipsButton = root.Q<Button>("ShipsButton");
        shipSelector = shipEditPopup.Q<DropdownField>("ShipSelector");
        removeShipButton = shipEditPopup.Q<Button>("RemoveShipButton");
        shipColorSelector = shipEditPopup.Q<EnumField>("ShipColorSelect");
        addShipButton = shipEditPopup.Q<Button>("AddNewShipButton");
        setShipColorButton = shipEditPopup.Q<Button>("SetColorButton");
        applyShipsButton = root.Q<Button>("ApplyShipsButton");
        backShipsButton = root.Q<Button>("BackShipsButton");
        shipColorSelector.Init(PassengerColor.Red);
        HidePopups();

        sizeButton.clicked += ShowSizeEditor;
        shipsButton.clicked += ShowShipEditor;
        applySizeButton.clicked += ApplySize;
        backSizeButton.clicked += HidePopups;
        applyShipsButton.clicked += ApplyShips;
        backShipsButton.clicked += HidePopups;
        addShipButton.clicked += AddShip;
        removeShipButton.clicked += RemoveShip;
        setShipColorButton.clicked += SetShipColor;

        var emptyButton = tileOptionsPanel.Q<Button>("EmptyCell");
        var invalidButton = tileOptionsPanel.Q<Button>("InvalidCell");
        var passengerButton = tileOptionsPanel.Q<Button>("Passenger");
        var tunnelButton = tileOptionsPanel.Q<Button>("Tunnel");

        emptyButton.clicked += () => SetTileType(TileType.Empty);
        invalidButton.clicked += () => SetTileType(TileType.Invalid);
        passengerButton.clicked += () => SetTileType(TileType.Passenger);
        tunnelButton.clicked += () => SetTileType(TileType.Tunnel);
        configButton.clicked += () => ShowObjectConfiguration();
        backToSelectionButton.clicked += () => OpenLevelSelection(levelName);
        saveButton.clicked += SaveCurrentLevel;

        passengerColorSelector.Init(PassengerColor.Red);
        tunnelOrientationSelector.Init(Tunnel.Orientation.Down);
        tunnelPassengerColorSelector.Init(PassengerColor.Red);

        tunnelOrientationSelector.RegisterValueChangedCallback(evt =>
        {
            if (grid.GetGridTileAt(selectedTilePosition)?.GridObject is Tunnel)
            {
                var orientation = (Tunnel.Orientation)evt.newValue;
                grid.SetTunnelOrientation(selectedTilePosition, (int)orientation);
            }
        });

        passengerColorSelector.RegisterValueChangedCallback(evt =>
        {
            if (grid.GetGridTileAt(selectedTilePosition)?.GridObject is Passenger)
            {
                var color = (PassengerColor)evt.newValue;
                grid.SetPassengerColor(selectedTilePosition, color);
            }
        });

        passengerHiddenToggle.RegisterValueChangedCallback(evt =>
        {
            if (grid.GetGridTileAt(selectedTilePosition)?.GridObject is Passenger)
            {
                bool isHidden = evt.newValue;
                grid.SetPassengerHidden(selectedTilePosition, isHidden);
            }
        });

        tunnelPassengerSelector.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue != null && int.TryParse(evt.newValue.Split(':')[0], out int index))
            {
                selectedPassengerIndex = index;
            }
        });

        tunnelAddPassenger.clicked += () =>
        {
            if (grid.GetGridTileAt(selectedTilePosition)?.GridObject is Tunnel tunnel)
            {
                var color = (PassengerColor)tunnelPassengerColorSelector.value;
                var currentData = tunnel.GetTunnelData();
                var newPassengers = currentData.passengers.ToList();
                newPassengers.Add(new TunnelPassenger { color = color });
                grid.SetTunnelPassengers(selectedTilePosition, newPassengers.ToArray());
                selectedPassengerIndex = newPassengers.Count - 1;
                UpdateTunnelPassengerList(newPassengers.ToArray());
            }
        };

        tunnelRemovePassenger.clicked += () =>
        {
            if (grid.GetGridTileAt(selectedTilePosition)?.GridObject is Tunnel tunnel &&
                int.TryParse(tunnelPassengerSelector.value?.Split(':')[0], out int index))
            {
                var currentData = tunnel.GetTunnelData();
                var passengers = currentData.passengers.ToList();
                if (index >= 0 && index < passengers.Count)
                {
                    passengers.RemoveAt(index);
                    grid.SetTunnelPassengers(selectedTilePosition, passengers.ToArray());
                    selectedPassengerIndex = Mathf.Max(0, index - 1);
                    UpdateTunnelPassengerList(passengers.ToArray());
                    grid.ConstructGrid(grid.GetLevelData());
                }
            }
        };

        tunnelSetColorPassenger.clicked += () =>
        {
            if (grid.GetGridTileAt(selectedTilePosition)?.GridObject is Tunnel tunnel &&
                int.TryParse(tunnelPassengerSelector.value?.Split(':')[0], out int index))
            {
                var currentData = tunnel.GetTunnelData();
                var passengers = currentData.passengers.ToList();
                if (index >= 0 && index < passengers.Count)
                {
                    var color = (PassengerColor)tunnelPassengerColorSelector.value;
                    passengers[index].color = color;
                    grid.SetTunnelPassengers(selectedTilePosition, passengers.ToArray());
                    UpdateTunnelPassengerList(passengers.ToArray());
                    grid.ConstructGrid(grid.GetLevelData());
                }
            }
        };

        grid.OnTileSelected += ShowTileOptions;
        HideAllPanels();
        LoadLevel(levelName);
        grid.IsInEditMode = true;
    }

    private void HidePopups()
    {
        popupsPanel.style.display = DisplayStyle.None;
        sizeEditPopup.style.display = DisplayStyle.None;
        shipEditPopup.style.display = DisplayStyle.None;
    }

    private void ShowSizeEditor()
    {
        popupsPanel.style.display = DisplayStyle.Flex;
        sizeEditPopup.style.display = DisplayStyle.Flex;
        shipEditPopup.style.display = DisplayStyle.None;

        widthSlider.value = currentLevel.gridSize.x;
        heightSlider.value = currentLevel.gridSize.y;
    }

    private void ApplySize()
    {
        currentLevel.gridSize = new SerializableVector2Int(widthSlider.value, heightSlider.value);
        grid.ConstructGrid(currentLevel);
        HidePopups();
    }

    private void ShowShipEditor()
    {
        popupsPanel.style.display = DisplayStyle.Flex;
        shipEditPopup.style.display = DisplayStyle.Flex;
        sizeEditPopup.style.display = DisplayStyle.None;

        UpdateShipList();
    }

    private void UpdateShipList()
    {
        var choices = new List<string>();
        for (int i = 0; i < currentLevel.busSequence.Length; i++)
        {
            choices.Add($"{i}: {currentLevel.busSequence[i].color}");
        }
        shipSelector.choices = choices;
        if (choices.Count > 0)
            shipSelector.value = choices[0];
    }

    private void AddShip()
    {
        var shipList = currentLevel.busSequence.ToList();
        shipList.Add(new ShipData
        {
            color = (PassengerColor)shipColorSelector.value,
            arrivalOrder = shipList.Count,
            capacity = 3
        });
        currentLevel.busSequence = shipList.ToArray();
        UpdateShipList();
    }

    private void RemoveShip()
    {
        if (int.TryParse(shipSelector.value?.Split(':')[0], out int index))
        {
            var shipList = currentLevel.busSequence.ToList();
            if (index >= 0 && index < shipList.Count)
            {
                shipList.RemoveAt(index);
                for (int i = 0; i < shipList.Count; i++)
                    shipList[i].arrivalOrder = i;

                currentLevel.busSequence = shipList.ToArray();
                UpdateShipList();
            }
        }
    }

    private void SetShipColor()
    {
        if (int.TryParse(shipSelector.value?.Split(':')[0], out int index))
        {
            var shipList = currentLevel.busSequence.ToList();
            if (index >= 0 && index < shipList.Count)
            {
                shipList[index].color = (PassengerColor)shipColorSelector.value;
                currentLevel.busSequence = shipList.ToArray();
                UpdateShipList();
            }
        }
    }

    private void ApplyShips()
    {
        grid.ConstructGrid(currentLevel);
        HidePopups();
    }

    void ShowTileOptions(Vector2Int position)
    {
        tileOptionsPanel.style.opacity = 1;
        tileOptionsPanel.style.display = DisplayStyle.Flex;

        if (selectedTilePosition != Vector2Int.zero)
        {
            CellBase oldCell = grid.GetCellAt(selectedTilePosition);
            if (oldCell != null)
                oldCell.CellDeselected();
        }

        selectedTilePosition = position;
        tilePosText.text = position.ToString();

        CellBase cell = grid.GetCellAt(selectedTilePosition);
        if (cell != null)
        {
            cell.CellSelected();
            ShowCellSpecificOptions(cell);
        }
    }

    private void UpdateTunnelPassengerList(TunnelPassenger[] passengers)
    {
        var choices = new List<string>();
        for (int i = 0; i < passengers.Length; i++)
        {
            choices.Add($"{i}: {passengers[i].color}");
        }
        tunnelPassengerSelector.choices = choices;
        if (choices.Count > 0)
        {
            selectedPassengerIndex = Mathf.Min(selectedPassengerIndex, choices.Count - 1);
            tunnelPassengerSelector.value = choices[selectedPassengerIndex];
        }
    }

    private void ShowCellSpecificOptions(CellBase cell)
    {
        HideAllPanels();

        if (cell is GridTile gridTile)
        {
            if (gridTile.GridObject != null)
            {
                configButton.style.display = DisplayStyle.Flex;
                tileOptionsPanel.style.display = DisplayStyle.Flex;
                return;
            }
            else
            {
                configButton.style.display = DisplayStyle.None;
                tileOptionsPanel.style.display = DisplayStyle.Flex;
                return;
            }
        }
        tileOptionsPanel.style.display = DisplayStyle.Flex;
    }

    void HideAllPanels()
    {
        tileOptionsPanel.style.display = DisplayStyle.None;
        tunnelOptions.style.display = DisplayStyle.None;
        passengerOptions.style.display = DisplayStyle.None;
    }

    private void ShowObjectConfiguration()
    {
        tileOptionsPanel.style.display = DisplayStyle.None;

        GridObject selectedObj = grid.GetGridTileAt(selectedTilePosition).GridObject;
        if (selectedObj == null) return;

        if (selectedObj is Tunnel tunnel)
            ShowTunnelOptions(tunnel);
        else if (selectedObj is Passenger passenger)
            ShowPassengerOptions(passenger);
    }

    void ShowPassengerOptions(Passenger passenger)
    {
        if (passengerOptions == null || passengerColorSelector == null)
        {
            Debug.LogError("Passenger UI elements not found!");
            return;
        }

        passengerOptions.style.display = DisplayStyle.Flex;
        PassengerData data = passenger.GetPassengerData();
        passengerColorSelector.value = data.color;
        passengerHiddenToggle.value = data.isHidden;
    }

    void ShowTunnelOptions(Tunnel tunnel)
    {
        if (tunnelOptions == null || tunnelOrientationSelector == null ||
            tunnelPassengerSelector == null || tunnelPassengerColorSelector == null)
        {
            Debug.LogError("Tunnel UI elements not found!");
            return;
        }

        tunnelOptions.style.display = DisplayStyle.Flex;
        TunnelData data = tunnel.GetTunnelData();
        tunnelOrientationSelector.value = (Tunnel.Orientation)data.orientation;
        UpdateTunnelPassengerList(data.passengers);
    }

    void SetTileType(TileType type)
    {
        grid.SetTileType(selectedTilePosition, type);
        ShowTileOptions(selectedTilePosition);
    }
}

public enum TileType
{
    Empty,
    Invalid,
    Passenger,
    Tunnel
}
