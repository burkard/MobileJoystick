using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MobileJoystick
{
    public class JoystickManager : MonoBehaviour
    {
        public static JoystickManager instance;

        [Header("Threshold settings")]
        [Range(0f, 0.5f)]
        [Tooltip("The minimum % distance the user must move the Joystick to consider as \"Moved\"\n\nBetween (0..1);  0.5 == 50% of the distance from the center to the edge\n\nLower values are better; *too low* ones make it too sensible - keep it around 0.15")]
        public float threshold = 0.1f;
        [Tooltip("When SET, will return ZERO when X or Y axis are less than threshold; when UNSET, if X >= threshold, Y will return its value (even if < than threshold) - and vice versa")]
        public bool independentForBothAxis = true;

        [Range(0.1f, 1.0f)]
        [Space(10)]
        [Tooltip("The max % distance the Joystick can move (vertical and horizontal)\n\nIt uses the width and height of the texture to calculate the % moved")]
        public float maxMoveDistance = 1.0f;

        [Header("Sprites and Color")]
        public Image outerUI;
        public Image innerUI;
        public Color joystickColor = Color.yellow;

        [Header("Ghost Joystick (optional)")]
        [Space(10)]
        [Tooltip("Should show the joystick when not touching screen?")]
        public bool showGhostJoystick;
        public Color ghostColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

        private float thresholdInPixels;
        private float maxMoveDistanceInPixels;

        private Vector2 ghostPos;
        private Vector2 initialPos = Vector2.zero;
        private Vector2 currentPos;
        private bool hasMoved = false;

        private int fingerIDInUse = -1;
        private Touch currentTouch;

        // delegate variables
        private delegate void UpdateDelegate();
        private UpdateDelegate checkInput;

        void Awake() {
            if (instance == null) JoystickManager.instance = this;
            if (outerUI == null || innerUI == null) Debug.LogError("ERROR -> The Image for Mobile Joystick wasn't assigned!");

            ghostPos = outerUI.rectTransform.anchoredPosition;

            maxMoveDistanceInPixels = outerUI.rectTransform.sizeDelta.x * 0.5f * maxMoveDistance;
            thresholdInPixels = outerUI.rectTransform.sizeDelta.x * 0.5f * threshold;

            if (showGhostJoystick) showGhost();
            else hideJoystick();
        }

        private void OnEnable()
        {
            if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                checkInput += checkInputPC;
            }
            else checkInput += checkInputMobile;
        }

        private void OnDisable()
        {
            if (checkInput == null) return;
            if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                checkInput -= checkInputPC;
            }
            else checkInput -= checkInputMobile;
        }

        private void Update()
        {
            checkInput();
        }

        void checkInputMobile()
        {
            currentTouch = getJoystickTouch();

            // no touches until now
            if (fingerIDInUse < 0 && currentTouch.phase == TouchPhase.Began)
            {
                fingerIDInUse = currentTouch.fingerId;
                showJoystick();
                initialPos = currentTouch.position;
                outerUI.rectTransform.anchoredPosition = initialPos;
                innerUI.rectTransform.anchoredPosition = initialPos;
            }

            // touch RELEASED:
            if (currentTouch.phase == TouchPhase.Ended || currentTouch.phase == TouchPhase.Canceled)
            {
                fingerIDInUse = -1;

                initialPos = Vector2.zero;
                currentPos = initialPos;
                if (showGhostJoystick)
                    showGhost();
                else
                    hideJoystick();
            }

            if (initialPos != Vector2.zero && fingerIDInUse != -1)
            {
                hasMoved = (Mathf.Abs(currentTouch.position.x - initialPos.x) >= thresholdInPixels) || (Mathf.Abs(currentTouch.position.y - initialPos.y) >= thresholdInPixels);

                if (hasMoved)
                {
                    currentPos = new Vector2(
                        Mathf.Clamp(currentTouch.position.x, initialPos.x - maxMoveDistanceInPixels, initialPos.x + maxMoveDistanceInPixels),
                        Mathf.Clamp(currentTouch.position.y, initialPos.y - maxMoveDistanceInPixels, initialPos.y + maxMoveDistanceInPixels)
                    );

                    innerUI.rectTransform.anchoredPosition = currentPos;
                }
                else
                {
                    innerUI.rectTransform.anchoredPosition = initialPos;
                }
            }
        }

        void checkInputPC()
        {
            hasMoved = false;

            if (initialPos == Vector2.zero && Input.GetMouseButtonDown(0))
            {
                showJoystick();
                initialPos = Input.mousePosition;
                outerUI.rectTransform.anchoredPosition = Input.mousePosition;
                innerUI.rectTransform.anchoredPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                initialPos = Vector2.zero;
                if (showGhostJoystick)
                {
                    showGhost();
                }
                else
                {
                    hideJoystick();
                }
                innerUI.rectTransform.anchoredPosition = outerUI.rectTransform.anchoredPosition;
            }

            if (initialPos != Vector2.zero)
            {
                hasMoved = (Mathf.Abs(Input.mousePosition.x - initialPos.x) >= thresholdInPixels) || (Mathf.Abs(Input.mousePosition.y - initialPos.y) >= thresholdInPixels);

                if (hasMoved)
                {
                    currentPos = new Vector2(
                        Mathf.Clamp(Input.mousePosition.x, initialPos.x - maxMoveDistanceInPixels, initialPos.x + maxMoveDistanceInPixels),
                        Mathf.Clamp(Input.mousePosition.y, initialPos.y - maxMoveDistanceInPixels, initialPos.y + maxMoveDistanceInPixels)
                    );

                    innerUI.rectTransform.anchoredPosition = currentPos;
                }
                else
                {
                    innerUI.rectTransform.anchoredPosition = initialPos;
                }
            }
        }


        private Touch getJoystickTouch()
        {
            if (fingerIDInUse < 0)
            {
                // no touches until now
                foreach (var touch in Input.touches)
                {
                    // only if touch is in lower left quadrant of the screen:
                    if (touch.position.x < Screen.width * 0.5f && touch.position.y < Screen.height * 0.5f)
                    {
                        return touch;
                    }
                }
            }

            // there's a finger touching the Joystick
            return getTouchByFingerId(fingerIDInUse);

        }

        private Touch getTouchByFingerId(int index)
        {
            foreach (var touch in Input.touches)
            {
                if (touch.fingerId == index) return touch;
            }
            Touch noTouch = new Touch();
            noTouch.phase = TouchPhase.Canceled;
            return noTouch;
        }


        // visual stuff: ----------------------------------------------------------
        private void Reset()
        {
            validateColors();
        }

        private void OnValidate()
        {
            validateColors();

            if (maxMoveDistance < threshold) threshold = maxMoveDistance;
        }

        private void validateColors()
        {
            findChildren();
            if (showGhostJoystick) ghostColors();
            else joystickColors();
        }

        private void joystickColors()
        {
            outerUI.GetComponent<Image>().color = joystickColor;
            innerUI.GetComponent<Image>().color = joystickColor;
        }

        private void ghostColors()
        {
            outerUI.GetComponent<Image>().color = ghostColor;
            innerUI.GetComponent<Image>().color = ghostColor;
        }
        private void showGhost()
        {
            innerUI.rectTransform.anchoredPosition = ghostPos;
            outerUI.rectTransform.anchoredPosition = ghostPos;
            ghostColors();
        }

        private void showJoystick()
        {
            outerUI.gameObject.SetActive(true);
            innerUI.gameObject.SetActive(true);
            joystickColors();
        }

        private void hideJoystick()
        {
            outerUI.gameObject.SetActive(false);
            innerUI.gameObject.SetActive(false);
        }

        private void findChildren()
        {
            if (innerUI == null || outerUI == null)
            {
                Canvas c = GetComponentInChildren<Canvas>();
                GameObject goC = c.gameObject;
                Transform inner = goC.transform.Find("Inner UI");
                Transform outer = goC.transform.Find("Outer UI");

                innerUI = inner.gameObject.GetComponent<Image>();
                outerUI = outer.gameObject.GetComponent<Image>();

                ghostColors();
            }
        }


        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Returns Vector2 like Cartesian Plane:: right = +X; left = -X; up = +Y; down = -Y
        /// </summary>
        /// <returns></returns>
        public Vector2 getAxis()
        {
            if (independentForBothAxis)
            {
                if (hasMoved)
                {
                    float tempXpercent = (currentPos.x - initialPos.x) / maxMoveDistanceInPixels;
                    float tempYpercent = (currentPos.y - initialPos.y) / maxMoveDistanceInPixels;

                    return new Vector2(
                        Mathf.Abs(currentPos.x - initialPos.x) >= thresholdInPixels ? tempXpercent : 0f,
                        Mathf.Abs(currentPos.y - initialPos.y) >= thresholdInPixels ? tempYpercent : 0f
                    );
                }
                return Vector2.zero;
            }
            else
            {
                float tempXpercent = (currentPos.x - initialPos.x) / maxMoveDistanceInPixels;
                float tempYpercent = (currentPos.y - initialPos.y) / maxMoveDistanceInPixels;

                return hasMoved ? new Vector2(tempXpercent, tempYpercent) : Vector2.zero;
            }
        }

        /// <summary>
        /// Change the threshold (minimum move from center) of the Joystick
        /// </summary>
        /// <param name="newThreshold">From 0 to 0.5</param>
        public void setThreshold(float newThreshold)
        {
            if (newThreshold >= 0f && newThreshold <= 0.5f) threshold = newThreshold;
            thresholdInPixels = outerUI.rectTransform.sizeDelta.x * 0.5f * threshold;
        }

        /// <summary>
        /// Change the maximum distance from the center the pad can move (in %) ::  0.5 == 50%; 1.0 == 100% :: The size of the image is taken into consideration
        /// </summary>
        /// <param name="newMMD">From 0.1 to 1</param>
        public void setMaxMoveDistance(float newMMD)
        {
            if (newMMD >= 0.1f && newMMD <= 1.0f) maxMoveDistance = newMMD;
            maxMoveDistanceInPixels = outerUI.rectTransform.sizeDelta.x * 0.5f * maxMoveDistance;
        }
    }
}
