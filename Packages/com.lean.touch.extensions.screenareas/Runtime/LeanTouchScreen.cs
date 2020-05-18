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

            public Vector2 IndicatorSidePos { 
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

        [Header("Touch indicator")]
        [Tooltip("Duration in seconds of touch indicator circle")]
        public float duration = 2;

        [Header("Directions")]
        [Tooltip("Check directions: Top + Left/Right and Down + Left/Right?")]
        public bool diagonals = true;

        [Header("Middle configuration")]
        [Tooltip("Do nothing if a screen middle is touched")]
        public bool cancelMiddle = true;
        public Vector2 middleDistance = Vector2.right;

        // ---- Events ----
        public static Action<Vector3, SideDirection, LeanFinger> OnTapScreenSide;
        public static Action<Vector3, SideDirection, LeanFinger> OnTapScreenDiagonal;

        protected Vector3 touchWorldPos = Vector3.zero;
        protected SideDirection lastTouchedDirection = SideDirection.NONE;

        // --- Components ----
        protected LeanFinger touchedFinger;
        protected LeanFingerTap fingerTap;
        protected UnityEngine.Touch touch;
        protected Camera cam;

        private Vector3 screenMiddleRaw = new Vector2(Screen.width / 2, Screen.height / 2);
        private Vector3 screenWorldPos = Vector3.zero;

        private Dictionary<SideDirection, DirectionData> directionsActions = default;

        public Vector3 ScreenMiddlePos
        {
            get {
                screenMiddleRaw = new Vector2(Screen.width / 2, Screen.height / 2);
                if (fingerTap != null)
                {
                    return fingerTap.ScreenDepth.Convert(screenMiddleRaw, fingerTap.gameObject);
                }
                return screenMiddleRaw;
            }
        }

        void Awake()
        {
            fingerTap = GetComponentInChildren<LeanFingerTap>();

            cam = LeanTouch.GetCamera(null);
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
                if (fingerTap != null && (fingerTap.IgnoreIsOverGui || fingerTap.IgnoreStartedOverGui) && touchedFinger.IsOverGui)
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
            float radius = 0;
            if (touchWorldPos != Vector3.zero && (touchedFinger != null && touchedFinger.IsActive))
            {
                if (fingerTap != null && (fingerTap.IgnoreIsOverGui || fingerTap.IgnoreStartedOverGui) && touchedFinger.IsOverGui)
                {
                    Color ignoreColor = Color.red;
                    ignoreColor.a = 0.3f;
                    Handles.color = ignoreColor;
                }
                else
                {
                    Handles.color = Color.green;
                }

                radius = GetTouchRadius();
            }

            Handles.DrawSolidDisc(touchWorldPos, Vector3.forward, radius);
            yield return new WaitForSeconds(seconds);
        }
#endif

        void OnEnable()
        {
            if (fingerTap != null)
            {
                fingerTap.OnPosition.AddListener(TapScreenPos);
            } else
            {
                LeanTouch.OnFingerTap += TapScreen;
            }

            LeanTouch.OnFingerSet += TapIndicator;
        }

        void OnDisable()
        {
            if (fingerTap != null)
            {
                fingerTap.OnPosition.RemoveListener(TapScreenPos);
            }
            else
            {
                LeanTouch.OnFingerTap -= TapScreen;
            }

            LeanTouch.OnFingerSet -= TapIndicator;
        }

        public virtual void TapScreen(LeanFinger finger)
        {
            TapScreenVerify(finger.LastScreenPosition, finger);
        }

        public virtual void TapScreenPos(Vector3 position)
        {
            TapScreenVerify(position);
        }

        public virtual void TapScreenVerify(Vector3 position, LeanFinger finger = null)
        {
            var callbackEvent = GetDirectionAction(position);
            callbackEvent?.Invoke(position, finger);
        }

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
            float radius = 1;
            touch = GetTouch();

            if (touch.radius > 0)
            {
                radius = touch.radius + touch.radiusVariance;
            }

            return radius;
        }

        public SideDirection IsScreenLeft(Vector2 touchPosition)
        {
            if (cancelMiddle && IsScreenMiddle(touchPosition).Equals(SideDirection.MIDDLE))
            {
                return SideDirection.NONE;
            }

            if (touchPosition != Vector2.zero)
            {
                /**
                 * Left: Only change "lastTouchedDirection" property when the 
                 * direction is not equal to SideDirection.NONE
                 * 
                 * This to not impact the Right directions
                 */
                if (touchPosition.x < ScreenMiddlePos.x)
                {
                    if (diagonals && touchPosition.y > ScreenMiddlePos.y)
                    {
                        lastTouchedDirection = SideDirection.TOP_LEFT;
                    } else if (diagonals && touchPosition.y < ScreenMiddlePos.y)
                    {
                        lastTouchedDirection = SideDirection.DOWN_LEFT;
                    } else
                    {
                        lastTouchedDirection = SideDirection.LEFT;
                    }

                    return lastTouchedDirection;
                }
            }

            return SideDirection.NONE;
        }

        public SideDirection IsScreenRight(Vector2 touchPosition)
        {

            if (cancelMiddle && IsScreenMiddle(touchPosition).Equals(SideDirection.MIDDLE))
            {
                return SideDirection.NONE;
            }

            if (touchPosition != Vector2.zero)
            {

                // Right
                if (touchPosition.x > ScreenMiddlePos.x)
                {
                    if (diagonals && touchPosition.y > ScreenMiddlePos.y)
                    {
                        lastTouchedDirection = SideDirection.TOP_RIGHT;
                    }
                    else if (diagonals && touchPosition.y < ScreenMiddlePos.y)
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
                posForDistance.screen.x = ScreenMiddlePos.x;
            }

            if (y)
            {
                posForDistance.touch.y = touchPosition.y;
                posForDistance.screen.y = ScreenMiddlePos.y;
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
