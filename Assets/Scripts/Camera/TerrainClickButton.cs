using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TerrainClickButton : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Transform regionTransform;
    [SerializeField] private string regionName;

    private static readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    void OnMouseDown()
    {
        if (IsPointerOverUi())
        {
            return;
        }

        if (uiManager != null)
        {
            uiManager.ShowTerrainPanel(regionName, regionTransform);
        }
    }

    private bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);
        return uiRaycastResults.Count > 0;
    }

}
