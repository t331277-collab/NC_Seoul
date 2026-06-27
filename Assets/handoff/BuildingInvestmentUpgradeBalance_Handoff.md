# 건물 투자 횟수/주택가 업그레이드 밸런스 설계

## 목적

각 건물 GameObject가 자기 투자 상태를 직접 기억하도록 새 스크립트를 붙이고, 특히 `House1~4` 주택가 건물은 투자 성공 횟수에 따라 모델이 단계적으로 교체되도록 설계한다.

목표 체감 시점은 다음과 같다.

- 주택가 5회 투자 성공: 정상 진행 기준 1960년대 전후에 달성 가능
- 주택가 10회 투자 성공: 정상 진행 기준 2000년대 전후에 달성 가능
- 주택가 최대 성장 한도: 15회 성공 투자

## 현재 확인된 근거

Unity-MCP로 확인한 현재 프로젝트 상태는 다음과 같다.

- CSV 경로: `Assets/Data/StructDefinition.csv`
- CSV 헤더: `건물 이름,출력 이름,해금 기술력,건설 비용,건설 시간,지원 비용,보수 비용,철거 비용,자금생산량,인구수 증가량,기술력 증가량,사랑 증가량,편의성 증가량,이미지 링크,부연설명,설립연도`
- 현재 주택가 지원 비용:
  - `House1`: 5
  - `House2`: 7
  - `House3`: 10
  - `House4`: 12
- 현재 활성 주택가 수:
  - `House1`: 10개
  - `House2`: 10개
  - `House3`: 10개
  - `House4`: 9개
  - 총 활성 주택가: 39개
- 현재 활성 건물 기준 연간 기술력 생산량: 37
- 단순 누적 기술력 기준:
  - 1945년: 0
  - 1960년: 약 555
  - 2000년: 약 2035
- 모델 교체 prefab 존재 확인:
  - `Assets/Prefab/NewHouse.prefab`: 존재
  - `Assets/Prefab/ApartMent.prefab`: 존재

## 중요한 해석

사용자 요구의 `주택가 건물의 경우 총 투자를 15번 받을 수 있음`은 구현 단계에서 아래처럼 해석한다.

- `successfulInvestmentCount`의 최대값을 15로 둔다.
- `totalInvestmentAttemptCount`도 별도로 기록한다.
- 실패한 투자는 시도 횟수에는 기록하지만 15회 성장 슬롯은 소모하지 않는다.

이유:

- 실패까지 포함해 총 15회로 제한하면, 1945년부터 빠르게 투자한 경우 1960년 전후에 모든 시도 횟수를 소모할 수 있다.
- 그러면 2000년대 전후 10회 성공 목표와 충돌한다.
- 따라서 `15번 받을 수 있음`은 성장 가능한 성공 투자 슬롯 15개로 해석하는 편이 목표 연도 밸런스와 맞다.

## 새 스크립트 설계

새 스크립트 이름 제안:

```csharp
StructureInvestmentState.cs
```

부착 대상:

- 모든 건물 GameObject
- 기존 직접 건물: `Stru_`로 시작하는 오브젝트
- CommonSense 건물: `House1`, `House2`, `House3`, `House4`, `DistrictOffice`, `School`, `University`

필드 설계:

```csharp
public class StructureInvestmentState : MonoBehaviour
{
    public int totalInvestmentAttemptCount;
    public int successfulInvestmentCount;
    public int failedInvestmentCount;

    public bool hasPendingInvestment;
    public int pendingResolveYear;
    public int pendingCost;
    public float pendingSuccessChance;

    public bool lastInvestmentSucceeded;
    public int lastResolvedYear;

    public int modelStage; // 0=기본, 1=NewHouse, 2=ApartMent
}
```

저장 방식:

- 우선은 씬 오브젝트 컴포넌트 필드로 저장한다.
- 추후 저장/불러오기 시스템이 생기면 이 값들을 세이브 데이터에 포함한다.

## 투자 결과 공개 타이밍

투자 버튼을 누른 해에는 성공/실패를 즉시 알려주지 않는다.

흐름:

1. 사용자가 투자 버튼 클릭
2. 비용 차감
3. `StructureInvestmentState.hasPendingInvestment = true`
4. `pendingResolveYear = CurrentYear + 1`
5. UI에는 `투자 진행 중` 상태만 표시
6. 다음 해로 넘어갈 때 `pendingResolveYear <= CurrentYear`이면 성공 판정
7. 성공/실패 결과를 그때 공개
8. 성공이면 `successfulInvestmentCount += 1`
9. 실패이면 `failedInvestmentCount += 1`
10. `totalInvestmentAttemptCount += 1`

권장 처리 위치:

- `StructureActionManager`가 투자 시작을 담당한다.
- `StructStageManager.AfterYearProduction` 또는 별도 `BeforeYearProduction` 이벤트에서 pending 투자 결과를 처리한다.
- 결과 공개는 Toast 또는 별도 결과 UI로 확장 가능하다.

## 중복 투자 제한

한 건물에 pending 투자가 걸려 있으면 추가 투자 버튼을 막는다.

규칙:

- `hasPendingInvestment == true`이면 같은 건물에 다시 투자 불가
- 다음 해 결과가 공개된 뒤 다시 투자 가능
- 주택가 `successfulInvestmentCount >= 15`이면 투자 불가

## 주택가 모델 교체 규칙

적용 대상:

- `House1`
- `House2`
- `House3`
- `House4`

모델 단계:

| 성공 횟수 | modelStage | 모델 |
|---:|---:|---|
| 0~4 | 0 | 기존 씬 모델 유지 |
| 5~9 | 1 | `Assets/Prefab/NewHouse.prefab` |
| 10~15 | 2 | `Assets/Prefab/ApartMent.prefab` |

구현 규칙:

- root GameObject 자체는 교체하지 않는다.
- 기존 `House1~4` GameObject는 그대로 유지한다.
- root 아래 시각 모델만 교체한다.
- 이렇게 해야 `StructureInvestmentState`, 활성/비활성 상태, 건설/철거 참조가 깨지지 않는다.

권장 구조:

```text
House1
  VisualRoot
    <현재 모델 또는 prefab instance>
```

교체 방식:

1. `VisualRoot`가 없으면 생성
2. 기존 시각 자식은 비활성 또는 제거
3. 목표 prefab을 `VisualRoot` 하위에 Instantiate
4. `localPosition = Vector3.zero`
5. `localRotation = Quaternion.identity`
6. `localScale = Vector3.one`
7. `modelStage` 갱신

## 기술력 기반 성공 확률

현재 연간 기술력 생산량 37 기준으로 보면, 정상 진행 시 대략 다음 기술력 구간을 가진다.

| 연도 | 예상 기술력 |
|---:|---:|
| 1945 | 0 |
| 1960 | 555 |
| 1980 | 1295 |
| 2000 | 2035 |

목표는 아래와 같다.

- 1~5번째 성공은 1960년대에 노려볼 수 있어야 한다.
- 6~10번째 성공은 낮은 기술력에서는 어렵고, 2000년대 기술력에서 안정적으로 노려야 한다.
- 11~15번째 성공은 엔드게임 성장으로 남긴다.

성공 확률 공식:

```csharp
float GetHouseInvestmentSuccessChance(int science, int successCount)
{
    if (successCount < 5)
    {
        return Mathf.Clamp(0.30f + science * 0.00028f, 0.30f, 0.75f);
    }

    if (successCount < 10)
    {
        return Mathf.Clamp(0.08f + science * 0.00036f, 0.10f, 0.88f);
    }

    return Mathf.Clamp(0.05f + science * 0.00022f, 0.08f, 0.65f);
}
```

확률표:

| 성공 구간 | 1945 기술력 0 | 1960 기술력 555 | 1980 기술력 1295 | 2000 기술력 2035 |
|---|---:|---:|---:|---:|
| 0~4 성공 구간 | 30% | 46% | 66% | 75% cap |
| 5~9 성공 구간 | 10% | 28% | 55% | 81% |
| 10~14 성공 구간 | 8% | 17% | 33% | 50% |

의도:

- 초반에는 5회 성공까지는 반복 투자로 달성 가능하다.
- 5회 이후는 1960년대 기술력으로는 성공률이 낮아 10회 성공이 빨리 터지기 어렵다.
- 2000년대 기술력에서는 6~10번째 성공 구간이 80%대가 되어 `ApartMent` 전환을 안정적으로 노릴 수 있다.

## 투자 비용 밸런스

현재 CSV의 `House1~4` 지원 비용 `5/7/10/12`는 기존 1.5배 임시 지원 효과 기준으로 매우 낮다.

주택가 모델 업그레이드 투자에는 별도 동적 비용을 적용한다.

권장 비용표:

| 목표 성공 번호 | 비용 |
|---:|---:|
| 1 | 80K |
| 2 | 100K |
| 3 | 120K |
| 4 | 150K |
| 5 | 180K |
| 6 | 250K |
| 7 | 320K |
| 8 | 400K |
| 9 | 500K |
| 10 | 650K |
| 11 | 800K |
| 12 | 950K |
| 13 | 1100K |
| 14 | 1300K |
| 15 | 1500K |

공식화하려면 배열로 둔다.

```csharp
private static readonly int[] HouseInvestmentCosts =
{
    80, 100, 120, 150, 180,
    250, 320, 400, 500, 650,
    800, 950, 1100, 1300, 1500
};
```

비용 조회:

```csharp
int nextSuccessIndex = state.successfulInvestmentCount;
int cost = HouseInvestmentCosts[nextSuccessIndex];
```

주의:

- 이 비용은 성공 여부와 관계없이 투자 시점에 차감한다.
- 실패해도 비용은 돌려주지 않는다.
- 실패는 성공 슬롯을 소모하지 않는다.

## 비용 목표 검증

현재 활성 건물 기준 연간 자금 생산량은 약 69다.

단순 누적 기준:

| 연도 | 예상 누적 자금 |
|---:|---:|
| 1960 | 약 1035 |
| 2000 | 약 3795 |

주택가 5회 성공까지 총 비용:

```text
80 + 100 + 120 + 150 + 180 = 630K
```

따라서 한 주택가에 집중하면 1960년대 전후 5회 성공 비용을 감당할 수 있다.

주택가 10회 성공까지 누적 비용:

```text
630 + 250 + 320 + 400 + 500 + 650 = 2750K
```

따라서 2000년대 전후에는 한 주택가를 10회 성공까지 밀어 `ApartMent`로 바꾸는 것이 가능하다.

## 일반 건물 투자 처리

주택가가 아닌 건물도 `StructureInvestmentState`를 가진다.

다만 v0에서는 모델 교체는 하지 않는다.

일반 건물 투자:

- `totalInvestmentAttemptCount` 기록
- `successfulInvestmentCount` 기록
- 기존 `StructureActionManager`의 1~5년 1.5배 생산 버프는 유지 가능
- 추후 일반 건물 전용 업그레이드가 필요하면 같은 상태 스크립트를 확장한다.

## 기존 지원 기능과의 관계

현재 `StructureActionManager`에는 투자 성공 여부 없이 즉시 비용을 차감하고 1~5년 1.5배 생산 버프를 주는 구조가 있다.

새 설계 적용 시 주택가는 아래처럼 바꾼다.

- 주택가 투자: pending 성공 판정 + 성공 횟수 누적 + 모델 교체
- 일반 건물 투자: 기존 1.5배 생산 버프 유지 또는 추후 별도 성공 판정으로 확장

즉, 주택가에 대해서는 `ConfirmInvest()`가 바로 버프를 주지 않고 `StructureInvestmentState`에 pending 투자만 등록하도록 분기한다.

## 구현 순서

1. `StructureInvestmentState.cs` 생성
2. 모든 건물에 `StructureInvestmentState` 자동 부착 도구 또는 런타임 보정 코드 작성
3. `StructureActionManager.ConfirmInvest()`에서 선택 건물의 `StructureInvestmentState` 조회
4. 주택가이면 동적 비용/확률로 pending 투자 등록
5. 일반 건물이면 기존 투자 버프 유지
6. `StructStageManager`의 연도 진행 이벤트에서 pending 투자 결과 판정
7. 성공 시 `successfulInvestmentCount` 증가
8. 주택가 성공 횟수 5/10 도달 시 prefab 교체
9. UI에 pending 상태, 성공 횟수, 남은 성장 가능 횟수 표시
10. Play Mode에서 1960/2000 목표 시점 검증

## 구현 검증 체크리스트

- 모든 활성/비활성 건물에 `StructureInvestmentState`가 붙어 있는가
- `House1~4`는 성공 횟수 최대 15에서 투자 버튼이 막히는가
- pending 투자 중 같은 건물에 중복 투자할 수 없는가
- 다음 해에 성공/실패가 공개되는가
- 성공 5회에서 `Assets/Prefab/NewHouse.prefab` 모델로 바뀌는가
- 성공 10회에서 `Assets/Prefab/ApartMent.prefab` 모델로 바뀌는가
- root GameObject는 유지되고 참조가 깨지지 않는가
- 1960년대 전후 5회 성공이 가능한가
- 2000년대 전후 10회 성공이 가능한가
- 실패 시 비용은 소비되지만 성공 슬롯은 소모되지 않는가


## 추가 확정 설계: 공통건물/일반건물 투자 강화 규칙

이 섹션은 위 문서의 `일반 건물 투자 처리`, `기존 지원 기능과의 관계`, `구현 순서` 일부를 보강하고, 충돌하는 이전 문구를 대체한다.

확인된 프리팹 근거:

- `Assets/Prefab/NewHouse.prefab`: 존재
- `Assets/Prefab/ApartMent.prefab`: 존재
- `Assets/Prefab/NewSchool.prefab`: 존재
- `Assets/Prefab/NewDistrict.prefab`: 존재
- `Assets/Prefab/NewUniversity.prefab`: 존재
- `Assets/Prefab/Structing.prefab`: 존재

## 능력치 배율 공통 원칙

투자 성공 시 능력치 증가 배율은 기본적으로 `1.1배`를 사용한다.

단, 5회 성공과 10회 성공은 임시 1.1배 누적이 아니라 영구 단계 보정으로 고정한다.

권장 계산 방식:

```csharp
// baseValues는 StructDefinition.csv의 원본 제공량이다.
// stagePermanentMultiplier는 0~4회 성공에서 1.0, 5~9회 성공에서 2.0, 10회 이상에서 4.0이다.
// stageSuccessOffset은 현재 영구 단계 이후 추가 성공 횟수다.
effectiveValue = Mathf.CeilToInt(baseValue * stagePermanentMultiplier * (1f + 0.1f * stageSuccessOffset));
```

주택가 예시:

| 성공 횟수 | 영구 단계 | stagePermanentMultiplier | stageSuccessOffset | 원본 대비 최종 배율 |
|---:|---|---:|---:|---:|
| 0 | 기본 | 1.0 | 0 | 1.0 |
| 1 | 기본 | 1.0 | 1 | 1.1 |
| 2 | 기본 | 1.0 | 2 | 1.2 |
| 3 | 기본 | 1.0 | 3 | 1.3 |
| 4 | 기본 | 1.0 | 4 | 1.4 |
| 5 | 1차 영구 강화 | 2.0 | 0 | 2.0 |
| 6 | 1차 영구 강화 이후 재투자 | 2.0 | 1 | 2.2 |
| 7 | 1차 영구 강화 이후 재투자 | 2.0 | 2 | 2.4 |
| 8 | 1차 영구 강화 이후 재투자 | 2.0 | 3 | 2.6 |
| 9 | 1차 영구 강화 이후 재투자 | 2.0 | 4 | 2.8 |
| 10 | 2차 영구 강화 | 4.0 | 0 | 4.0 |
| 11 | 2차 영구 강화 이후 재투자 | 4.0 | 1 | 4.4 |
| 12 | 2차 영구 강화 이후 재투자 | 4.0 | 2 | 4.8 |
| 13 | 2차 영구 강화 이후 재투자 | 4.0 | 3 | 5.2 |
| 14 | 2차 영구 강화 이후 재투자 | 4.0 | 4 | 5.6 |
| 15 | 최종 강화 | 4.0 | 5 | 6.0 |

주의:

- 여기서 10회 성공의 `기존 능력치의 2배`는 5회 성공으로 영구 고정된 값을 다시 2배로 만든다는 의미로 해석한다.
- 따라서 10회 성공 시 원본 CSV 능력치 대비 최종 영구 기준은 `4배`다.
- 만약 기획 의도가 `10회 성공도 원본 대비 2배`라면 이 표를 수정해야 한다.

## 주택가 투자 강화 규칙

대상:

- `House1`
- `House2`
- `House3`
- `House4`

규칙:

- 최대 성공 투자 횟수: 15회
- 투자 성공 여부 공개: 투자한 다음 턴
- 1~4회 성공: 원본 능력치에 1.1배씩 단계 증가
- 5회 성공: 원본 능력치의 2배를 영구 적용하고, 모델을 `Assets/Prefab/NewHouse.prefab`로 교체
- 6~9회 성공: 5회 영구 강화값을 기준으로 다시 1.1배씩 단계 증가
- 10회 성공: 5회 영구 강화값의 2배, 즉 원본 능력치의 4배를 영구 적용하고, 모델을 `Assets/Prefab/ApartMent.prefab`로 교체
- 11~15회 성공: 10회 영구 강화값을 기준으로 다시 1.1배씩 단계 증가

모델 교체는 성공 판정이 난 다음 턴에 수행한다.

## 학교 투자 강화 규칙

대상:

- `School`

규칙:

- 최대 성공 투자 횟수: 10회
- 투자 성공 여부 공개: 투자한 다음 턴
- 1~4회 성공: 원본 능력치에 1.1배씩 단계 증가
- 5회 성공: 원본 능력치의 2배를 영구 적용하고, 기존 학교 모델을 `Assets/Prefab/NewSchool.prefab`로 교체
- 6~9회 성공: 5회 영구 강화값을 기준으로 다시 1.1배씩 단계 증가
- 10회 성공: 5회 영구 강화값의 2배, 즉 원본 능력치의 4배를 영구 적용
- 10회 성공 시 추가 모델 프리팹은 현재 지정되지 않았으므로 모델은 `NewSchool.prefab` 상태를 유지한다.

모델 교체는 투자 버튼을 누른 즉시가 아니라, 다음 턴에 투자 성공 판정이 난 경우에만 수행한다.

## 구청 투자 강화 규칙

대상:

- `DistrictOffice`

규칙:

- 최대 성공 투자 횟수: 10회
- 투자 성공 여부 공개: 투자한 다음 턴
- 1~4회 성공: 원본 능력치에 1.1배씩 단계 증가
- 5회 성공: 원본 능력치의 2배를 영구 적용하고, 기존 구청 모델을 `Assets/Prefab/NewDistrict.prefab`로 교체
- 6~9회 성공: 5회 영구 강화값을 기준으로 다시 1.1배씩 단계 증가
- 10회 성공: 5회 영구 강화값의 2배, 즉 원본 능력치의 4배를 영구 적용
- 10회 성공 시 추가 모델 프리팹은 현재 지정되지 않았으므로 모델은 `NewDistrict.prefab` 상태를 유지한다.

모델 교체는 다음 턴 투자 성공 판정 시점에 수행한다.

## 대학교 투자 강화 규칙

대상:

- `University`

규칙:

- 최대 성공 투자 횟수: 10회
- 투자 성공 여부 공개: 투자한 다음 턴
- 1~4회 성공: 원본 능력치에 1.1배씩 단계 증가
- 5회 성공: 원본 능력치의 2배를 영구 적용하고, 기존 대학교 모델을 `Assets/Prefab/NewUniversity.prefab`로 교체
- 6~9회 성공: 5회 영구 강화값을 기준으로 다시 1.1배씩 단계 증가
- 10회 성공: 5회 영구 강화값의 2배, 즉 원본 능력치의 4배를 영구 적용
- 10회 성공 시 추가 모델 프리팹은 현재 지정되지 않았으므로 모델은 `NewUniversity.prefab` 상태를 유지한다.

모델 교체는 다음 턴 투자 성공 판정 시점에 수행한다.

## 공통건물 분류

공통건물은 아래 7종으로 본다.

- `House1`
- `House2`
- `House3`
- `House4`
- `DistrictOffice`
- `School`
- `University`

공통건물은 같은 건물 이름이 여러 구에 반복해서 존재하므로, 투자 상태는 반드시 hierarchy path 또는 GameObject 참조 기준으로 관리한다.

예:

```text
Seoul/YoungsanGu/Stru_CommonSense/House1
Seoul/GangNamGu/Stru_CommonSense/House1
```

두 오브젝트는 이름이 같아도 서로 다른 투자 상태를 가져야 한다.

## 공통건물을 제외한 일반 건물 투자 규칙

공통건물이 아닌 건물은 아래처럼 정의한다.

- `Stru_`로 시작하는 고유 건물
- 단, `Stru_CommonSense` 컨테이너는 건물이 아니라 컨테이너이므로 제외

규칙:

- 최대 성공 투자 횟수: 3회
- 투자 성공 여부 공개: 투자한 다음 턴
- 모델 교체는 현재 지정하지 않는다.
- 성공 시 능력치는 원본 능력치 기준 1.1배씩 증가한다.
- 일반 건물도 기술력의 영향을 받는다.
- 일반 건물은 제공 자원 총합이 높을수록 투자 성공확률이 낮아진다.
- 기술력이 높을수록 성공확률 보너스가 붙는다.
- 제공 자원 총합이 높은 건물일수록 같은 보너스를 받기 위해 더 높은 기술력을 요구한다.

제공 자원 총합:

```csharp
resourceTotal = MoneyProduction + PeopleIncrease + ScienceIncrease + LoveIncrease + ConvenienceIncrease;
```

일반 건물 성공 확률 공식 제안:

```csharp
float GetUniqueStructureSuccessChance(int science, StructDefinitionData definition)
{
    int resourceTotal = definition.MoneyProduction
                      + definition.PeopleIncrease
                      + definition.ScienceIncrease
                      + definition.LoveIncrease
                      + definition.ConvenienceIncrease;

    float baseChance = Mathf.Clamp(0.28f - resourceTotal * 0.015f, 0.06f, 0.25f);
    float requiredScience = 400f + resourceTotal * 260f;
    float techBonus = Mathf.Clamp01(science / requiredScience) * 0.65f;
    return Mathf.Clamp(baseChance + techBonus, 0.05f, 0.85f);
}
```

의도:

- 자원 총합이 낮은 건물은 낮은 기술력에서도 어느 정도 투자 성공 가능성이 있다.
- 자원 총합이 높은 핵심 건물은 기본 성공률이 낮다.
- 자원 총합이 높은 건물은 `requiredScience`가 커져서 기술력 보너스를 늦게 받는다.
- 기술력이 충분히 높아지면 강한 건물도 최대 85%까지 성공 가능하다.

예시:

| resourceTotal | requiredScience | 낮은 기술력 체감 | 높은 기술력 체감 |
|---:|---:|---|---|
| 5 | 1700 | 비교적 빠르게 보너스 획득 | 안정적 성공 가능 |
| 15 | 4300 | 초중반 성공률 낮음 | 후반부터 안정화 |
| 30 | 8200 | 초중반 매우 어려움 | 매우 높은 기술력 필요 |

## 공통건물 성공 확률 공식 보강

공통건물은 위 일반 건물 공식보다 목표 시점이 명확하므로 별도 공식을 둔다.

- `House1~4`: 기존 문서의 주택가 확률 공식 사용
- `School`, `DistrictOffice`, `University`: 주택가보다 약간 어렵게 설정

공통건물 10회 강화 대상 성공 확률 공식 제안:

```csharp
float GetCommonFacilitySuccessChance(int science, int successCount, int resourceTotal)
{
    float resourcePenalty = Mathf.Clamp(resourceTotal * 0.01f, 0f, 0.20f);

    if (successCount < 5)
    {
        return Mathf.Clamp(0.25f - resourcePenalty + science * 0.00024f, 0.12f, 0.70f);
    }

    return Mathf.Clamp(0.06f - resourcePenalty * 0.5f + science * 0.00030f, 0.08f, 0.82f);
}
```

의도:

- 학교/구청/대학교도 5회 성공은 1960년대 이후 노려볼 수 있다.
- 10회 성공은 2000년대 이후 안정권에 들어오도록 한다.
- 대학교처럼 자원 총합이 높은 건물은 같은 기술력에서도 학교보다 조금 더 어렵다.

## 건설 중 임시 프리팹 규칙

건설 시작 시 현재처럼 완성 건물을 바로 보이지 않게 두는 대신, 건물이 생성될 자리에 `Assets/Prefab/Structing.prefab`를 먼저 소환한다.

건설 흐름:

1. 사용자가 건설 버튼 클릭
2. 비용 차감 성공
3. 실제 완성 건물 GameObject는 계속 비활성 상태 유지
4. 완성 건물의 위치/회전/스케일 기준으로 `Assets/Prefab/Structing.prefab` Instantiate
5. `ConstructionJob`에 임시 공사 오브젝트 참조 저장
6. 건설 시간이 끝날 때까지 임시 공사 오브젝트 유지
7. 건설 완료 시 임시 공사 오브젝트 삭제
8. 실제 완성 건물 GameObject 활성화
9. 생산량/목록 UI 갱신

주의:

- 임시 공사 오브젝트는 생산량 계산 대상이 아니다.
- 임시 공사 오브젝트에는 `StructDefinitionData`를 연결하지 않는다.
- 저장 시스템이 생기면 건설 job과 임시 공사 오브젝트도 저장/복원해야 한다.

## 철거 중 임시 프리팹 규칙

철거도 건설과 마찬가지로 `Assets/Prefab/Structing.prefab`를 사용한다.

철거 흐름:

1. 사용자가 철거 버튼 클릭
2. 철거 비용 차감 성공
3. 철거 대상 건물의 위치/회전/스케일 기준으로 `Assets/Prefab/Structing.prefab` Instantiate
4. 철거 대상 건물은 즉시 비활성화한다.
5. `DemolitionJob`에 임시 철거 오브젝트 참조 저장
6. 철거 시간이 끝날 때까지 임시 철거 오브젝트 유지
7. 철거 완료 시 임시 철거 오브젝트 삭제
8. 철거 대상 건물은 비활성 상태로 유지
9. 생산량/목록 UI 갱신

철거 시간:

- 현재 CSV에는 별도 철거 시간 컬럼이 없다.
- v0에서는 1년 고정으로 둔다.
- 추후 필요하면 CSV에 `철거 시간` 컬럼을 추가한다.

주의:

- 철거 중인 건물은 생산량에 포함하지 않는다.
- 철거 중인 건물은 `CurStruc` 현재 건물 목록에서 빠지고, 필요하다면 `CanBuildStruc`에는 `철거 중` 상태로 잠금 표시한다.

## 구현 데이터 확장 제안

`StructureInvestmentState`에 아래 필드를 추가한다.

```csharp
public int maxSuccessfulInvestments;
public float currentStatMultiplier;
public int permanentMilestoneStage; // 0=없음, 1=5회, 2=10회
public GameObject activeVisualInstance;
public GameObject pendingWorkVisualInstance;
```

건설/철거 job에는 아래 필드를 추가한다.

```csharp
public GameObject WorkVisualObject; // Structing.prefab instance
```

## 기존 즉시 1.5배 버프와의 관계 수정

이전 문서와 현재 코드에는 투자 성공 시 1~5년 1.5배 생산 버프를 주는 흐름이 있다.

이번 확정 설계 이후에는 아래 기준으로 변경한다.

- 공통건물 투자: 1.5배 기간제 버프를 사용하지 않는다.
- 공통건물 투자: 다음 턴 성공/실패 판정 후 성공 횟수와 영구/단계 배율을 적용한다.
- 일반 고유 건물 투자: 최대 3회 성공 투자와 1.1배 단계 증가를 사용한다.
- 기존 1~5년 1.5배 버프는 폐기하거나, 별도 정책 이름 `단기 지원`으로 분리할 때만 유지한다.

즉, `InvestBtn`은 앞으로 `기간제 버프 버튼`이 아니라 `성장 투자 버튼`으로 해석한다.

## 구현 순서 보강

1. `StructureInvestmentState.cs`에 공통 필드 추가
2. 건물 타입 판별 함수 작성
   - `IsHouse`
   - `IsCommonFacility`
   - `IsUniqueStructure`
3. 투자 비용 계산 함수 작성
4. 투자 성공 확률 계산 함수 작성
5. `ConfirmInvest()`에서 즉시 버프 대신 pending 투자 등록
6. 다음 턴 이벤트에서 pending 투자 성공/실패 판정
7. 성공 시 `successfulInvestmentCount` 증가
8. 성공 수에 따라 능력치 배율 재계산
9. 5회/10회 milestone 도달 시 모델 교체
10. 건설 시작 시 `Structing.prefab` 생성
11. 건설 완료 시 `Structing.prefab` 삭제 후 실제 건물 활성화
12. 철거 시작 시 `Structing.prefab` 생성 후 실제 건물 비활성화
13. 철거 완료 시 `Structing.prefab` 삭제
14. UI에 투자 진행 중/성공/실패/강화 횟수 표시

## 추가 검증 체크리스트

- `House1~4` 5회 성공 시 다음 턴에 `NewHouse.prefab`로 교체되는가
- `House1~4` 10회 성공 시 다음 턴에 `ApartMent.prefab`로 교체되는가
- `School` 5회 성공 시 다음 턴에 `NewSchool.prefab`로 교체되는가
- `DistrictOffice` 5회 성공 시 다음 턴에 `NewDistrict.prefab`로 교체되는가
- `University` 5회 성공 시 다음 턴에 `NewUniversity.prefab`로 교체되는가
- 공통건물은 10회 성공 이후 추가 투자가 막히는가
- 주택가는 15회 성공 이후 추가 투자가 막히는가
- 일반 고유 건물은 3회 성공 이후 추가 투자가 막히는가
- 일반 고유 건물의 자원 총합이 높을수록 성공률이 낮게 계산되는가
- 높은 자원 총합 건물일수록 기술력 보너스를 받기 위해 더 높은 기술력이 필요한가
- 건설 시작 시 `Structing.prefab`가 즉시 생성되는가
- 건설 완료 시 `Structing.prefab`가 삭제되고 실제 건물이 활성화되는가
- 철거 시작 시 `Structing.prefab`가 즉시 생성되고 실제 건물이 비활성화되는가
- 철거 완료 시 `Structing.prefab`가 삭제되는가


## 단계별 구현 순서 및 Play 검증 안내 규칙

이 섹션은 실제 Code Builder가 작업을 나눌 때 따라야 하는 순서다. 각 단계는 가능한 한 독립적으로 끝내고, Play Mode에서만 확인 가능한 동작은 구현 완료 후 사용자에게 직접 검증 항목을 설명해야 한다.

### 공통 진행 원칙

- Code Builder는 한 번에 모든 기능을 묶어서 구현하지 않는다.
- 데이터 구조, UI 표시, 투자 판정, 모델 교체, 건설/철거 연출을 단계별로 나누어 구현한다.
- 각 단계가 끝날 때는 다음을 남긴다.
  - 수정한 파일
  - 구현한 기능
  - 에디터 코드나 콘솔로 확인한 항목
  - Play Mode에서 사용자가 확인해야 하는 항목
- Play Mode 검증이 필요한 단계에서는 Builder가 직접 Play 검증을 끝냈다고 말하지 않는다.
- Play Mode 검증이 필요한 경우 반드시 사용자에게 `어떤 씬에서`, `어떤 버튼을 누르고`, `어떤 결과를 봐야 하는지` 구체적으로 안내한다.

## 권장 구현 단계

### 1단계: 투자 상태 스크립트 추가

목표:

- `StructureInvestmentState.cs` 생성
- 각 건물의 투자 시도/성공/실패/pending 상태를 저장할 수 있게 한다.

작업:

1. `Assets/Scripts/Core/StructureInvestmentState.cs` 생성
2. 아래 상태 필드 구현
   - `totalInvestmentAttemptCount`
   - `successfulInvestmentCount`
   - `failedInvestmentCount`
   - `hasPendingInvestment`
   - `pendingResolveYear`
   - `pendingCost`
   - `pendingSuccessChance`
   - `lastInvestmentSucceeded`
   - `lastResolvedYear`
   - `maxSuccessfulInvestments`
   - `currentStatMultiplier`
   - `permanentMilestoneStage`
   - `activeVisualInstance`
   - `pendingWorkVisualInstance`
3. 건물 타입 판별에 필요한 enum 또는 helper 준비

에디터/코드 검증:

- 스크립트 컴파일 에러가 없어야 한다.
- `StructureInvestmentState` 타입을 reflection 또는 Unity-MCP로 찾을 수 있어야 한다.

Play 검증 필요 여부:

- 이 단계만으로는 Play 검증이 필수는 아니다.
- 사용자에게는 `아직 화면에서 확인할 기능은 없고, 다음 단계에서 건물에 상태가 붙는지 확인하게 된다`고 안내한다.

### 2단계: 모든 건물에 투자 상태 부착

목표:

- 모든 투자 가능 건물에 `StructureInvestmentState`를 붙인다.

작업:

1. 런타임 보정 또는 에디터 일괄 부착 로직 작성
2. 대상 포함
   - `Stru_`로 시작하는 고유 건물
   - `House1~4`
   - `DistrictOffice`
   - `School`
   - `University`
3. 대상 제외
   - `Stru_CommonSense` 컨테이너 자체
   - UI 오브젝트
   - 임시 공사/철거 오브젝트

에디터/코드 검증:

- 활성/비활성 건물까지 포함해서 누락 없이 컴포넌트가 붙는지 로그로 출력한다.
- 같은 이름의 `House1`이라도 서로 다른 구의 오브젝트는 각각 독립 컴포넌트를 가져야 한다.

Play 검증 안내:

- 사용자에게 `InGameScene`을 실행한 뒤 아무 지역의 Summary를 열고 기존 건물 목록이 정상 표시되는지 확인해달라고 안내한다.
- 확인할 것:
  - 지역 클릭이 정상 동작하는가
  - `CurStruc` 목록이 정상 표시되는가
  - 건물 row 버튼들이 사라지거나 눌리지 않는 문제가 없는가

### 3단계: 투자 비용/성공 확률 계산 구현

목표:

- 건물 타입에 따라 투자 비용과 성공 확률을 계산한다.

작업:

1. `HouseInvestmentCosts` 배열 구현
2. `GetHouseInvestmentSuccessChance` 구현
3. `GetCommonFacilitySuccessChance` 구현
4. `GetUniqueStructureSuccessChance` 구현
5. 현재 기술력은 `StructStageManager.Science` 값을 사용한다.
6. 일반 고유 건물은 `resourceTotal`이 높을수록 성공 확률이 낮아지고, 필요한 기술력이 높아지도록 한다.

에디터/코드 검증:

- 기술력 0, 555, 2035 기준으로 확률 로그를 출력한다.
- 주택가 0~4 성공 구간, 5~9 성공 구간, 10~14 성공 구간 확률이 문서 표와 크게 어긋나지 않아야 한다.

Play 검증 필요 여부:

- 이 단계는 수치 로그 검증이 중심이라 Play 검증은 선택이다.
- 사용자에게는 `아직 성공/실패 판정 UI는 다음 단계에서 확인한다`고 안내한다.

### 4단계: 투자 시작을 pending 방식으로 전환

목표:

- `InvestBtn` 클릭 시 즉시 성공/버프가 아니라 pending 투자를 등록한다.

작업:

1. `StructureActionManager.ConfirmInvest()` 수정
2. 돈 부족 시 기존 Toast 유지
3. 돈 충분 시 투자 비용 차감
4. `hasPendingInvestment = true`
5. `pendingResolveYear = CurrentYear + 1`
6. `pendingSuccessChance` 저장
7. pending 중인 건물은 다시 투자 불가
8. 기존 1~5년 1.5배 기간제 버프는 투자 성장 시스템에서는 사용하지 않는다.

에디터/코드 검증:

- 투자 버튼 클릭 후 해당 건물의 `hasPendingInvestment`가 true가 되는지 확인한다.
- 같은 건물에 다시 투자하려 할 때 막히는지 확인한다.

Play 검증 안내:

- 사용자에게 `InGameScene`에서 주택가 하나를 골라 지원 버튼을 눌러달라고 안내한다.
- 확인할 것:
  - 돈이 충분하면 비용이 차감되는가
  - 투자 직후 성공/실패가 바로 나오지 않는가
  - 같은 건물에 다시 투자하려 할 때 막히는가
  - 돈이 부족하면 `돈이 모자랍니다!` Toast가 나오는가

### 5단계: 다음 턴 투자 성공/실패 판정

목표:

- 다음 해로 넘어갈 때 pending 투자의 성공/실패를 판정한다.

작업:

1. `StructStageManager`의 연도 진행 이벤트에 투자 판정 처리 연결
2. `pendingResolveYear <= CurrentYear`인 건물을 찾는다.
3. `Random.value <= pendingSuccessChance`로 성공 판정
4. 성공 시 `successfulInvestmentCount += 1`
5. 실패 시 `failedInvestmentCount += 1`
6. 둘 다 `totalInvestmentAttemptCount += 1`
7. `hasPendingInvestment = false`
8. 결과를 UI/Toast/로그 중 최소 하나로 사용자에게 알려준다.

에디터/코드 검증:

- 강제로 성공 확률 100%/0% 케이스를 만들어 성공/실패 카운트가 정확히 증가하는지 확인한다.

Play 검증 안내:

- 사용자에게 투자 후 `NextYearBtn`을 눌러달라고 안내한다.
- 확인할 것:
  - 투자한 바로 다음 해에 결과가 공개되는가
  - 성공하면 성공 횟수가 증가하는가
  - 실패하면 실패 횟수가 증가하는가
  - 결과 공개 후 같은 건물에 다시 투자할 수 있는가

### 6단계: 능력치 배율 적용

목표:

- 투자 성공 횟수에 따라 실제 생산량에 배율을 적용한다.

작업:

1. `currentStatMultiplier` 계산 함수 구현
2. 주택가 5회/10회 영구 강화 배율 적용
3. 학교/구청/대학교 5회/10회 영구 강화 배율 적용
4. 일반 고유 건물 1.1배 단계 증가 적용
5. `StructStageManager`의 생산량 계산에서 `StructureInvestmentState.currentStatMultiplier`를 반영한다.

에디터/코드 검증:

- 성공 횟수별 multiplier 로그 출력
- 예: 0회 1.0, 1회 1.1, 5회 2.0, 6회 2.2, 10회 4.0

Play 검증 안내:

- 사용자에게 투자 성공 전후 `PlusMinus` 값 또는 다음 해 자원 증가량을 비교해달라고 안내한다.
- 확인할 것:
  - 성공 전보다 생산량이 증가하는가
  - 5회 성공 시 증가폭이 2배 기준으로 바뀌는가
  - 실패 시 생산량이 증가하지 않는가

### 7단계: 모델 교체 구현

목표:

- 성공 횟수 milestone 도달 시 모델을 교체한다.

작업:

1. `VisualRoot` 생성/탐색 함수 구현
2. root 건물 GameObject는 유지
3. `House1~4` 5회 성공 시 `NewHouse.prefab`
4. `House1~4` 10회 성공 시 `ApartMent.prefab`
5. `School` 5회 성공 시 `NewSchool.prefab`
6. `DistrictOffice` 5회 성공 시 `NewDistrict.prefab`
7. `University` 5회 성공 시 `NewUniversity.prefab`
8. 모델 교체는 투자 버튼 클릭 시점이 아니라 다음 턴 성공 판정 시점에만 실행

에디터/코드 검증:

- prefab 로드 성공 여부 확인
- 모델 교체 후 root GameObject의 컴포넌트와 hierarchy path가 유지되는지 확인

Play 검증 안내:

- 사용자에게 성공 횟수를 테스트용으로 빠르게 올리는 디버그 방법이 있다면 사용하고, 없다면 임시 확률 100% 테스트로 확인해달라고 안내한다.
- 확인할 것:
  - 5회 성공 전에는 모델이 바뀌지 않는가
  - 5회 성공 결과가 다음 턴에 공개될 때 모델이 바뀌는가
  - 10회 성공 결과가 다음 턴에 공개될 때 주택가는 `ApartMent`로 바뀌는가
  - 모델 교체 후에도 클릭/철거/생산량 계산이 정상인가

### 8단계: 투자 UI 표시 보강

목표:

- 현재 건물 목록에서 투자 상태를 플레이어가 이해할 수 있게 한다.

작업:

1. `CurStruc` row 또는 action panel에 성공 횟수 표시
2. pending 상태 표시
3. 다음 투자 비용 표시
4. 성공 확률 표시 여부 결정
5. 최대 강화 도달 시 투자 버튼 비활성화 또는 잠금 문구 표시

에디터/코드 검증:

- 각 건물 타입별 표시 문자열 확인
- 최대 강화 도달 상태에서 버튼 interactable 여부 확인

Play 검증 안내:

- 사용자에게 강화 전/진행 중/성공 후/최대 강화 상태를 각각 확인해달라고 안내한다.
- 확인할 것:
  - 성공 횟수가 UI에 맞게 표시되는가
  - pending 중에는 투자 버튼이 막히는가
  - 최대 강화 후 투자 버튼이 막히는가

### 9단계: 건설 중 `Structing.prefab` 연출 구현

목표:

- 건설 시작 즉시 건설 위치에 `Structing.prefab`를 표시한다.

작업:

1. `Assets/Prefab/Structing.prefab` 로드
2. 건설 시작 시 완성 건물 위치/회전/스케일 기준으로 Instantiate
3. `ConstructionJob.WorkVisualObject`에 참조 저장
4. 완성 건물은 비활성 유지
5. 건설 완료 시 `WorkVisualObject` 삭제
6. 실제 건물 활성화

에디터/코드 검증:

- 건설 job에 `WorkVisualObject`가 저장되는지 확인
- 건설 완료 시 null/destroy 처리되는지 확인

Play 검증 안내:

- 사용자에게 건설 가능한 건물을 하나 건설해달라고 안내한다.
- 확인할 것:
  - 건설 버튼 클릭 직후 완성 건물이 아니라 `Structing.prefab`가 보이는가
  - 건설 완료 전까지 생산량에 반영되지 않는가
  - 건설 완료 후 `Structing.prefab`가 사라지고 실제 건물이 활성화되는가

### 10단계: 철거 중 `Structing.prefab` 연출 구현

목표:

- 철거 시작 즉시 해당 위치에 `Structing.prefab`를 표시한다.

작업:

1. 철거 시작 시 대상 건물 위치/회전/스케일 기준으로 `Structing.prefab` Instantiate
2. 대상 건물은 즉시 비활성화
3. `DemolitionJob.WorkVisualObject`에 참조 저장
4. v0 철거 시간은 1년 고정
5. 철거 완료 시 `WorkVisualObject` 삭제
6. 대상 건물은 비활성 유지

에디터/코드 검증:

- 철거 job과 임시 오브젝트 참조 확인
- 철거 완료 시 임시 오브젝트 삭제 확인

Play 검증 안내:

- 사용자에게 현재 건물 하나를 철거해달라고 안내한다.
- 확인할 것:
  - 철거 버튼 클릭 직후 실제 건물은 사라지고 `Structing.prefab`가 보이는가
  - 철거 중 생산량에서 제외되는가
  - 다음 해 또는 철거 완료 시점에 `Structing.prefab`가 사라지는가
  - 철거된 건물은 비활성 상태로 남는가

### 11단계: 통합 밸런스 검증

목표:

- 1960년대 5회 성공, 2000년대 10회 성공 목표가 실제 플레이 흐름에서 과도하게 빠르거나 느리지 않은지 확인한다.

작업:

1. 1945년 시작 기준 자금/기술력 누적 시뮬레이션 작성
2. 주택가 한 곳 집중 투자 시 5회 성공 예상 시점 출력
3. 주택가 한 곳 집중 투자 시 10회 성공 예상 시점 출력
4. 학교/구청/대학교 5회 성공 예상 시점 출력
5. 일반 고유 건물 3회 성공 예상 난이도 확인

에디터/코드 검증:

- 반복 시뮬레이션을 여러 번 돌려 평균 성공 시점을 로그로 출력한다.
- 목표 시점과 크게 다르면 비용/확률 보정치를 조정한다.

Play 검증 안내:

- 사용자에게 정상 플레이로는 시간이 오래 걸릴 수 있으므로, 테스트용 빠른 연도 진행 또는 디버그 버튼을 사용할지 안내한다.
- 확인할 것:
  - 1960년대 전후에 주택가 5회 성공이 너무 쉽거나 불가능하지 않은가
  - 2000년대 전후에 주택가 10회 성공이 가능한가
  - 학교/구청/대학교 강화 속도가 주택가보다 과도하게 빠르지 않은가
  - 일반 고유 건물은 자원 총합이 높은 건물일수록 어렵게 느껴지는가

## Play 검증 요청 템플릿

Code Builder는 Play Mode 확인이 필요한 단계가 끝나면 아래 형식으로 사용자에게 안내한다.

```text
Play 검증이 필요한 단계입니다.
씬: InGameScene
검증 대상: <기능 이름>
진행 방법:
1. <사용자가 누를 버튼 또는 조작>
2. <다음 행동>
3. <확인할 UI/오브젝트>
성공 기준:
- <기대 결과 1>
- <기대 결과 2>
문제가 있으면 알려줄 로그/화면:
- <필요한 콘솔 로그 또는 화면 상태>
```

주의:

- Play Mode에서만 확인 가능한 항목은 Builder가 임의로 완료 처리하지 않는다.
- Builder는 에디터 코드로 가능한 검증과 사용자 Play 검증을 구분해서 보고한다.
- 사용자가 Play 검증 결과를 알려주면, 그 결과를 기준으로 다음 단계 구현 여부를 판단한다.
