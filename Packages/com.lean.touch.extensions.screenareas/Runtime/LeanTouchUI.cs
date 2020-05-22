using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lean.Touch.Extensions.ScreenAreas
{
    public class LeanTouchUI
    {
		// Used to find if the GUI is in use
		protected static List<RaycastResult> tempRaycastResults = new List<RaycastResult>(10);

		// Used by RaycastGui
		protected static EventSystem tempEventSystem;

		// Used by RaycastGui
		protected static PointerEventData tempPointerEventData;

		// ---- Events ----
		public static Action<Vector3, LeanTouchScreen.SideDirection, GameObject, LeanFinger> OnTapUI;

		public LeanFingerTap FingerTap { get; set; }

		public LeanFinger TouchedFinger { get; set; }

		public LayerMask layerToRaycast { get; set; }

		public bool IgnoreUI
		{
			get
			{
				if (TouchedFinger == null)
				{
					return false;
				}

				return FingerTap != null && FingerTap.isActiveAndEnabled && (FingerTap.IgnoreIsOverGui || FingerTap.IgnoreStartedOverGui) && TouchedFinger.IsOverGui;
			}
		}

		public LeanTouchUI(){ }

		/// <summary>This will return all the RaycastResults under the specified screen point using the specified layerMask.
		/// NOTE: The first result (0) will be the top UI element that was first hit.</summary>
		public static List<RaycastResult> Raycast(Vector2 screenPosition, LayerMask layerMask)
		{
			tempRaycastResults.Clear();

			var currentEventSystem = EventSystem.current;

			if (currentEventSystem != null)
			{
				// Create point event data for this event system?
				if (currentEventSystem != tempEventSystem)
				{
					tempEventSystem = currentEventSystem;

					if (tempPointerEventData == null)
					{
						tempPointerEventData = new PointerEventData(tempEventSystem);
					}
					else
					{
						tempPointerEventData.Reset();
					}
				}

				// Raycast event system at the specified point
				tempPointerEventData.position = screenPosition;

				currentEventSystem.RaycastAll(tempPointerEventData, tempRaycastResults);

				// Loop through all results and remove any that don't match the layer mask
				if (tempRaycastResults.Count > 0)
				{
					for (var i = tempRaycastResults.Count - 1; i >= 0; i--)
					{
						var raycastResult = tempRaycastResults[i];
						var raycastLayer = 1 << raycastResult.gameObject.layer;
						string layerName = LayerMask.LayerToName(raycastResult.gameObject.layer);

						// PS: That was changed from LeanTouch.RaycastGui()
						if (!LayerMask.GetMask(layerName).Equals(raycastLayer))
						{
							tempRaycastResults.RemoveAt(i);
						}
					}
				}
			}
			else
			{
				Debug.LogError("Failed to RaycastGui because your scene doesn't have an event system! To add one, go to: GameObject/UI/EventSystem");
			}

			return tempRaycastResults;
		}

		public virtual GameObject GetTouched(LeanFinger finger)
		{
			var results = Raycast(finger.ScreenPosition, layerToRaycast);
			GameObject uiElement = null;

			if (results != null && results.Count > 0)
			{
				uiElement = results[0].gameObject;
			}

			return uiElement;
		}

		public virtual GameObject GetTouched(Vector2 position)
		{
			var results = Raycast(position, layerToRaycast);
			GameObject uiElement = null;

			if (results != null && results.Count > 0)
			{
				uiElement = results[0].gameObject;
			}

			return uiElement;
		}

		public virtual bool IsTouched(Vector2 position, out GameObject element)
		{
			element = GetTouched(position);
			return element != null;
		}

		public virtual bool IsTouched(LeanFinger finger, out GameObject element)
		{
			element = GetTouched(finger);
			return IsTouched(finger, element);
		}

		public virtual bool IsTouched(LeanFinger finger, GameObject element = null)
		{
			finger = finger ?? TouchedFinger;

			if (finger == null)
			{
				return false;
			}

			if (FingerTap != null && FingerTap.isActiveAndEnabled)
			{
				return !FingerTap.IgnoreIsOverGui && !FingerTap.IgnoreStartedOverGui && finger.IsOverGui;
			} else
			{
				return element != null;
			}
		}
	}

}
