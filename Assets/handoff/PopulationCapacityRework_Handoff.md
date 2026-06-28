# 인구/수용 인구 시스템 개편 핸드오프

## 목적

기존 `People` 자원은 매년 건물 생산량을 그대로 누적하는 단일 수치였다. 이번 개편에서는 인구를 아래 두 값으로 분리한다.

- 현재 인구수: 실제 도시 안에 살고 있는 인구
- 수용 인구수: 주택가가 수용할 수 있는 최대 인구

`UI/PeoplePanel`과 `UI/PanelContainer/PeopleContainer`는 단일 숫자가 아니라 `현재 인구수 / 수용 인구수` 형태를 보여준다.

## 사용자 요구 해석

확정 해석:

- `UI/PeoplePanel` 메인 수치는 `현재 인구수 / 수용 인구수`로 표시한다.
- 현재 인구수 증가량은 `ConveniencePanel`의 편의성 수치에 비례해서 증가한다.
- 현재 인구수가 많을수록 매년 벌어들이는 돈에 보너스를 준다.
- 현재 인구수가 수용 인구수를 초과하면 `PeoplePanel` 메인 수치를 빨간색으로 표시한다.
- 현재 인구수가 수용 인구수를 초과하면 초과량에 비례해 편의성 증가량에 페널티를 준다.
- 수용 인구수는 매턴 생산되는 값이 아니다. 주택가 건물별로 정해지는 정적 수용량의 총합이다.
- 주택가 건물 투자 성공을 통해 해당 주택가의 수용량을 늘릴 수 있다.
- `UI/PanelContainer/PeopleContainer`에도 `현재 인구수 / 수용 인구수` 개념을 반영한다.
- 증가량 표시는 `현재 인구수 증가량 / 수용 인구수 증가량`으로 표시한다.

v0 구현 해석:

- CSV의 기존 `인구수 증가량` 컬럼은 더 이상 매년 현재 인구를 직접 증가시키는 생산량으로 쓰지 않는다.
- `House1~House4`의 `인구수 증가량`은 주택가 기본 수용량 계산의 원천값으로 재해석한다.
- `DistrictOffice`, `School`, `University`, 일반 `Stru_` 건물의 `인구수 증가량`은 v0에서는 현재 인구 직접 생산량도, 수용량도 아니다. 추후 별도 정책이 생기면 확장한다.
- 기존 투자 시스템의 `StructureInvestmentState.currentStatMultiplier`는 주택가 수용량에도 적용한다.

## 현재 코드 근거

현재 확인된 관련 코드:

- `Assets/Scripts/Core/StructStageManager.cs`
  - `people` 필드를 단일 인구 수치로 보관한다.
  - `ApplyNextYear()`에서 `people += pendingValues.People`을 수행한다.
  - `RefreshPendingValues()`에서 `peopleTexts.PlusMinusText`에 `pendingValues.People`만 표시한다.
  - `UpdateMainTexts()`에서 `PeoplePanel` 메인 텍스트에 `people.ToString()`만 표시한다.
- `Assets/Scripts/Core/DistrictStructurePanelManager.cs`
  - 구조물 row의 `People` 텍스트는 `definition.People`을 그대로 표시한다.
- `Assets/Scripts/Core/ResourceDetailPanelManager.cs`
  - `UI/PanelContainer/PeoplePanel` 또는 `PeopleContainer` 계열 상세 패널을 여닫는 바인딩이 있다.
- `Assets/Data/StructDefinition.csv`
  - `House1`: 인구수 증가량 `3`, 편의성 증가량 `1`
  - `House2`: 인구수 증가량 `4`, 편의성 증가량 `1`
  - `House3`: 인구수 증가량 `5`, 편의성 증가량 `2`
  - `House4`: 인구수 증가량 `6`, 편의성 증가량 `2`

기존 블랙보드 기준 현재 활성 주택가 수:

- `House1`: 10개
- `House2`: 10개
- `House3`: 10개
- `House4`: 9개
- 활성 주택가 총합: 39개

기존 블랙보드 기준 현재 활성 건물의 연간 편의성 생산량은 약 `192`다.

## v0 밸런스 제안

### 수치 단위

인구 UI의 숫자 1은 추상 인구 단위로 사용한다. 실제 명칭은 붙이지 않는다.

예:

```text
1044 / 1740
```

이 값은 내부적으로 `1044K / 1740K`처럼 이해해도 되지만, UI에는 `K`를 붙이지 않는다.

### 주택가 기본 수용량

CSV의 `인구수 증가량`에 `10`을 곱해 주택가 1개당 기본 수용량으로 사용한다.

| 건물 | CSV 인구수 증가량 | v0 기본 수용량 |
|---|---:|---:|
| House1 | 3 | 30 |
| House2 | 4 | 40 |
| House3 | 5 | 50 |
| House4 | 6 | 60 |

현재 활성 주택가 기준 초기 수용량 예상:

```text
House1 10개 * 30 = 300
House2 10개 * 40 = 400
House3 10개 * 50 = 500
House4  9개 * 60 = 540
총 수용 인구수 = 1740
```

### 초기 현재 인구수

기존 씬의 `PeoplePanel` 텍스트가 이미 의미 있는 숫자를 가지고 있으면 그 값을 현재 인구수로 읽는다.

단, 기존 값이 `0`이거나 비어 있으면 v0 마이그레이션 초기값은 아래처럼 잡는다.

```csharp
currentPopulation = Mathf.FloorToInt(populationCapacity * 0.6f);
```

현재 활성 주택가 기준이면:

```text
1740 * 0.6 = 1044
초기 표시: 1044 / 1740
```

의도:

- 게임 시작부터 수용량이 비어 있지 않게 한다.
- 초반에는 인구가 증가할 여지가 있다.
- 수용량 초과 페널티는 주택 건설/투자 없이 오래 방치했을 때 발생한다.

### 현재 인구 증가량

현재 인구 증가량은 편의성에 비례한다.

권장 공식:

```csharp
int CalculatePopulationGrowth(int convenience, int currentPopulation, int populationCapacity)
{
    if (populationCapacity <= 0)
    {
        return 0;
    }

    float occupancyRate = (float)currentPopulation / populationCapacity;
    if (occupancyRate >= 1f)
    {
        return 0;
    }

    int baseGrowth = Mathf.CeilToInt(Mathf.Max(0, convenience) * 0.015f);
    float capacityPressure = Mathf.Clamp01((1f - occupancyRate) / 0.4f);
    return Mathf.Max(0, Mathf.CeilToInt(baseGrowth * capacityPressure));
}
```

현재 기준 예시:

```text
편의성 192
현재 인구 1044
수용 인구 1740
점유율 60%
baseGrowth = ceil(192 * 0.015) = 3
capacityPressure = 1
현재 인구 증가량 = +3
표시: +3 / +0
```

의도:

- 편의성이 높을수록 인구가 늘어난다.
- 수용량에 가까워질수록 자연 증가가 둔화된다.
- 수용량을 넘으면 자연 증가는 멈춘다.

### 인구 기반 자금 보너스

현재 인구수가 많을수록 매년 자금 생산에 보너스를 준다.

권장 공식:

```csharp
int CalculatePopulationMoneyBonus(int currentPopulation, int populationCapacity)
{
    int effectivePopulation = populationCapacity <= 0
        ? 0
        : Mathf.Min(currentPopulation, populationCapacity);

    return Mathf.FloorToInt(effectivePopulation * 0.02f);
}
```

현재 기준 예시:

```text
현재 인구 1044
수용 인구 1740
자금 보너스 = floor(1044 * 0.02) = +20
```

의도:

- 기존 활성 건물 기준 연간 자금 생산량 약 `69`에 초반 보너스 `+20` 정도를 더한다.
- 인구를 늘리는 것이 자금 경제에 체감되게 한다.
- 수용량 초과분은 자금 보너스에 포함하지 않아 초과 인구 악용을 막는다.

### 수용량 초과 편의성 페널티

현재 인구수가 수용 인구수를 초과하면 편의성 증가량에서 페널티를 뺀다.

권장 공식:

```csharp
int CalculateOverCapacityConveniencePenalty(int currentPopulation, int populationCapacity)
{
    int overCapacity = Mathf.Max(0, currentPopulation - populationCapacity);
    return Mathf.CeilToInt(overCapacity * 0.1f);
}
```

예시:

```text
현재 인구 1850
수용 인구 1740
초과 인구 110
편의성 페널티 = ceil(110 * 0.1) = -11
```

의도:

- 수용량을 조금 넘는 정도는 경고 수준이다.
- 크게 초과하면 편의성이 확실히 깎인다.
- 편의성 생산량이 낮은 상황에서는 총 편의성 증가량이 음수가 될 수 있다.

### 투자에 따른 수용량 증가

주택가 수용량에는 기존 투자 배율을 적용한다.

기존 투자 배율 기준:

| 성공 횟수 | 배율 |
|---:|---:|
| 0 | 1.0 |
| 1 | 1.1 |
| 2 | 1.2 |
| 3 | 1.3 |
| 4 | 1.4 |
| 5 | 2.0 |
| 6 | 2.2 |
| 10 | 4.0 |
| 15 | 6.0 |

예:

```text
House1 기본 수용량 30
투자 성공 5회: ceil(30 * 2.0) = 60
투자 성공 10회: ceil(30 * 4.0) = 120
투자 성공 15회: ceil(30 * 6.0) = 180
```

모델 업그레이드와 수용량 증가 타이밍:

- 투자 버튼을 누른 즉시 수용량이 늘지 않는다.
- 다음 해 성공 판정이 난 뒤 `successfulInvestmentCount`가 증가하고, 그 결과로 수용량이 다시 계산된다.
- 실패하면 수용량은 증가하지 않는다.

## 구현 방향

### 1단계: 데이터 의미 분리

대상 파일:

- `Assets/Scripts/Core/StructStageManager.cs`

권장 필드:

```csharp
private int currentPopulation;
private int populationCapacity;
private int populationGrowthPreview;
private int populationCapacityDeltaPreview;
private Color peopleNormalColor;
```

기존 `people` 필드는 아래 둘 중 하나로 처리한다.

- 권장: `people`을 `currentPopulation`으로 rename
- 안전한 대안: 기존 `people` 필드는 남기고 의미를 `currentPopulation`으로 명확히 주석 처리

공개 API 제안:

```csharp
public int CurrentPopulation { get { return currentPopulation; } }
public int PopulationCapacity { get { return populationCapacity; } }
public int PopulationGrowthPreview { get { return populationGrowthPreview; } }
public int PopulationCapacityDeltaPreview { get { return populationCapacityDeltaPreview; } }
```

### 2단계: 수용량 계산 함수 추가

`StructStageManager`에 아래 계열 함수를 추가한다.

```csharp
private int CalculateCurrentPopulationCapacity()
private int GetStructurePopulationCapacity(Transform target)
private bool IsHouseStructureName(string structureName)
```

규칙:

- 활성 상태인 `House1~House4`만 수용량에 포함한다.
- `Stru_CommonSense` 컨테이너는 제외한다.
- `StructureInvestmentState.currentStatMultiplier`가 있으면 수용량에도 적용한다.
- 수용량은 매년 더하지 않고, 현재 활성 주택가 상태에서 매번 재계산한 총량으로 본다.

### 3단계: 매년 적용 순서 변경

`ApplyNextYear()` 권장 순서:

1. `currentYear += 1`
2. `BeforeYearProduction(currentYear)` 호출
   - 건설 완료
   - 철거 완료
   - 투자 성공/실패 판정
3. 활성 건물 생산량 계산
4. 수용 인구수 재계산
5. 현재 인구 증가량 계산
6. 인구 기반 자금 보너스 계산
7. 수용량 초과 편의성 페널티 계산
8. 자금/기술력/사랑/편의성/현재 인구 적용
9. UI 갱신
10. `AfterYearProduction(currentYear)` 호출
11. preview 갱신

중요:

- 기존 `pendingValues.People`를 그대로 `currentPopulation += pendingValues.People`로 쓰면 안 된다.
- `pendingValues.People`는 v0에서 `currentPopulationGrowth` 의미로만 사용하거나, 별도 필드로 분리한다.

### 4단계: 생산량 계산 수정

`CalculateCurrentStructValues()`에서 건물의 `definition.PeopleIncrease`는 더 이상 매년 현재 인구 증가량으로 더하지 않는다.

권장:

```csharp
total.Money += definition.MoneyProduction;
total.Science += definition.ScienceIncrease;
total.Love += definition.LoveIncrease;
total.Convenience += definition.ConvenienceIncrease;
// total.People += definition.PeopleIncrease; 제거 또는 수용량 계산으로 이동
```

그 뒤:

```csharp
populationCapacity = CalculateCurrentPopulationCapacity();
int populationGrowth = CalculatePopulationGrowth(convenience, currentPopulation, populationCapacity);
int moneyBonus = CalculatePopulationMoneyBonus(currentPopulation, populationCapacity);
int conveniencePenalty = CalculateOverCapacityConveniencePenalty(currentPopulation, populationCapacity);
```

적용:

```csharp
money += pendingValues.Money + moneyBonus;
convenience += pendingValues.Convenience - conveniencePenalty;
currentPopulation += populationGrowth;
```

주의:

- `convenience`가 음수가 될 수 있는지 정책을 결정해야 한다.
- v0 권장: 음수 허용. 단 UI가 깨지면 `Mathf.Max(0, convenience)`로 clamp하는 후속 작업을 따로 잡는다.

### 5단계: PeoplePanel 표시 수정

대상:

- `UI/PeoplePanel/Text (TMP)`
- `UI/PeoplePanel/PlusMinus`

메인 텍스트:

```text
현재 인구수 / 수용 인구수
```

예:

```text
1047 / 1740
```

증가량 텍스트:

```text
현재 인구수 증가량 / 수용 인구수 증가량
```

예:

```text
+3 / +0
```

색상:

- `currentPopulation <= populationCapacity`: 기존 `PeoplePanel/Text (TMP)` 색상 유지
- `currentPopulation > populationCapacity`: 빨간색

권장 빨간색:

```csharp
new Color32(220, 64, 64, 255)
```

### 6단계: PanelContainer PeopleContainer 반영

대상:

- `UI/PanelContainer/PeopleContainer`

구현자는 실제 hierarchy를 Unity에서 확인한 뒤 적용한다.

요구:

- 상세 패널의 총합 또는 헤더 수치도 `현재 인구수 / 수용 인구수` 개념을 보여야 한다.
- 증가량 표시는 `현재 인구수 증가량 / 수용 인구수 증가량` 형식을 사용한다.

권장 표시:

```text
현재 인구: 1047 / 1740
증가량: +3 / +0
```

만약 기존 상세 패널이 건물별 row만 표시하는 구조라면:

- 주택가 row의 `People` 칸은 `0 / +수용량` 또는 `수용 +30`처럼 바꾼다.
- 비주택 row의 `People` 칸은 `0 / +0` 또는 빈 값으로 둔다.
- 전체 패널 상단 또는 합계 텍스트가 있으면 `+현재 인구 증가량 / +수용 인구수 증가량`을 표시한다.

### 7단계: 구조물 목록 row 표시 정책

`DistrictStructurePanelManager.CreateItem()`은 현재 `People` 칸에 `definition.People`만 표시한다.

v0 권장:

- `CurStruc` / `CanBuildStruc` row의 `People` 칸은 주택가라면 수용량 기여치를 보여준다.
- 비주택이라면 `0` 또는 `-`로 표시한다.
- 혼동을 줄이려면 `People` 텍스트 오브젝트가 무엇을 의미하는지 UI 라벨을 `인구`에서 `수용`으로 바꾸는 별도 UI 작업을 고려한다.

단, 이번 handoff의 핵심은 `PeoplePanel`과 `PeopleContainer`이므로 row 라벨 변경은 별도 후속으로 분리해도 된다.

### 8단계: 저장/로드와 기존 값 마이그레이션

현재 저장 시스템이 명확하지 않으므로 v0는 씬 UI 텍스트 기반 초기화를 유지한다.

초기화 규칙:

1. 먼저 수용량을 계산한다.
2. 기존 `PeoplePanel/Text (TMP)`에서 숫자를 읽는다.
3. 텍스트가 `현재 / 수용` 형식이면 왼쪽 값을 현재 인구로 읽는다.
4. 텍스트가 단일 숫자이면 그 값을 현재 인구로 읽는다.
5. 값이 없거나 `0`이면 `capacity * 0.6`을 현재 인구로 사용한다.

파싱 함수는 기존 `ReadTextNumber(...)`를 확장하거나 별도 `ReadPopulationText(...)`를 만든다.

### 9단계: 밸런스 검증

에디터/코드 검증:

- 현재 활성 주택가 기준 수용량이 약 `1740`인지 확인한다.
- 초기 현재 인구가 `1044` 전후로 잡히는지 확인한다.
- 편의성 `192`, 현재 인구 `1044`, 수용량 `1740`에서 인구 증가량이 `+3` 전후인지 확인한다.
- 현재 인구 `1850`, 수용량 `1740`에서 `PeoplePanel`이 빨간색이 되고 편의성 페널티가 `-11` 전후인지 확인한다.
- `House1` 투자 성공 5회에서 해당 주택가 수용량이 `30 -> 60`으로 바뀌는지 확인한다.
- `House1` 투자 성공 10회에서 해당 주택가 수용량이 `30 -> 120`으로 바뀌는지 확인한다.

Play 검증:

```text
Play 검증이 필요한 단계입니다.
씬: InGameScene
검증 대상: 인구/수용 인구 UI 및 연도 진행
진행 방법:
1. InGameScene 실행
2. PeoplePanel 메인 수치를 확인
3. NextYearBtn을 눌러 다음 해로 진행
4. 주택가 건설 또는 투자 성공 후 PeoplePanel을 다시 확인
성공 기준:
- PeoplePanel이 현재 인구 / 수용 인구 형식으로 보인다.
- PlusMinus가 현재 인구 증가량 / 수용 인구 증가량 형식으로 보인다.
- 현재 인구가 수용 인구보다 많으면 PeoplePanel 수치가 빨간색이다.
- 수용량 초과 상태에서 다음 해 편의성 증가량에 초과량 기반 페널티가 반영된다.
- 주택가 투자 성공 또는 건설 완료 후 수용 인구수가 증가한다.
문제가 있으면 알려줄 로그/화면:
- PeoplePanel 메인 수치
- PeoplePanel PlusMinus
- ConveniencePanel PlusMinus
- 투자 성공한 주택가 이름과 성공 횟수
```

## 구현 단계 제안

### 1단계: 내부 인구 모델 추가

- `StructStageManager`에 `currentPopulation`, `populationCapacity`, preview 필드를 추가한다.
- 기존 단일 `people` 흐름을 현재 인구 의미로 분리한다.
- UI 표시는 아직 최소 변경으로 유지한다.
- 컴파일 검증만 수행한다.

### 2단계: 수용량 계산 연결

- 활성 `House1~House4`를 순회해 수용량을 계산한다.
- 기존 투자 배율을 수용량에 적용한다.
- `PeoplePanel` 메인을 `현재 / 수용`으로 바꾼다.
- 초과 시 빨간색 표시를 넣는다.

### 3단계: 인구 증가/돈 보너스/편의성 페널티 적용

- 편의성 기반 현재 인구 증가량을 적용한다.
- 현재 인구 기반 자금 보너스를 적용한다.
- 수용량 초과 편의성 페널티를 적용한다.
- `PeoplePanel/PlusMinus`를 `+현재증가 / +수용증가`로 바꾼다.

### 4단계: 상세 UI 반영

- `UI/PanelContainer/PeopleContainer` hierarchy를 확인한다.
- 총합/헤더/row 중 실제 존재하는 텍스트에 `현재 / 수용`과 `증가 / 수용증가`를 반영한다.
- 구조물 row의 `People` 칸은 주택가 수용량 중심으로 조정한다.

### 5단계: 투자/건설/철거 통합 검증

- 주택가 건설 완료 시 수용량이 증가하는지 확인한다.
- 주택가 철거 완료 시 수용량이 감소하는지 확인한다.
- 주택가 투자 성공 시 수용량이 증가하는지 확인한다.
- 투자 실패 시 수용량이 그대로인지 확인한다.

## 주의할 점

- 수용 인구수는 매턴 더하면 안 된다. 항상 현재 활성 주택가 상태에서 재계산한 총량이어야 한다.
- 현재 인구수는 매턴 편의성에 의해 증가하는 누적값이다.
- CSV `인구수 증가량` 컬럼 이름은 그대로 두되, v0에서는 주택가 수용량의 원천값으로 사용한다.
- 돈 보너스는 수용량 초과분을 포함하지 않는다.
- 수용량 초과 상태에서 현재 인구 증가량은 `0`으로 둔다.
- 빨간색 표시는 `PeoplePanel` 메인 텍스트에만 먼저 적용한다. 상세 패널 색상 확장은 후속으로 분리 가능하다.

