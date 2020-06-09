using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lean.Touch.Extensions.ScreenAreas
{
    public class LeanTouchScreen : MonoBehaviour
    {
        public enum SideDirection
        {
            NONE,
            MIDDLE,
            LEFT,
            RIGHT,
            TOP_LEFT,
            TOP_RIGHT,
            DOWN_LEFT,
            DOWN_RIGHT
        }

        public class DirectionData
        {
            public DirectionData() { }

            public Vector2 IndicatorSidePos
            {
                get
                {
                    return sidePos;
                }
                set
                {
                    sidePos = value;
                }
            }
            public Action<Vector3, LeanFinger> OnTapDirection { get; set; }

            protected Vector2 sidePos = Vector2.zero;

        }

        // ---- Events ----
        public static Action<Vector3, SideDirection, LeanFinger> OnTapScreenSide;
        public static Action<Vector3, SideDirection, LeanFinger> OnTapScreenDiagonal;

        [Header("Touch references")]
        [Tooltip("The player reference to check the screen sides")]
        [SerializeField]
        protected GameObject player;

        [Tooltip("If player is in Isometric plan, the reference will be change Y => Z")]
        [SerializeField]
        protected bool isIsometric = false;

        [Tooltip("The camera to calculate where show gizmos, touch indicators and sides")]
        [SerializeField]
        protected Camera cam;
        
        [Header("Touch indicator")]
        [Tooltip("Duration in seconds of touch indicator circle")]
        [SerializeField]
        protected float duration = 2;

        [Tooltip("The size of the touch indicator circle")]
        [SerializeField]
        [Range(1, 100)]
        protected float radius = 1;

        [Header("Directions")]
        [Tooltip("Check directions: Top + Left/Right and Down + Left/Right?")]
        [SerializeField]
        protected bool diagonals = true;

        [Header("Middle configuration")]
        [Tooltip("Do nothing if a screen middle is touched")]
        [SerializeField]
        protected bool cancelMiddle = true;

        [Tooltip("The X and Y values of the DISTANCE between the touch and the middle will be ignored")]
        [SerializeField]
        protected Vector2 middleDistance = Vector2.right;

        [Header("UI Touch")]
		[Tooltip("The layers you want the raycast/overlap to hit.")]
        public LayerMask layerMask = Physics.DefaultRaycastLayers;

        protected Vector3 touchWorldPos = Vector3.zero;
        protected SideDirection lastTouchedDirection = SideDirection.NONE;

        // --- Components ----
        protected LeanFinger touchedFinger;
        protected LeanFingerTap fingerTap;
        protected LeanTouchUI touchUI;
        protected UnityEngine.Touch touch;

        private Vector3 screenMiddleRaw = new Vector2(Screen.width / 2, Screen.height / 2);
        private Vector3 screenWorldPos = Vector3.zero;

        private Dictionary<SideDirection, DirectionData> directionsActions = default;

        public Vector3 ScreenMiddlePos
        {
            get
            {
                screenMiddleRaw = new Vector2(Screen.width / 2, Screen.height / 2);
                if (fingerTap != null && fingerTap.isActiveAndEnabled)
                {
                    return fingerTap.ScreenDepth.Convert(screenMiddleRaw, fingerTap.gameObject);
                }
                return screenMiddleRaw;
            }
        }

        public Vector3 ReferenceTouch
        {
            get
            {
                Vector3 reference = screenMiddleRaw;

                if (player != null)
                {
                    reference = player.transform.position;

                    // TODO: Create a Editor script to show this field, only if player != null
                    if (isIsometric)
                    {
                        reference.y = reference.z;
                    }
                }

                return reference;
            }

            protected set { }
        }

        void Awake()
        {
            fingerTap = GetComponentInChildren<LeanFingerTap>();
            layerMask = layerMask.value == LayerMask.NameToLayer("Default") ? LayerMask.NameToLayer("UI") : layerMask.value;
            cam = LeanTouch.GetCamera(cam);

            touchUI = new LeanTouchUI {
                FingerTap = fingerTap,
                layerToRaycast = layerMask
            };

            screenWorldPos = cam.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

            directionsActions = new Dictionary<SideDirection, DirectionData>()
            {
                { SideDirection.LEFT, new DirectionData {
                    OnTapDirection = (Vector3 pos, LeanFinger finger) => OnTapScreenSide?.Invoke(pos, SideDirection.LEFT, finger)
                } },
                { SideDirection.RIGHT, new DirectionData {
                    OnTapDirection = (Vector3 pos, LeanFinger finger) => OnTapScreenSide?.Invoke(pos, SideDirection.RIGHT, finger)
                } },
                { SideDirection.TOP_LEFT, new DirectionData {
                    IndicatorSidePos = new Vector2(-screenWorldPos.x, screenWorldPos.y),
                    OnTapDirection = (Vector3 pos, LeanFinger finger) => OnTapScreenDiagonal?.Invoke(pos, SideDirection.TOP_LEFT, finger)
                }},
                { SideDirection.TOP_RIGHT, new DirectionData {
                    IndicatorSidePos = new Vector2(screenWorldPos.x, screenWorldPos.y),
                    OnTapDirection = (Vector3 pos, LeanFinger finger) => OnTapScreenDiagonal?.Invoke(pos, SideDirection.TOP_RIGHT, finger)
                } },
                { SideDirection.DOWN_LEFT, new DirectionData {
                    IndicatorSidePos = new Vector2(-screenWorldPos.x, -screenWorldPos.y),
                    OnTapDirection = (Vector3 pos, LeanFinger finger) => OnTapScreenDiagonal?.Invoke(pos, SideDirection.DOWN_LEFT, finger)
                } },
                { SideDirection.DOWN_RIGHT, new DirectionData {
                    IndicatorSidePos = new Vector2(screenWorldPos.x, -screenWorldPos.y),
                    OnTapDirection = (Vector3 pos, LeanFinger finger) => OnTapScreenDiagonal?.Invoke(pos, SideDirection.DOWN_RIGHT, finger)
                } }
            };
        }

#if UNITY_EDITOR

        void OnDrawGizmos()
        {
            DrawMiddle();
            DrawTouchDiagonal();
            StartCoroutine(DrawTouchIndicator(duration));
        }

        void DrawMiddle()
        {
            Handles.color = cancelMiddle && lastTouchedDirection.Equals(SideDirection.MIDDLE) ? Color.red : Color.blue;

            // Y Axis line
            Handles.DrawDottedLine(new Vector3(0, screenMiddleRaw.y, 1), new Vector3(0, -screenMiddleRaw.y, 1), 10);

            if (diagonals)
            {
                // X axis line
                // TODO: Refactor this do use "Handles.DrawDottedLines(Vector3[])" method
                Handles.color = Color.blue;
                Handles.DrawDottedLine(new Vector3(screenMiddleRaw.x, 0, 1), new Vector3(-screenMiddleRaw.x, 0, 1), 10);
            }

            if (cancelMiddle && lastTouchedDirection.Equals(SideDirection.MIDDLE))
            {
                Handles.color = Color.red;
                Handles.DrawSolidDisc(Vector3.zero, Vector3.forward, 2);
            }
        }

        void DrawTouchDiagonal()
        {

            if (diagonals && screenWorldPos != Vector3.zero && (touchedFinger != null && touchedFinger.IsActive))
            {
                if (touchUI != null && touchUI.IgnoreUI)
                {
                    return;
                }

                Color color = Color.white;
                color.a = 0.3f;

                Handles.color = color;

                var rectSize = Vector2.zero;
                if (directionsActions.TryGetValue(lastTouchedDirection, out var directionData))
                {
                    rectSize = directionData.IndicatorSidePos;
                }

                if (!lastTouchedDirection.Equals(SideDirection.NONE))
                {
                    Rect rectangle = new Rect(position: Vector2.zero, size: rectSize);

                    Handles.DrawSolidRectangleWithOutline(rectangle, color, color);
                }
            }
        }

        IEnumerator DrawTouchIndicator(float seconds = 2)
        {
            float touchRadius = 0;

            if (touchedFinger != null && touchedFinger.IsActive)
            {
                if (touchUI != null && touchUI.IgnoreUI)
                {
                    Color ignoreColor = Color.red;
                    ignoreColor.a = 0.3f;
                    Handles.color = ignoreColor;
                }
                else
                {
                    Handles.color = Color.green;
                }

                touchRadius = GetTouchRadius();
            }

            //Draw a solid disc in the coordinates in a space in the game world
            Handles.DrawSolidDisc(touchWorldPos, Vector3.forward, touchRadius);
            yield return new WaitForSeconds(seconds);
        }
#endif

        void OnEnable()
        {
            if (fingerTap != null && fingerTap.isActiveAndEnabled)
            {
                fingerTap.OnFinger.AddListener(TapScreen);
            }
            else
            {
                LeanTouch.OnFingerTap += TapScreen;
            }

#if UNITY_EDITOR
            // Subscribe this event, only in editor (Gizmos debug)
            LeanTouch.OnFingerSet += TapIndicator;
#endif
        }

        void OnDisable()
        {
            if (fingerTap != null && fingerTap.isActiveAndEnabled)
            {
                fingerTap.OnFinger.RemoveListener(TapScreen);
            }
            else
            {
                LeanTouch.OnFingerTap -= TapScreen;
            }

#if UNITY_EDITOR
            // Unsubscribe this event, only in editor (Gizmos debug)
            LeanTouch.OnFingerSet -= TapIndicator;
#endif
        }

        public virtual void TapScreen(LeanFinger finger)
        {
            TapScreenVerify(finger.LastScreenPosition, finger);
        }

        public virtual void TapScreenVerify(Vector3 touchPosition, LeanFinger finger = null)
        {
            GameObject uiElement;
            bool touchedInUI = finger != null ? touchUI.IsTouched(finger, out uiElement) : touchUI.IsTouched(touchPosition, out uiElement);
            

            if (touchedInUI)
            {
                var direction = GetDirection(touchPosition);
                
                LeanTouchUI.OnTapUI?.Invoke(touchPosition, direction, uiElement, finger);
            } else
            {
                var callbackEvent = GetDirectionAction(touchPosition);
                callbackEvent?.Invoke(touchPosition, finger);
            }
        }

        /// <summary>
        /// Get touch data (touch world position, direction...)
        ///<b>PS:</b> This is only to debug
        /// </summary>
        /// <param name="finger">The last touch touch information object</param>
        public virtual void TapIndicator(LeanFinger finger)
        {
            touchWorldPos = cam.ScreenToWorldPoint(finger.LastScreenPosition);

            touchedFinger = finger;
            lastTouchedDirection = GetDirection(touchWorldPos);
        }

        public virtual UnityEngine.Touch GetTouch(int index = 0)
        {
            if (Input.touchCount > 0)
            {
                touch = Input.GetTouch(index);
            }

            return touch;
        }

        public virtual Action<Vector3, LeanFinger> GetDirectionAction(Vector3 position)
        {
            SideDirection direction = GetDirection(position);

            // C# 7.0+ Syntax: Use "out" + "var" in the same sentence
            if (directionsActions.TryGetValue(direction, out var directionData))
            {
                return directionData.OnTapDirection;
            }

            return default;
        }

        public virtual SideDirection GetDirection(Vector3 position)
        {
            SideDirection direction = IsScreenLeft(position);

            if (direction.Equals(SideDirection.NONE))
            {
                direction = IsScreenRight(position);
            }

            return direction;
        }

        public virtual float GetTouchRadius()
        {
            touch = GetTouch();

            if (touch.radius > 0)
            {
                radius = touch.radius + touch.radiusVariance;
            }

            return radius;
        }

        public SideDirection IsScreenLeft(Vector3 touchPosition)
        {

            if (cancelMiddle && IsScreenMiddle(touchPosition).Equals(SideDirection.MIDDLE))
            {
                return SideDirection.NONE;
            }

            if (touchPosition != Vector3.zero)
            {
                /**
                 * Left: Only change "lastTouchedDirection" property when the 
                 * direction is not equal to SideDirection.NONE
                 * 
                 * This to not impact the Right directions
                 */
                if (touchPosition.x < ReferenceTouch.x)
                {
                    if (diagonals && touchPosition.y > ReferenceTouch.y)
                    {
                        lastTouchedDirection = SideDirection.TOP_LEFT;
                    }
                    else if (diagonals && touchPosition.y < ReferenceTouch.y)
                    {
                        lastTouchedDirection = SideDirection.DOWN_LEFT;
                    }
                    else
                    {
                        lastTouchedDirection = SideDirection.LEFT;
                    }

                    return lastTouchedDirection;
                }
            }

            return SideDirection.NONE;
        }

        public SideDirection IsScreenRight(Vector3 touchPosition)
        {

            if (cancelMiddle && IsScreenMiddle(touchPosition).Equals(SideDirection.MIDDLE))
            {
                return SideDirection.NONE;
            }

            if (touchPosition != Vector3.zero)
            {

                // Right
                if (touchPosition.x > ReferenceTouch.x)
                {
                    if (diagonals && touchPosition.y > ReferenceTouch.y)
                    {
                        lastTouchedDirection = SideDirection.TOP_RIGHT;
                    }
                    else if (diagonals && touchPosition.y < ReferenceTouch.y)
                    {
                        lastTouchedDirection = SideDirection.DOWN_RIGHT;
                    }
                    else
                    {
                        lastTouchedDirection = SideDirection.RIGHT;
                    }

                    return lastTouchedDirection;
                }

            }

            return SideDirection.NONE;
        }

        /// <summary>
        /// Check if the touch is on screen middle
        /// </summary>
        /// <param name="touchPosition">The Vector2 with touched screen positions</param>
        /// <param name="checkAxis">A tuple with X,Y booleans to verify the distance from touch</param>
        /// <returns>The direction. Returns <b>SideDirection.MIDDLE</b> if screen middle is touched </returns>
        public SideDirection IsScreenMiddle(Vector2 touchPosition, (bool? x, bool? y) checkAxis = default)
        {
            var posForDistance = (touch: Vector2.zero, screen: Vector2.zero);

            bool x = checkAxis.x ?? false;
            bool y = checkAxis.y ?? false;

            if (x == false && y == false)
            {
                x = true;
            }

            if (x)
            {
                posForDistance.touch.x = touchPosition.x;
                posForDistance.screen.x = ReferenceTouch.x;
            }

            if (y)
            {
                posForDistance.touch.y = touchPosition.y;
                posForDistance.screen.y = ReferenceTouch.y;
            }

            /**
             * Ignore the Y coordinates for check the middle
             * 
             * Ps: rawDistance variable was assigned for debug breakpoints
             */
            float rawDistance = Vector2.Distance(posForDistance.touch, posForDistance.screen);
            var distance = Mathf.Round(rawDistance);

            if (distance <= middleDistance.x)
            {
                lastTouchedDirection = SideDirection.MIDDLE;
                return lastTouchedDirection;
            }

            return SideDirection.NONE;
        }
    }
}
