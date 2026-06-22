# 튜토리얼 보좌관 음성 대사 시스템 Handoff

## 목적
인게임 진입 시 `NC_Seoul_Proj/Assets/TXT/보좌관대사.txt`의 대사 흐름에 맞춰 보좌관 음성을 재생한다. 모든 대사 음성은 NC Varco Voice API로 생성한다. 사용자가 직접 `Assets/Audio` 같은 리소스 폴더에 대사 mp3를 저장해 두는 방식은 사용하지 않는다.

대사 재생 중에는 플레이어의 일반 입력/클릭을 막고, `Enter` 입력만 허용해 현재 대사를 스킵하고 다음 시퀀스로 넘어가게 한다.

## 현재 확인된 근거
- 대사 원문 파일은 존재한다: `NC_Seoul_Proj/Assets/TXT/보좌관대사.txt`.
- 현재 대사 원문에는 `{playerName}`과 `XXX` 표기가 함께 존재한다.
- 구현 기준 플레이스홀더는 `{playerName}` 하나로 통일한다.
- 구현 전 `보좌관대사.txt`에 남아 있는 `XXX`는 `{playerName}`으로 정규화해야 한다.
- Varco API 가이드 파일은 존재하며 `Text To Speech (lite)` 사용법, 화자 목록 API, 음성 합성 API, 요청/응답 포맷이 기록되어 있다: `NC_Seoul_Proj/Assets/TXT/VarcoVoiceAPIGuide.txt`.
- 가이드 기준 화자 목록 API는 `GET https://openapi.ai.nc.com/tts/lite/v1/api/voices/varco`이다.
- 가이드 기준 음성 합성 API는 `POST https://openapi.ai.nc.com/tts/lite/v1/api/synthesize`이다.
- 음성 합성 응답은 JSON의 `audio` 필드에 base64 encoded 오디오 데이터를 담는다.
- 합성 API는 `media_type`으로 `wav`, `mp3`, `flac`을 지원한다. 이 기능은 `mp3`를 사용한다.
- 현재 `MainMenuUI.cs`는 `NameInput`의 빈 값 여부만 검사하고 `SceneManager.LoadScene("InGameScene")`을 호출한다.
- 현재 코드에는 플레이어 이름을 다음 씬으로 전달/저장하는 로직이 없다.

## 핵심 요구사항
- 모든 대사 문장은 API로 음성 생성한다.
- 사용자가 직접 저장하는 대사 mp3 리소스는 없다.
- `{playerName}`만 플레이어 이름 치환 대상으로 사용한다.
- `{playerName}` 치환이 끝난 전체 대사 문장을 API에 넘긴다.
- 대사 일부만 따로 합성하거나 여러 음성 파일을 조합하지 않는다.
- API 키는 구현 단계에서 사용자가 입력할 수 있는 인스펙터/에디터 창을 제공한다.
- 지정 화자는 아래 `나디스(중립)` 값을 사용한다.

## 지정 Varco Voice
구현 단계에서 아래 목소리 값을 기본값으로 사용한다.

```text
index: 966
speaker_uuid: adfc2330-3a22-501b-897d-313d7472f2d8
speaker_name: 나디스(중립)
saas_name: 최아연
description: 여성, 청년, 고음, 맑음, 차분한
```

`voice` 요청 필드에는 `adfc2330-3a22-501b-897d-313d7472f2d8`을 넣고, `language` 요청 필드에는 `korean`을 넣는다.

## Varco API 필수 요청값
구현 단계에서 Varco 음성 합성 요청에는 아래 값을 반드시 사용한다.

```text
voice = adfc2330-3a22-501b-897d-313d7472f2d8
language = korean
```

`voice`는 나디스(중립) 화자의 `speaker_uuid`이고, `language`는 한국어 대사 합성을 위해 `korean`으로 고정한다.

## API 키 입력 방식
구현자는 사용자가 API 키를 넣을 수 있는 Unity 인스펙터 또는 에디터 창을 만들어야 한다.

권장 방식:
- `VarcoVoiceSettings` 컴포넌트 또는 `VarcoVoiceSettingsWindow` 에디터 창을 만든다.
- 창에는 `OPENAPI_KEY` 입력 필드와 저장 버튼을 둔다.
- 사용자가 직접 API 키를 입력하도록 안내한다.
- API 키는 Git 추적 대상 에셋에 저장하지 않는다.
- 저장 위치는 `EditorPrefs`, `Application.persistentDataPath`, 또는 Git 제외된 `NC_Seoul_Proj/LocalSecrets/varco_voice_api_key.txt` 중 하나를 사용한다.
- 입력된 키가 없으면 튜토리얼 음성 생성 시작 전에 사용자에게 API 키 입력이 필요하다는 로그/경고를 표시한다.

주의:
- API 키를 `Assets/` 아래 `.asset`, `.json`, `.txt`, 씬 파일에 직렬화하지 않는다.
- 인스펙터에 보이는 입력 필드는 개발 편의를 위한 UI이고, 저장은 로컬 비추적 경로로 해야 한다.

## 권장 파일 구조
```text
NC_Seoul_Proj/Assets/
  Data/
    TutorialDialogueFlow.json
    TutorialVoiceSettings.json
  Scripts/
    Core/
      GameSessionData.cs
      VarcoVoiceClient.cs
      TutorialVoiceSynthesisManager.cs
      TutorialDialogueRunner.cs
      TutorialInputLockManager.cs
      TutorialFlagStore.cs
  Editor/
    VarcoVoiceSettingsWindow.cs
```

사전 제작 대사 음성 리소스 폴더는 만들지 않는다.

## 플레이어 이름 저장
`MainMenuUI.LoadInGameScene()`에서 씬 이동 직전에 입력 이름을 저장한다.

권장 방식:
- `GameSessionData.PlayerName = nameInput.text.Trim();`
- 필요하면 `PlayerPrefs.SetString("PlayerName", value);`도 함께 사용한다.

`GameSessionData`는 간단한 static 클래스로 시작한다. 저장 데이터가 커질 때만 `DontDestroyOnLoad` 매니저로 확장한다.

## 전체 대사 API 합성 흐름
`TutorialVoiceSynthesisManager`가 담당한다.

흐름:
1. `InGameScene` 시작 시 `GameSessionData.PlayerName`을 읽는다.
2. `TutorialDialogueFlow.json`의 현재 노드 `text`를 가져온다.
3. `text` 안의 `{playerName}`만 실제 플레이어 이름으로 치환한다.
4. 치환 완료된 전체 문장을 `VarcoVoiceClient`에 넘긴다.
5. `VarcoVoiceClient`는 `POST /tts/lite/v1/api/synthesize`를 호출한다.
6. 응답의 base64 `audio`를 디코딩한다.
7. 디코딩된 mp3 데이터를 `AudioClip`으로 로드하거나, 필요하면 `Application.persistentDataPath/VoiceCache/Tutorial/`에 런타임 캐시한다.
8. 재생이 끝나면 노드의 `onCompleteSetFlag`를 세우고 `next`로 이동한다.

예시:
```text
원문: 안녕하세요, {playerName}님! 만나서 반가워요!
플레이어 이름: 홍길동
API 요청 text: 안녕하세요, 홍길동님! 만나서 반가워요!
```

이때 `홍길동님`만 따로 API에 보내지 않는다. 반드시 최종 문장 전체를 API에 보낸다.

## VarcoVoiceClient 구현 세부
화자 목록 요청:

```text
GET https://openapi.ai.nc.com/tts/lite/v1/api/voices/varco
Header: OPENAPI_KEY 또는 openapi_key
```

가이드의 샘플 코드는 `OPENAPI_KEY` 헤더명을 사용하고, 레퍼런스 표는 `openapi_key`로 표기되어 있다. 구현자는 먼저 샘플 코드와 같은 `OPENAPI_KEY`를 사용하고, 서버가 인증 실패를 반환하면 소문자 헤더명도 확인한다.

음성 합성 요청:

```json
{
  "text": "안녕하세요, 홍길동님! 만나서 반가워요!",
  "language": "korean",
  "voice": "adfc2330-3a22-501b-897d-313d7472f2d8",
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
4. mp3 바이트를 재생 가능한 `AudioClip`으로 로드한다.
5. 필요하면 응답의 `media_type`이 `mp3`인지 확인한다.

요청 제한:
- `text`는 UTF-8 기준 최대 1,200바이트다.
- 문단이 1,200바이트를 넘으면 `TutorialDialogueFlow.json`에서 노드를 나눈다.
- `language`는 `korean`을 사용한다.
- `voice`는 `adfc2330-3a22-501b-897d-313d7472f2d8`을 사용한다.
- `speed`와 `pitch`는 기본값 1이고, 가이드 권장 범위는 0.8 ~ 1.2다.
- `n_fm_steps`는 8 ~ 20 범위다.
- `seed`는 재현성을 위해 명시값을 권장한다.

## TutorialVoiceSettings.json 예시
```json
{
  "voiceIndex": 966,
  "voice": "adfc2330-3a22-501b-897d-313d7472f2d8",
  "speakerName": "나디스(중립)",
  "saasName": "최아연",
  "description": "여성, 청년, 고음, 맑음, 차분한",
  "language": "korean",
  "speed": 1.0,
  "pitch": 1.0,
  "n_fm_steps": 8,
  "seed": 1945,
  "media_type": "mp3"
}
```

API 키는 이 파일에 넣지 않는다.

## TutorialDialogueFlow.json 예시
```json
{
  "startNodeId": "intro_001",
  "nodes": [
    {
      "id": "intro_001",
      "text": "안녕하세요, {playerName}님! 만나서 반가워요!",
      "lockInput": true,
      "onCompleteSetFlag": "intro_001_done",
      "next": "intro_002"
    },
    {
      "id": "intro_002",
      "text": "{playerName}님은 지금부터 서울을 발전시켜, 세계적인 일류 도시로 만들어 나가게 될 거예요!",
      "lockInput": true,
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

## 대사 수정 정책
- 대사 문장을 수정할 때: `TutorialDialogueFlow.json`의 해당 노드 `text`만 수정한다.
- 플레이어 이름 위치가 바뀔 때: 해당 노드 `text` 안에서 `{playerName}` 위치만 이동한다.
- 대사 순서가 바뀔 때: 각 노드의 `next`만 수정한다.
- 특정 대사 재생이 끝난 뒤 직접 플래그를 세워야 할 때: 노드에 `onCompleteSetFlag`를 추가한다.
- 화자, 속도, 피치, seed가 바뀔 때: `TutorialVoiceSettings.json`만 수정한다.
- 사용자가 직접 저장하는 대사 mp3 교체 절차는 없다.

## 재생/스킵 규칙
`TutorialDialogueRunner`가 하나의 노드를 실행한다.

일반 대사 노드:
1. `lockInput`이 true면 `TutorialInputLockManager`로 일반 클릭/키 입력을 막는다.
2. 현재 노드의 `text`에서 `{playerName}`을 플레이어 이름으로 치환해 최종 문장을 만든다.
3. `TutorialVoiceSynthesisManager`가 최종 문장 전체를 Varco API로 합성한다.
4. 합성된 음성 재생이 끝나면 `onCompleteSetFlag`를 세우고 `next`로 이동한다.

`Enter` 입력:
- 대사 재생 중 `Enter`를 누르면 현재 재생 중인 음성을 중지한다.
- 기본 동작은 현재 노드를 완료 처리하고 다음 노드로 이동하는 것이다.
- `waitForAction` 노드에서는 `Enter`로 진행하지 않는다. 이 단계는 지정된 플레이어 행동만 허용한다.

## 플레이어 행동 대기
`보좌관대사.txt`에는 아래 행동 게이트가 있다.

- 튜토리얼 수락
- `UI/MoneyPanel` 클릭
- `UI/SciecnePanel` 클릭
- `UI/ConveniencePanel` 클릭
- `UI/PeoplePanel` 클릭
- `UI/LovePanel` 클릭

대사 중에는 모든 행동을 막고, 행동 게이트에 도달했을 때만 해당 UI 클릭을 허용한다. 이때 다른 UI/지역 클릭은 막는 것이 튜토리얼 흐름상 안전하다.

## 기존 코드와 연결할 지점
`MainMenuUI.cs`
- `LoadInGameScene()`에서 씬 로드 전에 이름 저장 로직 추가 필요.
- 현재는 이름을 보관하지 않고 바로 `SceneManager.LoadScene(InGameSceneName)`만 호출한다.

`InGameScene`
- `UI` 또는 별도 `TutorialManager` 오브젝트에 `TutorialDialogueRunner`, `TutorialVoiceSynthesisManager`, `TutorialInputLockManager`를 붙인다.
- 인게임 시작 시 `TutorialDialogueRunner.StartTutorial()` 호출.

`ResourceDetailPanelManager`
- 튜토리얼 행동 게이트에서 `UI/MoneyPanel` 등 클릭을 요구하므로 기존 리소스 패널 클릭 동작과 연결된다.
- 실제 경로는 `UI/SciecnePanel` 오타를 기준으로 처리해야 한다.

## 구현 순서 제안
1. `보좌관대사.txt`의 `XXX` 표기를 `{playerName}`으로 통일한다.
2. `GameSessionData`를 만들고 `MainMenuUI.LoadInGameScene()`에서 플레이어 이름을 저장한다.
3. API 키 입력용 인스펙터/에디터 창을 만든다.
4. API 키 저장 위치를 Git 제외 로컬 경로로 정한다.
5. `TutorialVoiceSettings.json`에 `나디스(중립)` voice 값을 넣는다.
6. `VarcoVoiceClient`를 만들고 `media_type: "mp3"` 합성 요청을 검증한다.
7. `TutorialVoiceSynthesisManager`를 만들고 전체 대사 문장 합성/재생을 검증한다.
8. `TutorialDialogueFlow.json` 초안을 만든다.
9. 기존 `보좌관대사.txt`를 기준으로 문단별 노드와 행동 게이트를 JSON에 옮긴다.
10. `Enter` 스킵과 `waitForAction` 게이트를 구현한다.
11. 입력 잠금 패널 또는 입력 차단 매니저를 연결한다.
12. API 키/음성 설정/대사 길이 검증용 에디터 검사 함수를 만든다.

## 검증 기준
- 구현 단계에서 사용자가 API 키를 입력할 수 있는 인스펙터/에디터 창이 있다.
- API 키를 입력하지 않으면 음성 합성을 시작하지 않고 명확한 안내를 남긴다.
- `voice` 요청값은 `adfc2330-3a22-501b-897d-313d7472f2d8`이다.
- `MainScene`에서 이름 입력 후 `InGameScene`에 들어가면 같은 이름이 유지된다.
- `안녕하세요, {playerName}님! 만나서 반가워요!`는 이름 치환 후 전체 문장 하나로 API 요청된다.
- Varco 합성 요청은 `POST /tts/lite/v1/api/synthesize`로 보내며 `media_type`은 `mp3`다.
- Varco 응답의 base64 `audio`를 디코딩해 재생한다.
- 대사 재생 중 일반 UI 클릭/지역 클릭은 무시된다.
- 대사 재생 중 `Enter`를 누르면 현재 대사를 스킵하고 다음 노드로 넘어간다.
- 행동 게이트에서는 지정된 UI만 클릭 가능하다.
- `UI/SciecnePanel` 클릭 게이트는 실제 씬 오타 이름으로 동작한다.
- 사용자가 직접 저장한 대사 mp3 리소스 없이도 튜토리얼 음성이 재생된다.

## 주의할 점
- API Key를 커밋하지 않는다.
- 사전 제작 대사 음성 파일을 프로젝트 리소스로 저장하는 설계는 사용하지 않는다.
- 이름만 따로 합성하지 않는다. 모든 대사는 최종 문장 전체를 API로 합성한다.
- `{playerName}` 외 다른 이름 플레이스홀더를 늘리지 않는다.
- 화자 목록 API는 개발/설정 단계에서만 사용하고, 매번 게임 시작 때 호출하지 않는다.
- 대사 파일 원문은 참고용 원본으로 유지하고, 실제 실행 흐름은 JSON 데이터로 관리한다.
- Play Mode 검증은 프로젝트 규칙상 사용자가 수행한다.
