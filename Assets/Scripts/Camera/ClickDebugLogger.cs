using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickDebugLogger : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform seoulRoot;
    [SerializeField] private bool logClicks = true;
    [SerializeField] private int maxPhysicsHitsToLog = 5;
    [SerializeField] private int maxUiHitsToLog = 5;

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
    }

    void Update()
    {
        if (!logClicks || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        LogUiHits();
        LogPhysicsHits();
    }

    private void LogUiHits()
    {
        if (EventSystem.current == null)
        {
            Debug.Log("[ClickDebug] UI EventSystem: none");
            return;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);

        if (uiRaycastResults.Count == 0)
        {
            Debug.Log("[ClickDebug] UI hit: none");
            return;
        }

        int count = Mathf.Min(uiRaycastResults.Count, maxUiHitsToLog);
        for (int i = 0; i < count; i++)
        {
            GameObject hitObject = uiRaycastResults[i].gameObject;
            Debug.Log("[ClickDebug] UI hit " + i + ": " + GetPath(hitObject.transform)
                + ", activeSelf=" + hitObject.activeSelf
                + ", activeInHierarchy=" + hitObject.activeInHierarchy
                + ", layer=" + LayerMask.LayerToName(hitObject.layer));
        }
    }

    private void LogPhysicsHits()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
        {
            Debug.Log("[ClickDebug] Physics camera: none");
            return;
        }

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        if (hits.Length == 0)
        {
            Debug.Log("[ClickDebug] Physics hit: none");
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        int count = Mathf.Min(hits.Length, maxPhysicsHitsToLog);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            GameObject hitObject = hit.collider.gameObject;
            Transform district = FindDirectSeoulChild(hitObject.transform);
            Renderer renderer = hitObject.GetComponent<Renderer>();

            Debug.Log("[ClickDebug] Physics hit " + i + ": " + GetPath(hitObject.transform)
                + ", distance=" + hit.distance.ToString("F3")
                + ", collider=" + hit.collider.GetType().Name
                + ", colliderEnabled=" + hit.collider.enabled
                + ", isTrigger=" + hit.collider.isTrigger
                + ", rendererEnabled=" + (renderer != null ? renderer.enabled.ToString() : "none")
                + ", activeSelf=" + hitObject.activeSelf
                + ", activeInHierarchy=" + hitObject.activeInHierarchy
                + ", layer=" + LayerMask.LayerToName(hitObject.layer)
                + ", seoulDistrict=" + (district != null ? district.name : "none"));
        }
    }

    private Transform FindDirectSeoulChild(Transform target)
    {
        if (seoulRoot == null || target == null)
        {
            return null;
        }

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

    private string GetPath(Transform target)
    {
        if (target == null)
        {
            return "null";
        }

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
