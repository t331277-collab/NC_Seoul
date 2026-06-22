# Tutorial Assistant Voice Dialogue System Handoff

## Purpose
When entering the in-game scene, play assistant voice lines according to the dialogue flow in `NC_Seoul_Proj/Assets/TXT/보좌관대사.txt`. Every dialogue voice line must be generated through the NC Varco Voice API. Do not use a workflow where the user manually stores pre-made dialogue mp3 files in project resources.

While dialogue audio is playing, block normal player input and clicks. Only `Enter` is allowed, and pressing it skips the current voice line and advances to the next sequence.

## Verified Current Evidence
- The source dialogue file exists: `NC_Seoul_Proj/Assets/TXT/보좌관대사.txt`.
- The current dialogue source contains both `{playerName}` and `XXX` notation.
- The implementation placeholder must be standardized to `{playerName}` only.
- Before implementation, any remaining `XXX` notation in `보좌관대사.txt` must be normalized to `{playerName}`.
- The Varco API guide exists and documents `Text To Speech (lite)`, the voice-list API, the synthesis API, and request/response formats: `NC_Seoul_Proj/Assets/TXT/VarcoVoiceAPIGuide.txt`.
- The voice-list API in the guide is `GET https://openapi.ai.nc.com/tts/lite/v1/api/voices/varco`.
- The synthesis API in the guide is `POST https://openapi.ai.nc.com/tts/lite/v1/api/synthesize`.
- The synthesis response stores base64 encoded audio data in the JSON `audio` field.
- The synthesis API supports `wav`, `mp3`, and `flac` through `media_type`. This feature uses `mp3`.
- The current `MainMenuUI.cs` only checks whether `NameInput` is empty and then calls `SceneManager.LoadScene("InGameScene")`.
- Current code does not persist or pass the player name to the next scene.

## Core Requirements
- Every dialogue sentence is generated through the API.
- There are no user-supplied dialogue mp3 resources.
- Only `{playerName}` is used as the player-name placeholder.
- Replace `{playerName}` with the entered player name, then send the entire final sentence to the API.
- Do not synthesize only the name portion, and do not combine multiple voice files.
- During implementation, provide an inspector or editor window where the user can enter the API key.
- Use the specified `Nadis (Neutral)` voice below.

## Required Varco Voice
Use the following voice as the default during implementation.

```text
index: 966
speaker_uuid: adfc2330-3a22-501b-897d-313d7472f2d8
speaker_name: Nadis (Neutral)
saas_name: Choi A-yeon
description: female, young adult, high pitch, clear, calm
```

Set the request `voice` field to `adfc2330-3a22-501b-897d-313d7472f2d8`, and set the request `language` field to `korean`.

## Required Varco API Request Values
The Varco voice synthesis request must always use the following values:

```text
voice = adfc2330-3a22-501b-897d-313d7472f2d8
language = korean
```

`voice` is the `speaker_uuid` for Nadis (Neutral), and `language` is fixed to `korean` for Korean dialogue synthesis.

## API Key Input
The implementer must create a Unity inspector or editor window where the user can enter the API key.

Recommended approach:
- Create a `VarcoVoiceSettings` component or a `VarcoVoiceSettingsWindow` editor window.
- Provide an `OPENAPI_KEY` input field and a save button.
- Ask the user to enter the API key directly.
- Do not store the API key in Git-tracked assets.
- Store the key in `EditorPrefs`, `Application.persistentDataPath`, or a Git-ignored local path such as `NC_Seoul_Proj/LocalSecrets/varco_voice_api_key.txt`.
- If no key is available, log or display a clear warning before tutorial voice synthesis starts.

Cautions:
- Do not serialize the API key into any `.asset`, `.json`, `.txt`, or scene file under `Assets/`.
- The inspector-visible field is only a development convenience; the saved value must live in a local untracked location.

## Recommended File Structure
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

Do not create a folder for pre-made dialogue voice resources.

## Player Name Persistence
In `MainMenuUI.LoadInGameScene()`, store the entered name immediately before loading the next scene.

Recommended approach:
- `GameSessionData.PlayerName = nameInput.text.Trim();`
- If needed, also use `PlayerPrefs.SetString("PlayerName", value);`.

Start with a simple static `GameSessionData` class. Only expand to a `DontDestroyOnLoad` manager if stored session data grows.

## Full-Sentence API Synthesis Flow
`TutorialVoiceSynthesisManager` owns this flow.

Flow:
1. On `InGameScene` start, read `GameSessionData.PlayerName`.
2. Read the current node `text` from `TutorialDialogueFlow.json`.
3. Replace only `{playerName}` in `text` with the actual player name.
4. Send the fully substituted sentence to `VarcoVoiceClient`.
5. `VarcoVoiceClient` calls `POST /tts/lite/v1/api/synthesize`.
6. Decode the base64 `audio` response.
7. Load the decoded mp3 data as an `AudioClip`, or cache it at runtime under `Application.persistentDataPath/VoiceCache/Tutorial/` if needed.
8. When playback finishes, set the node's `onCompleteSetFlag` and move to `next`.

Example:
```text
Source: 안녕하세요, {playerName}님! 만나서 반가워요!
Player name: 홍길동
API request text: 안녕하세요, 홍길동님! 만나서 반가워요!
```

Do not send only `홍길동님` to the API. Always send the entire final sentence.

## VarcoVoiceClient Details
Voice-list request:

```text
GET https://openapi.ai.nc.com/tts/lite/v1/api/voices/varco
Header: OPENAPI_KEY or openapi_key
```

The guide's sample code uses the `OPENAPI_KEY` header name, while the reference table shows `openapi_key`. Implement first with `OPENAPI_KEY`, and check the lowercase header name if the server returns an authentication failure.

Synthesis request:

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

Response handling:
1. Confirm that HTTP status is 200.
2. Read the `audio` field from the JSON response.
3. Convert it to mp3 bytes with `System.Convert.FromBase64String(audio)`.
4. Load the mp3 bytes as a playable `AudioClip`.
5. Optionally verify that the response `media_type` is `mp3`.

Request limits:
- `text` is limited to 1,200 bytes in UTF-8.
- If a paragraph exceeds 1,200 bytes, split it into multiple nodes in `TutorialDialogueFlow.json`.
- Use `language = korean`.
- Use `voice = adfc2330-3a22-501b-897d-313d7472f2d8`.
- `speed` and `pitch` default to 1. The guide's recommended range is 0.8 to 1.2.
- `n_fm_steps` ranges from 8 to 20.
- Use an explicit `seed` value for reproducibility.

## TutorialVoiceSettings.json Example
```json
{
  "voiceIndex": 966,
  "voice": "adfc2330-3a22-501b-897d-313d7472f2d8",
  "speakerName": "Nadis (Neutral)",
  "saasName": "Choi A-yeon",
  "description": "female, young adult, high pitch, clear, calm",
  "language": "korean",
  "speed": 1.0,
  "pitch": 1.0,
  "n_fm_steps": 8,
  "seed": 1945,
  "media_type": "mp3"
}
```

Do not put the API key in this file.

## TutorialDialogueFlow.json Example
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
      "instruction": "Wait for UI/MoneyPanel click",
      "allowedTargetPath": "UI/MoneyPanel",
      "onActionSetFlag": "money_panel_clicked",
      "next": "money_001"
    }
  ]
}
```

## Dialogue Editing Policy
- To edit a dialogue sentence, update only the node `text` in `TutorialDialogueFlow.json`.
- To move the player name, move the `{playerName}` placeholder inside that node `text`.
- To change dialogue order, update each node's `next` value.
- To set a flag after a specific line finishes, add `onCompleteSetFlag` to that node.
- To change voice, speed, pitch, or seed, update only `TutorialVoiceSettings.json`.
- There is no replacement workflow for user-stored dialogue mp3 files.

## Playback And Skip Rules
`TutorialDialogueRunner` executes one node at a time.

Normal dialogue node:
1. If `lockInput` is true, `TutorialInputLockManager` blocks normal clicks and key input.
2. Replace `{playerName}` in the current node `text` with the player name to build the final sentence.
3. `TutorialVoiceSynthesisManager` synthesizes the full final sentence through the Varco API.
4. When playback finishes, set `onCompleteSetFlag` and move to `next`.

`Enter` input:
- While dialogue is playing, pressing `Enter` stops the current audio.
- The default behavior is to mark the current node complete and move to the next node.
- In a `waitForAction` node, `Enter` does not advance. Only the specified player action is allowed.

## Player Action Gates
`보좌관대사.txt` contains these action gates:

- Tutorial accept
- Click `UI/MoneyPanel`
- Click `UI/SciecnePanel`
- Click `UI/ConveniencePanel`
- Click `UI/PeoplePanel`
- Click `UI/LovePanel`

During dialogue playback, block all actions. When an action gate is reached, allow only the corresponding UI click. Blocking other UI and terrain clicks is safest for the tutorial flow.

## Existing Code Integration Points
`MainMenuUI.cs`
- Add name persistence before scene loading in `LoadInGameScene()`.
- Currently it does not store the name and directly calls `SceneManager.LoadScene(InGameSceneName)`.

`InGameScene`
- Attach `TutorialDialogueRunner`, `TutorialVoiceSynthesisManager`, and `TutorialInputLockManager` to `UI` or a separate `TutorialManager` object.
- Call `TutorialDialogueRunner.StartTutorial()` when the in-game scene starts.

`ResourceDetailPanelManager`
- Tutorial action gates require clicks such as `UI/MoneyPanel`, so this connects to the existing resource panel click behavior.
- Use the verified scene typo `UI/SciecnePanel` for the science panel path.

## Suggested Implementation Order
1. Normalize `XXX` in `보좌관대사.txt` to `{playerName}`.
2. Create `GameSessionData` and store the player name in `MainMenuUI.LoadInGameScene()`.
3. Create the API-key inspector/editor window.
4. Choose a Git-ignored local storage location for the API key.
5. Put the `Nadis (Neutral)` voice values into `TutorialVoiceSettings.json`.
6. Create `VarcoVoiceClient` and verify `media_type = mp3` synthesis requests.
7. Create `TutorialVoiceSynthesisManager` and verify full-sentence synthesis/playback.
8. Draft `TutorialDialogueFlow.json`.
9. Convert each paragraph and action gate from `보좌관대사.txt` into JSON nodes.
10. Implement `Enter` skip and `waitForAction` gates.
11. Connect the input-blocking panel or input-block manager.
12. Add editor validation for API key, voice settings, and dialogue length.

## Verification Criteria
- There is an inspector/editor window where the user can enter the API key.
- If no API key is entered, synthesis does not start and a clear message is produced.
- The request `voice` value is `adfc2330-3a22-501b-897d-313d7472f2d8`.
- The request `language` value is `korean`.
- The same player name entered in `MainScene` is available after entering `InGameScene`.
- `안녕하세요, {playerName}님! 만나서 반가워요!` is sent as one full sentence after name substitution.
- The Varco synthesis request uses `POST /tts/lite/v1/api/synthesize` and `media_type = mp3`.
- The base64 `audio` field from the Varco response is decoded and played.
- Normal UI clicks and terrain clicks are ignored while dialogue plays.
- Pressing `Enter` during dialogue skips the current line and moves to the next node.
- During action gates, only the specified UI target is clickable.
- The `UI/SciecnePanel` click gate uses the verified scene typo.
- Tutorial voice works without any user-stored dialogue mp3 resources.

## Cautions
- Do not commit the API key.
- Do not store pre-made dialogue voice files as project resources.
- Do not synthesize only the player name. Every dialogue line must be synthesized as one final full sentence.
- Do not introduce name placeholders other than `{playerName}`.
- Use the voice-list API only during development/configuration, not every time the game starts.
- Keep the source dialogue file as the reference source, and manage executable flow through JSON data.
- Per project rules, Play Mode verification is left to the user.
