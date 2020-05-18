using Lean.Touch;
using Lean.Touch.Extensions.ScreenAreas;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 1;

    protected Dictionary<LeanTouchScreen.SideDirection, Vector3> translationsDir = new Dictionary<LeanTouchScreen.SideDirection, Vector3>()
    {
        { LeanTouchScreen.SideDirection.LEFT, Vector3.left },
        { LeanTouchScreen.SideDirection.RIGHT, Vector3.right },
        { LeanTouchScreen.SideDirection.TOP_LEFT, Vector3.left + Vector3.up },
        { LeanTouchScreen.SideDirection.TOP_RIGHT, Vector3.right + Vector3.up },
        { LeanTouchScreen.SideDirection.DOWN_LEFT, Vector3.left + Vector3.down },
        { LeanTouchScreen.SideDirection.DOWN_RIGHT, Vector3.right + Vector3.down }
    };

    void OnEnable()
    {
        LeanTouchScreen.OnTapScreenSide += Move;
        LeanTouchScreen.OnTapScreenDiagonal += Move;
    }

    void OnDisable()
    {
        LeanTouchScreen.OnTapScreenSide -= Move;
        LeanTouchScreen.OnTapScreenDiagonal -= Move;
    }

    public virtual void Move(Vector3 position, LeanTouchScreen.SideDirection direction, LeanFinger finger)
    {
        Vector3 translation = Vector3.zero;
        if(translationsDir.TryGetValue(direction, out translation))
        {
            transform.Translate(translation * speed * Time.deltaTime, Space.World);
        }
    }
}
