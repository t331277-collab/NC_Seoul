using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject terrainPanel;
    [SerializeField] private TextMeshProUGUI terrainName;
    [SerializeField] private Transform mainCamera;

    private Coroutine cameraMoveCoroutine;
    private readonly Dictionary<string, Vector3> regionCameraPositions = new Dictionary<string, Vector3>
    {
        { "JungRangGu", new Vector3(0.53f, 1f, -0.80f) },
        { "JungGu", new Vector3(0.54f, 1f, -0.47f) },
        { "SeoDaemunGu", new Vector3(0.07f, 1f, -0.40f) },
        { "MaPoGu", new Vector3(-0.12f, 1f, -0.69f) },
        { "Enpyeonggu", new Vector3(-0.09f, 1f, -0.10f) },
        { "JongRoGu", new Vector3(0.31f, 1f, -0.20f) },
        { "JongRohu", new Vector3(0.31f, 1f, -0.20f) },

        { "SungBukGu", new Vector3(0.38f, 1f, -0.14f) },
        { "NoWangGu", new Vector3(1.30f, 1f, 0.24f) },
        { "DoBongGu", new Vector3(0.97f, 1f, 0.43f) },
        { "GangBukGu", new Vector3(0.84f, 1f, 0.22f) },
        { "SungDongGu", new Vector3(1.17f, 1f, -0.30f) },
        { "DongDaeMunGu", new Vector3(1.04f, 1f, -0.63f) },
        { "GangSeoGu", new Vector3(-0.70f, 1f, -0.72f) },
        { "YangChunGu", new Vector3(-0.60f, 1f, -0.96f) },
        { "GuRoGu", new Vector3(-0.70f, 1f, -1.31f) },
        { "YoungDungPoGu", new Vector3(-0.20f, 1f, -1.15f) },
        { "DongJakGu", new Vector3(0.23f, 1f, -1.23f) },
        { "GeaumChunGu", new Vector3(-0.14f, 1f, -1.74f) },
        { "GwanAkGu", new Vector3(0.25f, 1f, -1.74f) },
        { "SeoChoGu", new Vector3(1.09f, 1f, -1.77f) },
        { "GangNamGu", new Vector3(1.30f, 1f, -1.45f) },
        { "SongPaGu", new Vector3(1.75f, 1f, -1.32f) },
        { "GangDongGu", new Vector3(1.97f, 1f, -1.08f) },
        { "YoungsanGu", new Vector3(0.64f, 1f, -0.75f) },
        { "GwangJinGu", new Vector3(1.41f, 1f, -0.75f) },
    };

    void Start()
    {
        if (terrainPanel != null)
        {
            terrainPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (terrainPanel != null && terrainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTerrainPanel();
        }
    }


    public void ShowTerrainPanel(string regionName, Transform regionTransform)
    {
        if (terrainPanel != null)
        {
            terrainPanel.SetActive(true);
        }

        if (terrainName != null)
        {
            terrainName.text = regionName;
        }

        MoveCameraToRegion(regionName);
    }

    public void CloseTerrainPanel()
    {
        if (terrainPanel != null)
        {
            terrainPanel.SetActive(false);
        }
    }

    private void MoveCameraToRegion(string regionName)
    {
        if (mainCamera == null || !regionCameraPositions.TryGetValue(regionName, out Vector3 targetPosition))
        {
            return;
        }

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
        }

        cameraMoveCoroutine = StartCoroutine(MoveCamera(targetPosition));
    }

    private IEnumerator MoveCamera(Vector3 targetPosition)
    {
        const float moveDuration = 0.2f;

        Vector3 startPosition = mainCamera.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            mainCamera.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            yield return null;
        }

        mainCamera.position = targetPosition;
        cameraMoveCoroutine = null;
    }
}
