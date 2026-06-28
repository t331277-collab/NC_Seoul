# 정책 시스템 핸드오프

## 목적

게임 내에서 특정 조건을 만족하면 일정 기간 동안 특수 효과를 발동하는 `정책` 기능을 추가한다.

v0 목표는 아래 흐름이다.

- `Assets/Data/PolicyDefinition.csv`에서 정책 데이터를 읽는다.
- `UI/PolicyBtn`을 클릭하면 `UI/PolicyPanel`을 연다.
- 현재 연도 기준으로 해금된 정책을 `PolicyPanel/Content/Template` 복사본으로 표시한다.
- 정책 항목을 클릭하면 `UI/PolicyChoicePanel`을 열고 적용 여부를 묻는다.
- `Yes`를 누르면 정책 효과가 적용된다.
- `No`를 누르면 적용하지 않고 선택창만 닫는다.

## CSV 생성

새 파일:

```text
Assets/Data/PolicyDefinition.csv
```

필수 컬럼:

| 컬럼 | 설명 |
|---|---|
| 정책이름 | UI에 표시할 정책 이름 |
| 정책 설명 | 정책의 배경 설명 |
| 정책 유효 기간 | 적용 후 효과가 유지되는 연수 |
| 정책 유효 내용 | 코드가 해석할 정책 효과 |
| 요구 능력치 | 적용에 필요한 조건. v0에서는 `없음` 지원 |
| 해금 년도 | 현재 연도가 이 값 이상일 때 목록에 표시 |

초기 데이터:

```csv
정책이름,정책 설명,정책 유효 기간,정책 유효 내용,요구 능력치,해금 년도
전후 주택복구사업,전쟁으로 파괴된 주택을 복구하고 이재민을 위한 주택을 건설합니다. 주택가 건물을 짓는데 필요한 비용이 절반 감소합니다.,5,HouseBuildCostMultiplier=0.5,없음,1945
```

주의:

- 사용자가 요구한 컬럼에는 `정책 유효 내용`이 있었고, 예시 정책에는 `정책 유효 기간: 5년`이 있었다.
- 구현에서는 두 값을 모두 필요로 하므로 `정책 유효 기간`과 `정책 유효 내용`을 별도 컬럼으로 둔다.
- `정책 유효 내용`은 사람이 읽는 문장이 아니라 코드가 파싱할 수 있는 효과 토큰으로 사용한다.

## 첫 정책 정의

정책 이름:

```text
전후 주택복구사업
```

정책 설명:

```text
전쟁으로 파괴된 주택을 복구하고 이재민을 위한 주택을 건설합니다. 주택가 건물을 짓는데 필요한 비용이 절반 감소합니다.
```

정책 유효 기간:

```text
5년
```

정책 유효 내용:

```text
HouseBuildCostMultiplier=0.5
```

요구 능력치:

```text
없음
```

해금 년도:

```text
1945
```

## UI 흐름

### PolicyPanel 열기

대상:

```text
UI/PolicyBtn
UI/PolicyPanel
```

동작:

- `UI/PolicyBtn` 클릭 시 `UI/PolicyPanel`을 활성화한다.
- 패널을 열 때 현재 연도와 `PolicyDefinition.csv`의 `해금 년도`를 비교한다.
- `해금 년도 <= 현재 연도`인 정책만 표시한다.
- 이미 적용 중이거나 이미 사용한 정책은 v0에서 다시 적용하지 못하게 막는 것을 권장한다.

### 정책 목록 표시

대상:

```text
UI/PolicyPanel/Content
UI/PolicyPanel/Content/Template
```

동작:

- `Template`은 원본 템플릿으로 유지하고 런타임에는 비활성화한다.
- 사용 가능한 정책마다 `Template`을 복사한다.
- 복사된 항목에 아래 텍스트를 할당한다.

| Template 하위 오브젝트 | 할당 값 |
|---|---|
| Statue | `정책이름` |
| Desc | `정책 설명` |
| Need | `요구 능력치` |

권장 표시:

- `요구 능력치`가 `없음`이면 `Need`에는 `요구 능력치: 없음` 또는 `없음`으로 표시한다.
- 정책이 적용 중이면 항목에 `적용 중`, 이미 사용 완료면 `사용 완료` 같은 보조 문구를 붙일 수 있다.

### 정책 선택 확인

대상:

```text
UI/PolicyChoicePanel
UI/PolicyChoicePanel/Statue
UI/PolicyChoicePanel/Desc
UI/PolicyChoicePanel/Yes
UI/PolicyChoicePanel/No
```

동작:

- 정책 목록의 복사된 `Template`을 클릭하면 `UI/PolicyChoicePanel`을 활성화한다.
- `PolicyChoicePanel/Statue`에는 정책 이름을 넣는다.
- `PolicyChoicePanel/Desc`에는 정책 설명과 유효 기간/효과 요약을 넣는다.
- `Yes` 클릭 시 정책을 적용한다.
- `No` 클릭 시 정책을 적용하지 않고 `PolicyChoicePanel`만 비활성화한다.

## 정책 적용 상태

v0 권장 런타임 상태:

```csharp
public class ActivePolicyState
{
    public string PolicyName;
    public int AppliedYear;
    public int ExpireYear;
    public string EffectToken;
}
```

기간 처리:

- `전후 주택복구사업`을 1945년에 적용하면 `AppliedYear = 1945`, `ExpireYear = 1950`으로 둔다.
- 효과는 `CurrentYear < ExpireYear` 동안 적용한다.
- 즉 1945, 1946, 1947, 1948, 1949년에 효과가 유효하고 1950년부터 만료된다.

사용 제한:

- v0에서는 같은 정책을 한 번만 적용 가능하게 하는 것을 권장한다.
- 만료 후 재적용을 허용하려면 별도 기획 확인이 필요하다.

## 전후 주택복구사업 효과

효과:

```text
House1, House2, House3, House4 건설 비용 50% 감소
```

적용 위치:

- `StructureActionManager.OpenBuildPanel(...)`
- `StructureActionManager.ConfirmBuild()`
- 건축 가능 목록에서 표시되는 비용 텍스트가 있다면 그 비용도 동일하게 할인값을 사용한다.

구현 기준:

```csharp
if (activePolicy.HasEffect("HouseBuildCostMultiplier") && IsHouseStructureName(definition.Name))
{
    buildCost = Mathf.CeilToInt(definition.BuildCost * 0.5f);
}
```

반올림:

- v0에서는 `Mathf.CeilToInt`를 권장한다.
- 예: `House2` 비용 `45`는 할인 후 `23`이 된다.

주의:

- CSV 원본 `건설 비용`은 수정하지 않는다.
- 정책 효과는 런타임 계산에만 적용한다.
- 돈 부족 체크, 실제 돈 차감, 건축 승인서 UI 설명은 모두 같은 할인 비용을 써야 한다.

## 필요 코드 구조 제안

### PolicyDefinitionDatabase

새 파일 제안:

```text
Assets/Scripts/Core/PolicyDefinitionDatabase.cs
```

역할:

- `Assets/Data/PolicyDefinition.csv` 로드
- CSV 파싱
- `PolicyDefinitionData` 생성

`StructDefinitionDatabase`의 CSV 파서 패턴을 재사용하는 것을 권장한다.

### PolicyManager

새 파일 제안:

```text
Assets/Scripts/Core/PolicyManager.cs
```

부착 위치:

```text
UI
```

역할:

- `UI/PolicyBtn` 클릭 바인딩
- `UI/PolicyPanel` 열기/닫기
- 현재 연도 기준 정책 목록 생성
- 정책 항목 클릭 시 `PolicyChoicePanel` 열기
- `Yes/No` 처리
- 활성 정책 상태 보관
- 다른 시스템에서 정책 효과를 조회할 수 있는 API 제공

필요 API 예시:

```csharp
public bool IsPolicyActive(string policyName)
public bool TryGetFloatEffect(string effectKey, out float value)
public int GetAdjustedBuildCost(StructDefinitionData definition)
```

### StructureActionManager 연동

현재 건설 비용은 `selectedDefinition.BuildCost`를 직접 사용한다.

정책 적용 후에는 아래 경로에서 직접 접근을 줄이고 정책 보정 비용을 사용해야 한다.

- BuildPanel 설명 텍스트
- 건설 가능 여부의 돈 체크
- `TrySpendMoney(...)` 차감 금액
- 로그/디버그 비용 표시

권장:

```csharp
int buildCost = policyManager == null
    ? selectedDefinition.BuildCost
    : policyManager.GetAdjustedBuildCost(selectedDefinition);
```

## 구현 방향

1. `Assets/Data/PolicyDefinition.csv` 생성
   - 요청 컬럼과 초기 정책 1개 추가

2. `PolicyDefinitionDatabase.cs` / `PolicyDefinitionData` 추가
   - UTF-8 CSV 읽기
   - `정책 유효 기간`, `해금 년도`는 int로 파싱
   - `요구 능력치`가 비어 있으면 `없음`으로 처리

3. `PolicyManager.cs` 추가
   - `UI`에 붙이거나 런타임에서 없으면 자동 추가
   - `PolicyBtn`, `PolicyPanel`, `PolicyChoicePanel`, `Content`, `Template`, `Yes`, `No` 바인딩

4. PolicyPanel 목록 생성
   - 현재 연도는 `StructStageManager.CurrentYear` 사용
   - 해금 정책만 표시
   - `Template` 원본은 비활성화
   - 복사본 클릭 이벤트 연결

5. PolicyChoicePanel 확인 흐름 구현
   - 선택한 정책 이름/설명 표시
   - `Yes`: 정책 적용 후 패널 닫기, 목록 새로고침
   - `No`: 정책 미적용 후 패널 닫기

6. 활성 정책 효과 API 구현
   - `HouseBuildCostMultiplier=0.5` 파싱
   - 기간 만료 체크
   - 같은 정책 재적용 방지

7. `StructureActionManager` 건설 비용 연동
   - 주택가 건물 비용 계산에 정책 할인 반영
   - UI 비용 표시와 실제 차감 비용 일치 확인

8. 검증
   - 1945년에 `전후 주택복구사업`이 PolicyPanel 목록에 표시되는지 확인
   - `Need`에 `없음` 표시 확인
   - 정책 항목 클릭 시 PolicyChoicePanel 표시 확인
   - `No` 클릭 시 정책 미적용 확인
   - `Yes` 클릭 후 House1~House4 건설 비용이 절반으로 표시/차감되는지 확인
   - 5년 경과 후 할인 효과가 사라지는지 확인

## QA 체크리스트

- `PolicyDefinition.csv`가 UTF-8로 정상 로드된다.
- `UI/PolicyBtn` 클릭 시 `PolicyPanel`이 열린다.
- 현재 연도보다 해금 연도가 미래인 정책은 보이지 않는다.
- `Template/Statue`, `Template/Desc`, `Template/Need` 텍스트가 올바르게 채워진다.
- 정책 항목 클릭 시 `PolicyChoicePanel`이 열린다.
- `PolicyChoicePanel/Yes` 클릭 시 정책이 적용된다.
- `PolicyChoicePanel/No` 클릭 시 정책이 적용되지 않는다.
- 정책 적용 후 주택가 건설 비용 UI와 실제 차감 금액이 일치한다.
- 정책 만료 후 주택가 건설 비용이 원래 값으로 돌아온다.
