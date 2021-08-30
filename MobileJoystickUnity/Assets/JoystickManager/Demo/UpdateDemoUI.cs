using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateDemoUI : MonoBehaviour
{
    public GameObject infoCanvas;
    public Text txt;
    public GameObject rightArrow, leftArrow, topArrow, downArrow;

    [Header("Sliders")]
    [Space(15)]
    public Text mmdText;
    public Slider mmdSlider;
    public Text thresholdText;
    public Slider thresholdSlider;

    void Update()
    {
        Vector2 input = MobileJoystick.JoystickManager.instance.getAxis();
        txt.text = input.ToString();

        if (Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            if (infoCanvas.activeInHierarchy) infoCanvas.SetActive(false);
        }
        else if (!infoCanvas.activeInHierarchy) infoCanvas.SetActive(true);

        if (input.x == 0)
        {
            rightArrow.SetActive(false);
            leftArrow.SetActive(false);
        }
        else if (input.x > 0) rightArrow.SetActive(true);
        else leftArrow.SetActive(true);

        if (input.y == 0)
        {
            topArrow.SetActive(false);
            downArrow.SetActive(false);
        }
        else if (input.y > 0) topArrow.SetActive(true);
        else downArrow.SetActive(true);
    }

    public void onMMDchange()
    {
        mmdText.text = "Max Move Dist: "+ mmdSlider.value.ToString();
        MobileJoystick.JoystickManager.instance.setMaxMoveDistance(mmdSlider.value);
        if (mmdSlider.value < thresholdSlider.value)
        {
            thresholdSlider.value = mmdSlider.value;
            onThresholdchange();
        }
    }

    public void onThresholdchange()
    {
        thresholdText.text = "Threshold: " + thresholdSlider.value.ToString();
        MobileJoystick.JoystickManager.instance.setThreshold(thresholdSlider.value);
    }

}
