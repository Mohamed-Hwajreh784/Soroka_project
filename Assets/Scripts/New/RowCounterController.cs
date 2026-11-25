using UnityEngine;
using TMPro;

public class RowCounterController : MonoBehaviour
{
    [Header("UI Settings")]
    public TMP_Text numberText; // Drag the center number TMP text here

    [Header("Counter Settings")]
    public int currentNumber = 5;
    public int minLimit = 0;
    public int maxLimit = 10;

    public void IncreaseValue()
    {
        if (currentNumber < maxLimit)
        {
            currentNumber++;
            UpdateDisplay();
        }
    }

    public void DecreaseValue()
    {
        if (currentNumber > minLimit)
        {
            currentNumber--;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (numberText != null)
            numberText.text = currentNumber.ToString();
    }

    void Start()
    {
        UpdateDisplay();
    }
}