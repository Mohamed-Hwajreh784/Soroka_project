// Use English for comments in the code
using UnityEngine;
using TMPro;

public class ScoreboardLinker : MonoBehaviour
{
    private void Start()
    {
        TextMeshProUGUI textComponent = GetComponentInChildren<TextMeshProUGUI>();

        if (textComponent != null && ScoreDisplayManager.Instance != null)
        {
            ScoreDisplayManager.Instance.RegisterFloatingHUD(textComponent);
        }
    }
}