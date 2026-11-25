// Use English for comments in the code
using TMPro;
using UnityEngine;

public class SelectionGroupManager : MonoBehaviour
{
    public enum GroupType { SideSelection, DifficultySelection }

    [Header("Group Settings")]
    public GroupType groupType = GroupType.DifficultySelection;

    [Header("Colors")]
    public Color defaultColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Option Cubes")]
    public GameObject[] optionCubes;

    [Header("References")]
    public SpawnerManager spawnerManager;

    void Start()
    {
        if (spawnerManager == null)
            spawnerManager = FindFirstObjectByType<SpawnerManager>();

        if (optionCubes != null && optionCubes.Length > 0)
            SelectOption(optionCubes[0]);
    }

    public void SelectOption(GameObject clickedCube)
    {
        if (clickedCube == null) return;

        // Reset colors for all cubes in this group
        foreach (GameObject cube in optionCubes)
        {
            if (cube != null)
            {
                SetCubeColor(cube, defaultColor);
            }
        }

        // Highlight selected cube
        SetCubeColor(clickedCube, selectedColor);

        // Get text from TMP or Cube Name
        var tmpText = clickedCube.GetComponentInChildren<TMP_Text>();
        if (tmpText == null) tmpText = clickedCube.GetComponentInChildren<TextMeshProUGUI>();

        string rawText = (tmpText != null) ? tmpText.text : clickedCube.name;
        string cleanedText = System.Text.RegularExpressions.Regex.Replace(rawText, "<.*?>", string.Empty).Trim().ToUpper();
        string cubeName = clickedCube.name.ToUpper();

        UpdateSpawnerSettings(clickedCube, cleanedText, cubeName);

        Debug.Log($"[SelectionGroupManager] Selected '{clickedCube.name}' in group '{groupType}': {cleanedText}");
    }

    private void UpdateSpawnerSettings(GameObject clickedCube, string optionText, string cubeName)
    {
        if (spawnerManager == null)
        {
            spawnerManager = FindFirstObjectByType<SpawnerManager>();
            if (spawnerManager == null)
            {
                Debug.LogError("[SelectionGroupManager] SpawnerManager reference is missing!");
                return;
            }
        }

        // Process based on Group Type to prevent cross-matching errors
        if (groupType == GroupType.SideSelection)
        {
            if (optionText.Contains("LEFT") || optionText.Contains("שמאל") || cubeName.Contains("LEFT"))
            {
                spawnerManager.SetSideLeft();
            }
            else if (optionText.Contains("RIGHT") || optionText.Contains("ימין") || cubeName.Contains("RIGHT"))
            {
                spawnerManager.SetSideRight();
            }
            else if (optionText.Contains("BOTH") || optionText.Contains("שניהם") || optionText.Contains("שני") || cubeName.Contains("BOTH"))
            {
                spawnerManager.SetSideBoth();
            }
            else
            {
                Debug.LogWarning($"[SelectionGroupManager] Could not map Side option: Text='{optionText}', Name='{cubeName}'");
            }
        }
        else if (groupType == GroupType.DifficultySelection)
        {
            if (optionText.Contains("EASY") || optionText.Contains("קל") || cubeName.Contains("EASY"))
            {
                spawnerManager.SetDifficultyEasy();
            }
            else if (optionText.Contains("MEDIUM") || optionText.Contains("בינוני") || cubeName.Contains("MEDIUM"))
            {
                spawnerManager.SetDifficultyMedium();
            }
            else if (optionText.Contains("HARD") || optionText.Contains("קשה") || cubeName.Contains("HARD"))
            {
                spawnerManager.SetDifficultyHard();
            }
            else
            {
                Debug.LogWarning($"[SelectionGroupManager] Could not map Difficulty option: Text='{optionText}', Name='{cubeName}'");
            }
        }
    }

    private void SetCubeColor(GameObject cube, Color color)
    {
        Renderer r = cube.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            r.material.color = color;
        }
    }
}