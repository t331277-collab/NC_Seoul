using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TerrainRaycastClickManager : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform seoulRoot;
    [SerializeField] private UIManager uiManager;

    private static readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (seoulRoot == null)
        {
            GameObject seoul = GameObject.Find("Seoul");
            if (seoul != null)
            {
                seoulRoot = seoul.transform;
            }
        }

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0) || IsPointerOverUi())
        {
            return;
        }

        Transform district = FindClickedDistrict();
        if (district != null && uiManager != null)
        {
            uiManager.ShowTerrainPanel(district.name, district);
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

    private Transform FindClickedDistrict()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null || seoulRoot == null)
        {
            return null;
        }

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        if (hits.Length == 0)
        {
            return null;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform district = FindDirectSeoulChild(hits[i].collider.transform);
            if (district != null)
            {
                return district;
            }
        }

        return null;
    }

    private Transform FindDirectSeoulChild(Transform target)
    {
        Transform current = target;
        while (current != null && current.parent != null)
        {
            if (current.parent == seoulRoot)
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }
}
