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
    private UISfxManager uiSfxManager;
    private readonly Dictionary<string, Vector3> regionCameraPositions = new Dictionary<string, Vector3>
    {
        { "JungRangGu", new Vector3(1.56f, 1f, 0.132f) },
        { "JungGu", new Vector3(0.54f, 1f, -0.47f) },
        { "SeoDaemunGu", new Vector3(0.26f, 1f, -0.23f) },
        { "MaPoGu", new Vector3(0.07f, 1f, -0.27f) },
        { "Enpyeonggu", new Vector3(-0.09f, 1f, -0.10f) },
        { "JongRoGu", new Vector3(0.7845429f, 1f, -0.02f) },
        { "JongRohu", new Vector3(0.7845429f, 1f, -0.02f) },

        { "SungBukGu", new Vector3(0.99f, 1f, 0.22f) },
        { "NoWangGu", new Vector3(1.30f, 1f, 0.24f) },
        { "DoBongGu", new Vector3(0.97f, 1f, 0.43f) },
        { "GangBukGu", new Vector3(0.84f, 1f, 0.22f) },
        { "SungDongGu", new Vector3(1.17f, 1f, -0.30f) },
        { "DongDaeMunGu", new Vector3(1.14f, 1f, 0.00f) },
        { "GangSeoGu", new Vector3(-0.70f, 1f, -0.72f) },
        { "YangChunGu", new Vector3(-0.60f, 1f, -0.96f) },
        { "GuRoGu", new Vector3(-0.70f, 1f, -1.31f) },
        { "YoungDungPoGu", new Vector3(-0.20f, 1f, -1.15f) },
        { "DongJakGu", new Vector3(0.23f, 1f, -1.23f) },
        { "GeaumChunGu", new Vector3(-0.14f, 1f, -1.74f) },
        { "GwanAkGu", new Vector3(0.25f, 1f, -1.74f) },
        { "SeoChoGu", new Vector3(1.290707f, 1f, -1.32f) },
        { "GangNamGu", new Vector3(1.503132f, 1f, -1.04f) },
        { "SongPaGu", new Vector3(1.82f, 1f, -0.92f) },
        { "GangDongGu", new Vector3(2.08289f, 1f, -0.77f) },
        { "YoungsanGu", new Vector3(0.64f, 1f, -0.75f) },
        { "GwangJinGu", new Vector3(1.41f, 1f, -0.75f) },
    };

    private readonly Dictionary<string, string> regionDisplayNames = new Dictionary<string, string>
    {
        { "JungRangGu", "중랑구" },
        { "JungGu", "중구" },
        { "SeoDaemunGu", "서대문구" },
        { "MaPoGu", "마포구" },
        { "Enpyeonggu", "은평구" },
        { "JongRoGu", "종로구" },
        { "JongRohu", "종로구" },
        { "SungBukGu", "성북구" },
        { "NoWangGu", "노원구" },
        { "DoBongGu", "도봉구" },
        { "GangBukGu", "강북구" },
        { "SungDongGu", "성동구" },
        { "DongDaeMunGu", "동대문구" },
        { "GangSeoGu", "강서구" },
        { "YangChunGu", "양천구" },
        { "GuRoGu", "구로구" },
        { "YoungDungPoGu", "영등포구" },
        { "DongJakGu", "동작구" },
        { "GeaumChunGu", "금천구" },
        { "GwanAkGu", "관악구" },
        { "SeoChoGu", "서초구" },
        { "GangNamGu", "강남구" },
        { "SongPaGu", "송파구" },
        { "GangDongGu", "강동구" },
        { "YoungsanGu", "용산구" },
        { "GwangJinGu", "광진구" },
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
        string regionDisplayName = GetRegionDisplayName(regionName);

        if (terrainPanel != null)
        {
            terrainPanel.SetActive(true);
            PlayPanelOpenSfx();
        }

        if (terrainName != null)
        {
            terrainName.text = regionDisplayName;
        }

        DistrictStructurePanelManager structurePanelManager = GetComponent<DistrictStructurePanelManager>();
        if (structurePanelManager != null)
        {
            structurePanelManager.ShowRegion(regionDisplayName, regionTransform);
        }

        MoveCameraToRegion(regionName);
    }

    public string GetRegionDisplayName(string regionName)
    {
        if (regionDisplayNames.TryGetValue(regionName, out string displayName))
        {
            return displayName;
        }

        return regionName;
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

    private void PlayPanelOpenSfx()
    {
        if (uiSfxManager == null)
        {
            uiSfxManager = UISfxManager.Instance;
        }

        if (uiSfxManager != null)
        {
            uiSfxManager.PlayPanelOpen();
        }
    }
}
