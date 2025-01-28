using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class LevelValidator
{
    public enum ValidationError
    {
        None,
        InvalidTimeLimit,
        NoShips,
        NotEnoughPassengers,
        TooManyPassengers,
        InvalidTunnelSpawnPoint,
        BlockedPassengerPath,
    }

    private const string LEVELS_PATH = "Assets/Resources/Levels/";

    public static void ValidateAllLevels()
    {
        var levels = Resources.LoadAll<TextAsset>("Levels");
        var levelDict = new List<(int fileNumber, TextAsset asset, LevelData data)>();
        foreach (var level in levels)
        {
            string[] parts = level.name.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out int fileNumber))
            {
                LevelData levelData = JsonUtility.FromJson<LevelData>(level.text);
                levelDict.Add((fileNumber, level, levelData));
            }
            else
            {
                Debug.LogError($"Invalid level filename format: {level.name}");
            }
        }
        levelDict.Sort((a, b) => a.fileNumber.CompareTo(b.fileNumber));
        bool needsRewrite = false;
        for (int i = 0; i < levelDict.Count; i++)
        {
            int expectedNumber = i + 1;
            var (fileNumber, asset, data) = levelDict[i];
            if (fileNumber != expectedNumber)
            {
                needsRewrite = true;
            }
            if (data.levelNumber != expectedNumber)
            {
                data.levelNumber = expectedNumber;
                needsRewrite = true;
            }
            if (needsRewrite)
            {
                string newFileName = $"level_{expectedNumber}.json";
                string jsonData = JsonUtility.ToJson(data, true);
                string newPath = Path.Combine(LEVELS_PATH, newFileName);
                string oldPath = Path.Combine(LEVELS_PATH, asset.name + ".json");
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                    string oldMetaPath = oldPath + ".meta";
                    if (File.Exists(oldMetaPath))
                        File.Delete(oldMetaPath);
                }
                File.WriteAllText(newPath, jsonData);

#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
        }

        if (needsRewrite)
        {
            Debug.Log("Level files have been reordered and fixed");
        }
        else
        {
            Debug.Log("All level files are properly sequenced");
        }
    }

    public static void ValidateSingleLevel(string levelName)
    {
        var level = Resources.Load<TextAsset>($"Levels/{levelName}");
        if (level == null)
        {
            Debug.LogError($"Could not find level: {levelName}");
            return;
        }

        string[] parts = level.name.Split('_');
        if (parts.Length != 2 || !int.TryParse(parts[1], out int fileNumber))
        {
            Debug.LogError($"Invalid level filename format: {levelName}");
            return;
        }

        LevelData levelData = JsonUtility.FromJson<LevelData>(level.text);
        if (levelData.levelNumber != fileNumber)
        {
            Debug.LogWarning($"Level number mismatch in {levelName}: File number is {fileNumber}, internal number is {levelData.levelNumber}");
            levelData.levelNumber = fileNumber;

            string jsonData = JsonUtility.ToJson(levelData, true);
            string path = Path.Combine(LEVELS_PATH, $"{levelName}.json");
            File.WriteAllText(path, jsonData);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            Debug.Log($"Fixed level number in {levelName}");
        }
    }

    public static bool IsLevelSequenceValid()
    {
        var levels = Resources.LoadAll<TextAsset>("Levels");
        var numbers = new List<int>();

        foreach (var level in levels)
        {
            string[] parts = level.name.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out int number))
            {
                numbers.Add(number);
            }
        }

        numbers.Sort();
        for (int i = 0; i < numbers.Count; i++)
        {
            if (numbers[i] != i + 1)
                return false;
        }

        return true;
    }


    public static (bool isValid, List<ValidationError> errors) ValidateLevelData(LevelData levelData, List<ValidationError> errorsToSkip)
    {
        var errors = new List<ValidationError>();

        ValidateBasicRequirements(levelData, errors, errorsToSkip);

        if (levelData.busSequence != null && levelData.busSequence.Length > 0)
        {
            var tunnelPositions = new HashSet<Vector2Int>();
            var invalidCells = new HashSet<Vector2Int>(
                levelData.invalidCells.Select(pos => new Vector2Int(pos.x, pos.y)));

            if (levelData.tunnels != null)
            {
                foreach (var tunnel in levelData.tunnels)
                {
                    tunnelPositions.Add(new Vector2Int(tunnel.x, tunnel.y));
                }
            }

            ValidatePassengerCounts(levelData, errors, errorsToSkip);

            if (!errorsToSkip.Contains(ValidationError.InvalidTunnelSpawnPoint))
                ValidateTunnelSpawnPoints(levelData, invalidCells, tunnelPositions, errors);

            if (!errorsToSkip.Contains(ValidationError.BlockedPassengerPath))
                ValidatePaths(levelData, invalidCells, tunnelPositions, errors);
        }

        return (errors.Count == 0, errors);
    }

    private static void ValidateBasicRequirements(LevelData levelData, List<ValidationError> errors, List<ValidationError> errorsToSkip)
    {
        if (levelData.timeLimit <= 0)
        {
            if (!errorsToSkip.Contains(ValidationError.InvalidTimeLimit))
                errors.Add(ValidationError.InvalidTimeLimit);
        }

        if (levelData.busSequence == null || levelData.busSequence.Length == 0)
        {
            if (!errorsToSkip.Contains(ValidationError.NoShips))
                errors.Add(ValidationError.NoShips);
        }
    }

    private static void ValidatePassengerCounts(LevelData levelData, List<ValidationError> errors, List<ValidationError> errorsToSkip)
    {
        var requiredPassengers = new Dictionary<PassengerColor, int>();
        foreach (var ship in levelData.busSequence)
        {
            if (!requiredPassengers.ContainsKey(ship.color))
                requiredPassengers[ship.color] = 0;
            requiredPassengers[ship.color] += ship.capacity;
        }

        var availablePassengers = new Dictionary<PassengerColor, int>();

        if (levelData.passengers != null)
        {
            foreach (var passenger in levelData.passengers)
            {
                if (!availablePassengers.ContainsKey(passenger.color))
                    availablePassengers[passenger.color] = 0;
                availablePassengers[passenger.color]++;
            }
        }

        if (levelData.tunnels != null)
        {
            foreach (var tunnel in levelData.tunnels)
            {
                if (tunnel.passengers != null)
                {
                    foreach (var passenger in tunnel.passengers)
                    {
                        if (!availablePassengers.ContainsKey(passenger.color))
                            availablePassengers[passenger.color] = 0;
                        availablePassengers[passenger.color]++;
                    }
                }
            }
        }

        foreach (var required in requiredPassengers)
        {
            PassengerColor color = required.Key;
            int requiredCount = required.Value;
            int availableCount = availablePassengers.ContainsKey(color) ? availablePassengers[color] : 0;

            if (availableCount < requiredCount)
            {
                if (!errorsToSkip.Contains(ValidationError.NotEnoughPassengers))
                    errors.Add(ValidationError.NotEnoughPassengers);
            }
            else if (availableCount > requiredCount)
            {
                if (!errorsToSkip.Contains(ValidationError.TooManyPassengers))
                    errors.Add(ValidationError.TooManyPassengers);
            }
        }
    }

    private static void ValidateTunnelSpawnPoints(LevelData levelData, HashSet<Vector2Int> invalidCells,
        HashSet<Vector2Int> tunnelPositions, List<ValidationError> errors)
    {
        if (levelData.tunnels != null)
        {
            foreach (var tunnel in levelData.tunnels)
            {
                Vector2Int tunnelPos = new Vector2Int(tunnel.x, tunnel.y);
                Vector2Int spawnPoint = GetTunnelSpawnPoint(tunnelPos, tunnel.orientation);

                if (!IsValidSpawnPoint(spawnPoint, levelData.gridSize, invalidCells, tunnelPositions, tunnelPos))
                {
                    errors.Add(ValidationError.InvalidTunnelSpawnPoint);
                }
            }
        }
    }

    private static void ValidatePaths(LevelData levelData, HashSet<Vector2Int> invalidCells,
        HashSet<Vector2Int> tunnelPositions, List<ValidationError> errors)
    {
        if (levelData.passengers != null)
        {
            foreach (var passenger in levelData.passengers)
            {
                Vector2Int passengerPos = new Vector2Int(passenger.x, passenger.y);
                if (!HasPathToFirstRow(passengerPos, levelData.gridSize, invalidCells, tunnelPositions))
                {
                    errors.Add(ValidationError.BlockedPassengerPath);
                }
            }
        }

        if (levelData.tunnels != null)
        {
            foreach (var tunnel in levelData.tunnels)
            {
                Vector2Int spawnPoint = GetTunnelSpawnPoint(new Vector2Int(tunnel.x, tunnel.y), tunnel.orientation);
                if (!HasPathToFirstRow(spawnPoint, levelData.gridSize, invalidCells, tunnelPositions))
                {
                    errors.Add(ValidationError.BlockedPassengerPath);
                }
            }
        }
    }

    private static Vector2Int GetTunnelSpawnPoint(Vector2Int tunnelPos, int orientation)
    {
        return orientation switch
        {
            0 => tunnelPos + new Vector2Int(0, -1),
            1 => tunnelPos + new Vector2Int(1, 0),
            2 => tunnelPos + new Vector2Int(0, 1),
            3 => tunnelPos + new Vector2Int(-1, 0),
            _ => tunnelPos
        };
    }

    private static bool IsValidSpawnPoint(Vector2Int point, SerializableVector2Int gridSize,
        HashSet<Vector2Int> invalidCells, HashSet<Vector2Int> tunnelPositions, Vector2Int excludeTunnel)
    {
        if (point.x < 0 || point.x >= gridSize.x || point.y < 0 || point.y >= gridSize.y)
            return false;
        if (invalidCells.Contains(point))
            return false;
        foreach (var tunnelPos in tunnelPositions)
        {
            if (tunnelPos != excludeTunnel && tunnelPos == point)
                return false;
        }

        return true;
    }

    private static bool HasPathToFirstRow(Vector2Int start, SerializableVector2Int gridSize,
        HashSet<Vector2Int> invalidCells, HashSet<Vector2Int> tunnelPositions)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] directions = new[]
        {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current.y == 0)
                return true;
            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next))
                    continue;
                if (next.x < 0 || next.x >= gridSize.x || next.y < 0 || next.y >= gridSize.y)
                    continue;
                if (invalidCells.Contains(next) || tunnelPositions.Contains(next))
                    continue;

                queue.Enqueue(next);
                visited.Add(next);
            }
        }

        return false;
    }

    public static string GetValidationErrorMessage(ValidationError error)
    {
        return error switch
        {
            ValidationError.InvalidTimeLimit => "Time limit must be greater than 0",
            ValidationError.NoShips => "Level must have at least one ship",
            ValidationError.NotEnoughPassengers => "Not enough passengers to fill ships",
            ValidationError.TooManyPassengers => "Too many passengers for available ships",
            ValidationError.InvalidTunnelSpawnPoint => "Tunnels have invalid spawn points",
            ValidationError.BlockedPassengerPath => "Passengers or tunnel spawn points have no valid path to first row",
            _ => "Unknown validation error"
        };
    }

    public static (bool canSave, string message) ValidateLevelDataWithMessage(LevelData levelData, List<ValidationError> errorsToSkip)
    {
        var (isValid, errors) = ValidateLevelData(levelData, errorsToSkip);
        if (!isValid)
        {
            Dictionary<ValidationError, int> errorCounts = new Dictionary<ValidationError, int>();
            foreach (var error in errors)
            {
                if (!errorCounts.ContainsKey(error))
                    errorCounts[error] = 0;
                errorCounts[error]++;
            }
            string errorMessage = "";
            foreach (var errorCount in errorCounts)
            {
                if (errorMessage.Length > 0)
                    errorMessage += "\n";

                string baseMessage = GetValidationErrorMessage(errorCount.Key);
                if (errorCount.Value > 1)
                {
                    errorMessage += $"• {baseMessage} (x{errorCount.Value})";
                }
                else
                {
                    errorMessage += $"• {baseMessage}";
                }
            }
            return (false, errorMessage);
        }
        return (true, "Successfully Saved!");
    }
}