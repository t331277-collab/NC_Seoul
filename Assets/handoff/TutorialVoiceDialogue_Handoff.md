# 튜토리얼 보좌관 음성 대사 시스템 Handoff

## 목적
인게임 진입 시 `NC_Seoul_Proj/Assets/TXT/보좌관대사.txt`의 흐름에 맞춰 각 대사에 대응되는 mp3 음성을 순차 재생한다. 대사 재생 중에는 플레이어의 일반 입력/클릭을 막고, `Enter` 입력만 허용해 현재 대사를 스킵하고 다음 시퀀스로 넘어가게 한다.

`XXX님` 부분은 `MainScene`의 `UI/GameStartPanel/NameInput`에 입력된 이름을 사용한다. 플레이어 이름 음성은 NC Varco Voice API로 mp3를 생성/캐싱한 뒤, `XXX님`이 등장하는 모든 대사 구간에서 기존 녹음 mp3 사이에 끼워 재생한다.

## 현재 확인된 근거
- 대사 원문 파일은 존재한다: `NC_Seoul_Proj/Assets/TXT/보좌관대사.txt`.
- Varco API 가이드 파일은 존재하며 `Text To Speech (lite)` 사용법, 화자 목록 API, 음성 합성 API, 요청/응답 포맷이 기록되어 있다: `NC_Seoul_Proj/Assets/TXT/VarcoVoiceAPIGuide.txt`.
- 가이드 기준 화자 목록 API는 `GET https://openapi.ai.nc.com/tts/lite/v1/api/voices/varco`이다.
- 가이드 기준 음성 합성 API는 `POST https://openapi.ai.nc.com/tts/lite/v1/api/synthesize`이다.
- 음성 합성 응답은 JSON의 `audio` 필드에 base64 encoded 오디오 데이터를 담는다.
- 합성 API는 `media_type`으로 `wav`, `mp3`, `flac`을 지원한다. 이 기능은 요구사항에 맞춰 `mp3`를 사용한다.
- IDE 탭에는 `VarcoVoiceAPIGuide.txxt`가 열려 있었지만, 실제 파일은 확인되지 않았다.
- 현재 이름 입력 흐름은 `NC_Seoul_Proj/Assets/Scripts/Core/MainMenuUI.cs`에 있다.
- `MainMenuUI.cs`는 `NameInput`의 빈 값 여부만 검사하고 `SceneManager.LoadScene("InGameScene")`을 호출한다.
- 현재 코드에는 플레이어 이름을 다음 씬으로 전달/저장하는 로직이 없다.
- 현재 스크립트 검색 기준으로 `AudioSource`, `mp3`, `Dialogue`, `Tutorial` 관련 기존 구현은 확인되지 않았다.
- 실제 씬 UI에는 이전 작업 기준 `SciencePanel`이 아니라 `SciecnePanel` 오타 이름이 존재한다. 튜토리얼 데이터에서도 실제 경로는 `UI/SciecnePanel`로 기록해야 한다.

## 구현 전 선행 결정
- API Key는 코드나 Git 추적 파일에 직접 넣지 않는다. Unity 개발 중에는 `ProjectSettings` 외부 설정, 환경 변수, 또는 Git 제외 JSON을 사용한다.
- 빌드 런타임에서 `Assets/` 폴더에 새 mp3를 저장할 수 없으므로, 플레이어 이름 mp3 캐시는 `Application.persistentDataPath/VoiceCache/`를 기본 저장소로 설계한다.
- 에디터 개발용으로만 `Assets/Audio/Voice/Generated/`에 저장하는 선택지를 둘 수 있지만, 빌드에서도 동작하려면 `persistentDataPath` 로딩 경로가 반드시 필요하다.
- Varco 합성 요청은 이름 음성 전용으로만 사용한다. 전체 대사 문장을 API로 합성하지 않는다.
- 이름 음성은 `media_type: "mp3"`로 요청해 mp3를 바로 저장한다. wav를 받은 뒤 변환하는 흐름은 기본 설계에서 제외한다.
- 화자 `speaker_uuid`, `speed`, `pitch`, `n_fm_steps`, `seed`는 코드 상수로 박지 말고 설정 데이터로 분리한다.

## 권장 파일 구조
```text
NC_Seoul_Proj/Assets/
  Audio/
    Voice/
      Tutorial/
        intro_001_hello.mp3
        intro_001_after_name.mp3
        intro_002_after_name.mp3
        money_001.mp3
        science_001.mp3
        convenience_001_before_name.mp3
        convenience_001_after_name.mp3
      Generated/              # 에디터 전용 생성 파일이 필요할 때만 사용
  Data/
    TutorialDialogueFlow.json
    TutorialVoiceClipCatalog.csv
  Scripts/
    Core/
      GameSessionData.cs
      PlayerNameVoiceManager.cs
      VarcoVoiceClient.cs
      TutorialDialogueRunner.cs
      TutorialInputLockManager.cs
      TutorialFlagStore.cs
```

## 핵심 설계
튜토리얼은 하드코딩된 C# 배열이 아니라 데이터 파일로 제어한다. 다음 작업자가 mp3를 교체하거나 대사 순서를 바꿔도 코드 수정 없이 `TutorialDialogueFlow.json`과 mp3 파일만 수정하게 만드는 것이 목표다.

### 1. 플레이어 이름 저장
`MainMenuUI.LoadInGameScene()`에서 씬 이동 직전에 입력 이름을 저장한다.

권장 방식:
- `GameSessionData.PlayerName = nameInput.text.Trim();`
- 필요하면 `PlayerPrefs.SetString("PlayerName", value);`도 함께 사용한다.

`GameSessionData`는 간단한 static 클래스로 시작한다. 저장 데이터가 커질 때만 `DontDestroyOnLoad` 매니저로 확장한다.

### 2. 이름 mp3 생성/캐싱
`PlayerNameVoiceManager`가 담당한다.

흐름:
1. `InGameScene` 시작 시 `GameSessionData.PlayerName`을 읽는다.
2. 이름 음성 생성용 문구를 만든다. 예: `홍길동님`.
3. 이름과 Varco 음성 옵션을 기준으로 해시 파일명을 만든다. 예: `player_name_9f3a2c_voiceA.mp3`.
4. `Application.persistentDataPath/VoiceCache/`에 같은 파일이 있으면 API를 다시 호출하지 않는다.
5. 없으면 `VarcoVoiceClient`로 mp3를 요청해 저장한다.
6. 저장된 mp3를 `UnityWebRequestMultimedia.GetAudioClip("file://...")`로 `AudioClip`으로 로드한다.

주의:
- `XXX님`이 여러 번 나오더라도 API 호출은 한 번만 한다.
- API 실패 시에는 대사가 멈추지 않도록 무음 클립 fallback을 둔다.
- 같은 플레이어 이름이라도 화자, 속도, 피치, seed가 달라지면 다른 캐시 파일로 취급한다.

### 3. VarcoVoiceClient 구현 세부
`VarcoVoiceClient`는 `VarcoVoiceAPIGuide.txt`에 있는 TTS Lite API를 감싼다.

화자 목록 요청:

```text
GET https://openapi.ai.nc.com/tts/lite/v1/api/voices/varco
Header: OPENAPI_KEY 또는 openapi_key
```

가이드의 샘플 코드는 `OPENAPI_KEY` 헤더명을 사용하고, 레퍼런스 표는 `openapi_key`로 표기되어 있다. 구현자는 먼저 샘플 코드와 같은 `OPENAPI_KEY`를 사용하고, 서버가 인증 실패를 반환하면 소문자 헤더명도 확인한다.

음성 합성 요청:

```json
{
  "text": "홍길동님",
  "language": "korean",
  "voice": "d7da3489-245d-5691-8f55-7f5552b0431e",
  "properties": {
    "speed": 1,
    "pitch": 1
  },
  "n_fm_steps": 8,
  "seed": 1945,
  "return_metadata": false,
  "media_type": "mp3"
}
```

응답 처리:
1. HTTP 200인지 확인한다.
2. JSON에서 `audio` 필드를 읽는다.
3. `System.Convert.FromBase64String(audio)`로 mp3 바이트를 만든다.
4. `Application.persistentDataPath/VoiceCache/{cacheKey}.mp3`에 저장한다.
5. 필요하면 응답의 `media_type`이 `mp3`인지 확인한다.

요청 제한:
- `text`는 UTF-8 기준 최대 1,200바이트다. 이 기능에서는 이름과 `님`만 요청하므로 제한에 걸릴 가능성은 낮다.
- `language`는 `korean`을 사용한다.
- `voice`는 speaker_uuid이며 필수다.
- `speed`와 `pitch`는 기본값 1이고, 가이드 권장 범위는 0.8 ~ 1.2다.
- `n_fm_steps`는 8 ~ 20 범위다. 낮을수록 빠르고, 높을수록 품질 중심이다.
- `seed`는 같은 입력에서 같은 음성을 재생산하기 위해 고정값을 쓴다. 이름 음성 캐시 안정성을 위해 `-1` 랜덤보다는 명시 seed를 권장한다.

권장 설정 파일 예시:

```json
{
  "apiKeySource": "local_untracked_file",
  "voice": "d7da3489-245d-5691-8f55-7f5552b0431e",
  "language": "korean",
  "speed": 1.0,
  "pitch": 1.0,
  "n_fm_steps": 8,
  "seed": 1945,
  "media_type": "mp3"
}
```

API Key 저장:
- `Assets/` 아래 Git 추적 파일에 API Key를 저장하지 않는다.
- 개발 중에는 예를 들어 `NC_Seoul_Proj/LocalSecrets/varco_voice_api_key.txt` 같은 Git 제외 경로를 사용한다.
- 구현자가 이 경로를 쓴다면 `.gitignore` 또는 기존 제외 규칙을 반드시 확인한다.

### 4. 대사 데이터 구조
`TutorialDialogueFlow.json` 예시:

```json
{
  "startNodeId": "intro_001",
  "nodes": [
    {
      "id": "intro_001",
      "text": "안녕하세요, {playerName}님! 만나서 반가워요!",
      "lockInput": true,
      "segments": [
        { "type": "clip", "key": "intro_001_hello" },
        { "type": "playerName" },
        { "type": "clip", "key": "intro_001_after_name" }
      ],
      "onCompleteSetFlag": "intro_001_done",
      "next": "intro_002"
    },
    {
      "id": "intro_002",
      "text": "{playerName}님은 지금부터 서울을 발전시켜, 세계적인 일류 도시로 만들어 나가게 될 거예요!",
      "lockInput": true,
      "segments": [
        { "type": "playerName" },
        { "type": "clip", "key": "intro_002_after_name" }
      ],
      "onCompleteSetFlag": "intro_002_done",
      "next": "intro_003"
    },
    {
      "id": "wait_money_panel",
      "type": "waitForAction",
      "instruction": "UI/MoneyPanel 클릭 대기",
      "allowedTargetPath": "UI/MoneyPanel",
      "onActionSetFlag": "money_panel_clicked",
      "next": "money_001"
    }
  ]
}
```

`TutorialVoiceClipCatalog.csv` 예시:

```csv
key,path
intro_001_hello,Audio/Voice/Tutorial/intro_001_hello
intro_001_after_name,Audio/Voice/Tutorial/intro_001_after_name
intro_002_after_name,Audio/Voice/Tutorial/intro_002_after_name
money_001,Audio/Voice/Tutorial/money_001
science_001,Audio/Voice/Tutorial/science_001
```

Unity에서 `Resources.Load<AudioClip>()`를 쓸 경우 경로는 `Assets/Resources/` 기준이어야 한다. 현재 프로젝트 구조에 `Resources` 사용 방침이 없다면, Inspector에 `AudioClip` 목록을 넣는 ScriptableObject 방식도 가능하다. 단, 목표는 “mp3 수정 자유도”이므로 `key -> clip/path` 매핑 파일을 유지하는 방식이 좋다.

### 5. mp3 수정/교체 정책
mp3 수정이 들어왔을 때 코드를 수정하지 않게 하려면 아래 규칙을 따른다.

- 같은 대사 구간을 새 mp3로 교체할 때: 기존 파일명 그대로 덮어쓰기.
- 파일명을 바꿔야 할 때: `TutorialVoiceClipCatalog.csv`의 `path`만 수정.
- 대사를 더 잘게 쪼개거나 합칠 때: `TutorialDialogueFlow.json`의 `segments`만 수정.
- `XXX님` 위치가 바뀔 때: 해당 노드의 `segments`에서 `{ "type": "playerName" }` 위치만 이동.
- 대사 순서가 바뀔 때: 각 노드의 `next`만 수정.
- 특정 mp3가 끝난 뒤 직접 플래그를 세워야 할 때: 노드 또는 세그먼트에 `onCompleteSetFlag`를 추가한다.

세그먼트 단위 플래그 예시:

```json
{
  "type": "clip",
  "key": "money_001",
  "onCompleteSetFlag": "money_voice_done"
}
```

### 6. 재생/스킵 규칙
`TutorialDialogueRunner`가 하나의 노드를 실행한다.

일반 대사 노드:
1. `lockInput`이 true면 `TutorialInputLockManager`로 일반 클릭/키 입력을 막는다.
2. `segments`를 순서대로 재생한다.
3. `clip` 타입은 카탈로그에서 mp3를 찾아 재생한다.
4. `playerName` 타입은 `PlayerNameVoiceManager`가 캐싱한 이름 음성 클립을 재생한다.
5. 모든 세그먼트가 끝나면 `onCompleteSetFlag`를 세우고 `next`로 이동한다.

`Enter` 입력:
- 대사 재생 중 `Enter`를 누르면 현재 노드의 남은 mp3를 모두 중지한다.
- 기본 동작은 현재 노드를 완료 처리하고 다음 노드로 이동하는 것이다.
- `waitForAction` 노드에서는 `Enter`로 진행하지 않는다. 이 단계는 지정된 플레이어 행동만 허용한다.

### 7. 플레이어 행동 대기
`보좌관대사.txt`에는 아래 행동 게이트가 있다.

- 튜토리얼 수락
- `UI/MoneyPanel` 클릭
- `UI/SciecnePanel` 클릭
- `UI/ConveniencePanel` 클릭
- `UI/PeoplePanel` 클릭
- `UI/LovePanel` 클릭

대사 중에는 모든 행동을 막고, 행동 게이트에 도달했을 때만 해당 UI 클릭을 허용한다. 이때 다른 UI/지역 클릭은 막는 것이 튜토리얼 흐름상 안전하다.

권장 방식:
- `TutorialInputLockManager`가 전역 입력 차단 패널을 켠다.
- 대사 중에는 차단 패널이 전체 화면을 덮고 `Enter`만 `TutorialDialogueRunner`가 받는다.
- 행동 게이트에서는 차단 패널을 유지하되, 허용 대상 버튼만 클릭 가능하게 하거나, 클릭 이벤트를 가로채서 `allowedTargetPath`와 일치할 때만 통과시킨다.
- 기존 `ResourceDetailPanelManager`의 ESC 닫기 흐름과 충돌하지 않도록, 튜토리얼 행동 게이트에서 필요한 패널 닫기 입력도 명시적으로 허용한다.

### 8. 기존 코드와 연결할 지점
`MainMenuUI.cs`
- `LoadInGameScene()`에서 씬 로드 전에 이름 저장 로직 추가 필요.
- 현재는 이름을 보관하지 않고 바로 `SceneManager.LoadScene(InGameSceneName)`만 호출한다.

`InGameScene`
- `UI` 또는 별도 `TutorialManager` 오브젝트에 `TutorialDialogueRunner`, `PlayerNameVoiceManager`, `TutorialInputLockManager`를 붙인다.
- 인게임 시작 시 `TutorialDialogueRunner.StartTutorial()` 호출.

`ResourceDetailPanelManager`
- 튜토리얼 행동 게이트에서 `UI/MoneyPanel` 등 클릭을 요구하므로 기존 리소스 패널 클릭 동작과 연결된다.
- 실제 경로는 `UI/SciecnePanel` 오타를 기준으로 처리해야 한다.

## 구현 순서 제안
1. `GameSessionData`를 만들고 `MainMenuUI.LoadInGameScene()`에서 플레이어 이름을 저장한다.
2. Varco API Key를 Git 제외 경로에 둔다.
3. Varco 음성 설정 파일을 만든다.
4. `VarcoVoiceClient`를 만들고 `media_type: "mp3"` 합성 요청/저장을 검증한다.
5. `PlayerNameVoiceManager`를 만들고 이름 mp3 캐싱과 파일 로딩을 검증한다.
6. `TutorialDialogueFlow.json`, `TutorialVoiceClipCatalog.csv` 초안을 만든다.
7. 기존 `보좌관대사.txt`를 기준으로 문단별 노드와 행동 게이트를 JSON에 옮긴다.
8. `TutorialDialogueRunner`로 세그먼트 순차 재생을 구현한다.
9. `Enter` 스킵과 `waitForAction` 게이트를 구현한다.
10. 입력 잠금 패널 또는 입력 차단 매니저를 연결한다.
11. 누락 mp3/key/path/API 설정 검증용 에디터 검사 함수를 만든다.

## 검증 기준
- `MainScene`에서 이름 입력 후 `InGameScene`에 들어가면 같은 이름이 유지된다.
- 이름 mp3는 한 번만 생성되고, 이후 같은 이름/음성 옵션이면 캐시 파일을 재사용한다.
- Varco 합성 요청은 `POST /tts/lite/v1/api/synthesize`로 보내며 `media_type`은 `mp3`다.
- Varco 응답의 base64 `audio`를 디코딩해 `.mp3` 파일로 저장한다.
- API Key가 없거나 인증 실패하면 튜토리얼 전체가 멈추지 않고 무음 이름 클립 fallback으로 진행한다.
- `안녕하세요, XXX님! 만나서 반가워요!`는 `고정 mp3 -> 이름 mp3 -> 고정 mp3` 순서로 재생된다.
- 대사 재생 중 일반 UI 클릭/지역 클릭은 무시된다.
- 대사 재생 중 `Enter`를 누르면 현재 대사를 스킵하고 다음 노드로 넘어간다.
- 행동 게이트에서는 지정된 UI만 클릭 가능하다.
- `UI/SciecnePanel` 클릭 게이트는 실제 씬 오타 이름으로 동작한다.
- mp3 파일을 같은 이름으로 교체하면 코드 수정 없이 새 음성이 재생된다.
- mp3 파일명을 바꿔도 카탈로그 파일만 수정하면 동작한다.

## 주의할 점
- 런타임 빌드에서 `Assets/`에 파일을 쓰는 설계는 피한다.
- API Key를 커밋하지 않는다.
- `XXX님`을 포함한 문장을 통째로 API로 만들지 않는다. 사용자 요구사항은 고정 녹음 mp3와 이름 mp3를 분리 재생하는 구조다.
- Varco 가이드는 mp3 반환을 지원하므로, 이름 음성은 mp3로 직접 요청한다.
- 화자 목록 API는 개발/설정 단계에서만 사용하고, 매번 게임 시작 때 호출하지 않는다.
- 대사 파일 원문은 참고용 원본으로 유지하고, 실제 실행 흐름은 JSON/CSV 데이터로 관리한다.
- Play Mode 검증은 프로젝트 규칙상 사용자가 수행한다.
