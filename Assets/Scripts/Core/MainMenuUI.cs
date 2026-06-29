using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private const string InGameSceneName = "InGameScene";
    private const float LicenseFirstItemY = 282f;
    private const float LicenseWheelStep = 90f;
    private const string LicenseDescriptionText =
@"정선 아리랑체(글꼴, 폰트)

본 저작물은 강원특별자치도에서 2019년작성하여 공공누리 제1 유형으로 개방한 정선아리랑체(작성자:강원특별자치도 정선군)를 이용하였으며, 해당 저작물은 공공누리(기관명), https://www.kogl.or.kr/recommend/recommendDivView.do?oc=&recommendIdx=9524&division=font(홈페이지 주소)에서 무료로 다운받으실 수 있습니다.


서울 한강체(4종)

본 저작물은 서울 특별시에서 2008년작성하여 공공누리 제1 유형으로 개방한 서울한강체(작성자:서울특별시)를 이용하였으며, 해당 저작물은 서울특별시(기관명), https://www.seoul.go.kr/seoul/font.do홈페이지 주소)에서 무료로 다운받으실 수 있습니다.


국악연주곡_아리랑 해금연주 오케스트라ver.

본 저작물 국악연주곡_아리랑 해금연주 오케스트라ver. 은 한국저작권위원회에서 제작하였으며 CC BY 4.0 라이선스에 따라 사용되었습니다.
저작자: 한국저작권위원회
창작년도: 2020-11-20
원본 출처: https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?menuNo=200275&wrtSn=13262876
라이선스: CC BY 4.0
라이선스 안내: https://creativecommons.org/licenses/by/4.0/


국악연주곡_강원도아리랑

본 저작물 국악연주곡_강원도아리랑 은 한국저작권위원회에서 제작하였으며 CC BY 4.0 라이선스에 따라 사용되었습니다.
저작자: 한국저작권위원회
원본 출처: https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?menuNo=200018&wrtSn=13263024&utm_source=chatgpt.com
창작년도: 2020-11-20
라이선스: CC BY 4.0
라이선스 안내: https://creativecommons.org/licenses/by/4.0/


국악연주곡_경기아리랑

본 저작물 국악연주곡_경기아리랑 은 한국저작권위원회에서 제작하였으며 CC BY 4.0 라이선스에 따라 사용되었습니다.
저작자: 한국저작권위원회
원본 출처: https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?menuNo=200275&wrtSn=13263029&utm_source=chatgpt.com
창작년도: 2020-11-20
라이선스: CC BY 4.0
라이선스 안내: https://creativecommons.org/licenses/by/4.0/


국악연주곡_진도아리랑

본 저작물 국악연주곡_경기아리랑 은 한국저작권위원회에서 제작하였으며 CC BY 4.0 라이선스에 따라 사용되었습니다.
저작자: 한국저작권위원회
원본 출처: https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?menuNo=200197&wrtSn=13263061&utm_source=chatgpt.com
창작년도: 2020-11-20
라이선스: CC BY 4.0
라이선스 안내: https://creativecommons.org/licenses/by/4.0/


국악연주곡_진도아리랑 피아노ver.

본 저작물
국악연주곡_진도아리랑 피아노ver. 은 한국저작권위원회에서 제작하였으며 CC BY 4.0 라이선스에 따라 사용되었습니다.
저작자: 한국저작권위원회
원본 출처: https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?menuNo=200020&wrtSn=13263062
창작년도: 2020-11-20
라이선스: CC BY 4.0
라이선스 안내: https://creativecommons.org/licenses/by/4.0/";

    private GameObject titleGameStartButtonObject;
    private GameObject titleLicenseButtonObject;
    private GameObject gameStartPanel;
    private GameObject gameStartButtonObject;
    private GameObject licensePanel;
    private GameObject licensePanel2;
    private Button titleGameStartButton;
    private Button titleLicenseButton;
    private Button gameStartButton;
    private Button licenseNextButton;
    private TMP_InputField nameInput;
    private RectTransform licenseTemplate;
    private RectTransform licenseContentRoot;
    private Scrollbar licenseScrollbar;
    private bool licenseContentBuilt;
    private float licenseScrollOffset;
    private float licenseMaxScrollOffset;

    private void Awake()
    {
        BindSceneObjects();
        InitializeState();
    }

    private void Update()
    {
        UpdateLicenseCloseInput();
        UpdateLicenseScrollInput();
    }

    private void OnEnable()
    {
        BindSceneObjects();
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    private void BindSceneObjects()
    {
        Transform uiRoot = transform;
        if (gameObject.name != "UI")
        {
            GameObject uiObject = GameObject.Find("UI");
            if (uiObject != null)
            {
                uiRoot = uiObject.transform;
            }
        }

        Transform titleButtonTransform = uiRoot.Find("TitlePanel/GameStartBtn");
        if (titleButtonTransform != null)
        {
            titleGameStartButtonObject = titleButtonTransform.gameObject;
            titleGameStartButton = titleButtonTransform.GetComponent<Button>();
        }

        Transform titleLicenseButtonTransform = uiRoot.Find("TitlePanel/LicenseBtn");
        if (titleLicenseButtonTransform != null)
        {
            titleLicenseButtonObject = titleLicenseButtonTransform.gameObject;
            titleLicenseButton = titleLicenseButtonTransform.GetComponent<Button>();
        }

        Transform gameStartPanelTransform = uiRoot.Find("GameStartPanel");
        if (gameStartPanelTransform != null)
        {
            gameStartPanel = gameStartPanelTransform.gameObject;

            Transform nameInputTransform = gameStartPanelTransform.Find("NameInput");
            if (nameInputTransform != null)
            {
                nameInput = nameInputTransform.GetComponent<TMP_InputField>();
            }

            Transform gameStartButtonTransform = gameStartPanelTransform.Find("GameStartBtn");
            if (gameStartButtonTransform != null)
            {
                gameStartButtonObject = gameStartButtonTransform.gameObject;
                gameStartButton = gameStartButtonTransform.GetComponent<Button>();
            }
        }

        Transform licensePanelTransform = uiRoot.Find("LicensePanel");
        if (licensePanelTransform == null)
        {
            licensePanelTransform = uiRoot.Find("LicenscPanel");
        }

        if (licensePanelTransform != null)
        {
            licensePanel = licensePanelTransform.gameObject;
            licenseTemplate = licensePanelTransform.Find("Template") as RectTransform;
            Transform licenseNextButtonTransform = licensePanelTransform.Find("NextBtn");
            if (licenseNextButtonTransform != null)
            {
                licenseNextButton = licenseNextButtonTransform.GetComponent<Button>();
            }

            if (licenseTemplate == null && licenseContentRoot != null)
            {
                licenseTemplate = licenseContentRoot.Find("Template") as RectTransform;
            }

            licenseScrollbar = licensePanelTransform.GetComponent<Scrollbar>();
        }

        Transform licensePanel2Transform = uiRoot.Find("LicensePanel2");
        if (licensePanel2Transform == null)
        {
            licensePanel2Transform = uiRoot.Find("LicensPanel2");
        }

        if (licensePanel2Transform != null)
        {
            licensePanel2 = licensePanel2Transform.gameObject;
        }
    }

    private void InitializeState()
    {
        SetActive(titleGameStartButtonObject, true);
        SetActive(titleLicenseButtonObject, true);
        SetActive(gameStartPanel, false);
        SetActive(gameStartButtonObject, false);
        SetActive(licensePanel, false);
        SetActive(licensePanel2, false);
    }

    private void AddListeners()
    {
        if (titleGameStartButton != null)
        {
            titleGameStartButton.onClick.RemoveListener(OpenGameStartPanel);
            titleGameStartButton.onClick.AddListener(OpenGameStartPanel);
        }

        if (titleLicenseButton != null)
        {
            titleLicenseButton.onClick.RemoveListener(OpenLicensePanel);
            titleLicenseButton.onClick.AddListener(OpenLicensePanel);
        }

        if (licenseScrollbar != null)
        {
            licenseScrollbar.onValueChanged.RemoveListener(OnLicenseScrollbarValueChanged);
            licenseScrollbar.onValueChanged.AddListener(OnLicenseScrollbarValueChanged);
        }

        if (licenseNextButton != null)
        {
            licenseNextButton.onClick.RemoveListener(OpenLicensePanel2);
            licenseNextButton.onClick.AddListener(OpenLicensePanel2);
        }

        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(OnNameInputChanged);
            nameInput.onValueChanged.AddListener(OnNameInputChanged);
        }

        if (gameStartButton != null)
        {
            gameStartButton.onClick.RemoveListener(LoadInGameScene);
            gameStartButton.onClick.AddListener(LoadInGameScene);
        }
    }

    private void RemoveListeners()
    {
        if (titleGameStartButton != null)
        {
            titleGameStartButton.onClick.RemoveListener(OpenGameStartPanel);
        }

        if (titleLicenseButton != null)
        {
            titleLicenseButton.onClick.RemoveListener(OpenLicensePanel);
        }

        if (licenseScrollbar != null)
        {
            licenseScrollbar.onValueChanged.RemoveListener(OnLicenseScrollbarValueChanged);
        }

        if (licenseNextButton != null)
        {
            licenseNextButton.onClick.RemoveListener(OpenLicensePanel2);
        }

        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(OnNameInputChanged);
        }

        if (gameStartButton != null)
        {
            gameStartButton.onClick.RemoveListener(LoadInGameScene);
        }
    }

    private void OpenGameStartPanel()
    {
        SetActive(titleGameStartButtonObject, false);
        SetActive(licensePanel, false);
        SetActive(licensePanel2, false);
        SetActive(gameStartPanel, true);

        if (nameInput != null)
        {
            nameInput.text = string.Empty;
        }

        UpdateGameStartButtonState();
    }

    private void OpenLicensePanel()
    {
        BuildLicenseContent();
        SetActive(gameStartPanel, false);
        SetActive(gameStartButtonObject, false);
        SetActive(licensePanel2, false);
        SetActive(licensePanel, true);
        SetLicenseScrollOffset(0f, true);
    }

    private void OpenLicensePanel2()
    {
        SetActive(licensePanel, false);
        SetActive(licensePanel2, true);
    }

    private void CloseLicensePanel()
    {
        SetActive(licensePanel, false);
        SetActive(licensePanel2, false);
    }

    private void OnNameInputChanged(string value)
    {
        UpdateGameStartButtonState();
    }

    private void UpdateGameStartButtonState()
    {
        SetActive(gameStartButtonObject, HasNameInputText());
    }

    private bool HasNameInputText()
    {
        return nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text);
    }

    private void LoadInGameScene()
    {
        if (!HasNameInputText())
        {
            return;
        }

        GameSessionData.PlayerName = nameInput.text.Trim();
        SceneManager.LoadScene(InGameSceneName);
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private void BuildLicenseContent()
    {
        if (licenseContentBuilt || licensePanel == null || licenseTemplate == null)
        {
            return;
        }

        RectTransform panelRect = licensePanel.transform as RectTransform;
        if (licensePanel.GetComponent<RectMask2D>() == null)
        {
            licensePanel.AddComponent<RectMask2D>();
        }

        licenseContentRoot = CreateLicenseContentRoot(panelRect);
        licenseTemplate.SetParent(licenseContentRoot, false);
        licenseTemplate.anchoredPosition = new Vector2(licenseTemplate.anchoredPosition.x, LicenseFirstItemY);

        TextMeshProUGUI summaryText = FindText(licenseTemplate, "Summary");
        TextMeshProUGUI descText = FindText(licenseTemplate, "Desc");
        SetText(summaryText, "라이선스");
        SetText(descText, LicenseDescriptionText);
        SetText(FindText(licenseTemplate, "TXT"), LicenseDescriptionText);

        if (descText != null)
        {
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.overflowMode = TextOverflowModes.Overflow;
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.ForceMeshUpdate();

            RectTransform descRect = descText.rectTransform;
            float descHeight = Mathf.Max(descRect.sizeDelta.y, descText.preferredHeight + 40f);
            descRect.sizeDelta = new Vector2(descRect.sizeDelta.x, descHeight);
            licenseTemplate.sizeDelta = new Vector2(licenseTemplate.sizeDelta.x, Mathf.Max(licenseTemplate.sizeDelta.y, descHeight + 120f));

            float panelHalfHeight = panelRect == null ? 0f : panelRect.rect.height * 0.5f;
            float descBottomY = licenseTemplate.anchoredPosition.y + descRect.anchoredPosition.y - descHeight * (1f - descRect.pivot.y);
            licenseMaxScrollOffset = Mathf.Max(0f, -panelHalfHeight + 40f - descBottomY);
        }
        else
        {
            licenseMaxScrollOffset = 0f;
        }

        licenseContentBuilt = true;
    }

    private RectTransform CreateLicenseContentRoot(RectTransform panelRect)
    {
        Transform existing = panelRect == null ? null : panelRect.Find("LicenseContent");
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject contentObject = new GameObject("LicenseContent", typeof(RectTransform));
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.SetParent(panelRect, false);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = panelRect == null ? Vector2.zero : panelRect.sizeDelta;
        contentRect.anchoredPosition = Vector2.zero;
        return contentRect;
    }

    private TextMeshProUGUI FindText(Transform parent, string childName)
    {
        Transform child = parent == null ? null : parent.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void UpdateLicenseScrollInput()
    {
        if (licensePanel == null || !licensePanel.activeInHierarchy || licenseMaxScrollOffset <= 0f)
        {
            return;
        }

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(wheel, 0f))
        {
            return;
        }

        SetLicenseScrollOffset(licenseScrollOffset - wheel * LicenseWheelStep, true);
    }

    private void UpdateLicenseCloseInput()
    {
        if (!IsLicensePanelOpen())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLicensePanel();
        }
    }

    private bool IsLicensePanelOpen()
    {
        return IsActiveInHierarchy(licensePanel) || IsActiveInHierarchy(licensePanel2);
    }

    private bool IsActiveInHierarchy(GameObject target)
    {
        return target != null && target.activeInHierarchy;
    }

    private void OnLicenseScrollbarValueChanged(float value)
    {
        if (!licenseContentBuilt || licenseScrollbar == null)
        {
            return;
        }

        SetLicenseScrollOffset((1f - value) * licenseMaxScrollOffset, false);
    }

    private void SetLicenseScrollOffset(float offset, bool updateScrollbar)
    {
        licenseScrollOffset = Mathf.Clamp(offset, 0f, licenseMaxScrollOffset);
        if (licenseContentRoot != null)
        {
            licenseContentRoot.anchoredPosition = new Vector2(0f, licenseScrollOffset);
        }

        if (updateScrollbar && licenseScrollbar != null)
        {
            licenseScrollbar.SetValueWithoutNotify(licenseMaxScrollOffset <= 0f ? 1f : 1f - licenseScrollOffset / licenseMaxScrollOffset);
        }
    }

}
