using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsSliderDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Slider slider;
    [SerializeField] private int multiplier;

    private void Start() {
        UpdateDisplayText();
    }

    public void UpdateDisplayText() {
        displayText.text = "(" + slider.value * multiplier + ")";
    }
}
