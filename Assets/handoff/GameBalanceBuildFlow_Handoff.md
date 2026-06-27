# 게임 밸런스 및 건설 흐름 구현 Handoff

## 목적

이 문서는 다음 작업자가 `NC_Seoul_Proj`의 전체 게임 루프를 완성하기 전에 먼저 수행해야 할 밸런스 패치와 건설/지원/보수/철거 흐름을 정리한 설계 문서다.

핵심 목표는 다음과 같다.

- `Assets/Data/StructDefinition.csv`를 모든 건물 밸런스와 건설 조건의 원본 데이터로 확장한다.
- 기존 `해금 년도` 컬럼은 제거하고 `해금 기술력`, `건설 비용`, `건설 시간` 컬럼으로 교체한다.
- 현재 1945년 시작 기준 활성화된 건물 수익으로 1955년에 `Stru_SeoulNationalCemetery`를 건설할 수 있도록 밸런스를 맞춘다.
- 기존에 예외 처리했던 `Stru_CommonSense` 하위 건물(`House1~4`, `DistrictOffice`, `School`, `University`)을 CSV와 게임 로직에 포함한다.
- `CurStruc`의 지원/보수/철거 버튼과 `CanBuildStruc`의 건축 버튼을 실제 게임 로직에 연결한다.



## 추가 참고 자료

밸런스와 건설 해금 순서를 잡을 때는 아래 파일을 반드시 참고한다.

- `Assets/TXT/건물 건축 순서.txt`

이 파일은 건물·유적·시설의 건축/조성 연도를 연도순으로 정리한 표다. 단, 절대 해금 순서가 아니다. `해금 기술력`, `건설 비용`, `건설 시간`을 배치할 때 역사적 흐름을 참고하는 보조 자료로만 사용하고, 실제 건설 가능 여부는 돈과 자원 조건으로 판단한다.

특히 `국립서울현충원`은 이 파일에서 `1955년` 항목으로 확인된다. 이 값은 연도 잠금이 아니라 밸런스 목표다. 1945년 시작 기준으로 정상 진행 시 1955년 무렵에는 `Stru_SeoulNationalCemetery`를 지을 만큼 돈과 자원이 모이도록 조정하되, 조건이 더 빨리 충족되면 1955년 전에도 건설 가능해야 한다.

## 현재 확인된 근거

Unity-MCP로 확인한 현재 상태는 아래와 같다.

- 현재 활성 씬: `Assets/Scenes/InGameScene.unity`
- CSV 경로: `Assets/Data/StructDefinition.csv`
- 현재 CSV 헤더: `건물 이름,출력 이름,해금 년도,자금생산량,인구수 증가량,기술력 증가량,사랑 증가량,편의성 증가량,이미지 링크,부연설명,설립연도`
- 현재 CSV 행 수: 헤더 포함 36줄, 건물 35개
- `StructStageManager.cs`는 현재 CSV `columns[2]`를 `UnlockYear`로 읽고, `columns[3]~[7]`을 자원 수치로 읽는다.
- `DistrictStructurePanelManager.cs`는 CSV를 읽어 `CurStruc`, `CanBuildStruc`, `DescStruc`에 표시한다.
- `DistrictStructurePanelManager.cs`는 현재 `Stru_CommonSense`를 무시하고, `Stru_`로 시작하는 직접 자식 건물만 목록에 포함한다.
- `StructStageManager.cs`는 현재 `Stru_`로 시작하는 활성 오브젝트만 생산량에 포함한다. 따라서 `House1`, `DistrictOffice` 등은 CSV에 추가해도 코드 수정 없이는 생산량에 반영되지 않는다.
- `DongJakGu/Stru_SeoulNationalCemetery`는 씬에 존재하며 현재 `activeSelf=False`다.
- 현재 직접 활성 건물은 10개다.
  - `DongDaeMunGu/Stru_DongDaeMun`
  - `Enpyeonggu/Stru_JinGwanSA`
  - `GangDongGu/Stru_AmsaDong`
  - `GangNamGu/Stru_BongEnsa`
  - `JongRohu/Stru_GyoungBokGung`
  - `JongRohu/Stru_JosunChongdokBu`
  - `JungGu/Stru_SeoulMetropolitanGovernment_Gu`
  - `JungGu/Stru_Sungnyemun`
  - `SeoDaemunGu/Stru_Dokripmun`
  - `SungBukGu/Stru_GilSangSA`
- `Stru_CommonSense`는 25개 구 하위에 존재한다.
- `Stru_CommonSense` 하위 공통 건물 타입은 다음 7개다.
  - `House1`
  - `House2`
  - `House3`
  - `House4`
  - `DistrictOffice`
  - `School`
  - `University`
- 시작 시 활성화된 공통 건물 수는 다음과 같다.
  - `House1`: 10개
  - `House2`: 10개
  - `House3`: 10개
  - `House4`: 9개
  - `DistrictOffice`: 9개
  - `School`: 9개
  - `University`: 6개
- 시작 시 비활성 공통 건물 수는 다음과 같다.
  - `House1`: 15개
  - `House2`: 15개
  - `House3`: 15개
  - `House4`: 15개
  - `DistrictOffice`: 15개
  - `School`: 15개
  - `University`: 9개
- `CurStruc` 관련 UI는 비활성 상태로 존재한다.
- `CurStruc` 템플릿 경로: `UI/CurStruc/StruContainer/StrTemplate`
- `CurStruc` 템플릿 버튼 자식:
  - `InvestBtn`
  - `RepairBtn`
  - `DestructBtn`
- `CurStruc` 액션 패널 실제 경로:
  - `UI/CurStruc/StruContainer/InvestPanel`
  - `UI/CurStruc/StruContainer/RepairPanel`
  - `UI/CurStruc/StruContainer/DestPanel`
- `CanBuildStruc` 템플릿 경로: `UI/CanBuildStruc/StruContainer/StrTemplate`
- `CanBuildStruc` 템플릿 버튼 자식:
  - `BuildBtn`
- `BuildPanel` 실제 경로: `UI/CanBuildStruc/BuildPanel`
- `BuildPanel`은 현재 확인 시 `activeSelf=True`였으므로 Builder 단계에서 초기 비활성 처리 필요

## CSV 스키마 설계

현재 `해금 년도` 컬럼은 제거한다. 단, `설립연도`는 건물 설명용 텍스트 컬럼이므로 유지한다.

권장 CSV 헤더는 아래 순서를 사용한다.

```csv
건물 이름,출력 이름,해금 기술력,건설 비용,건설 시간,지원 비용,보수 비용,철거 비용,자금생산량,인구수 증가량,기술력 증가량,사랑 증가량,편의성 증가량,이미지 링크,부연설명,설립연도
```

### 컬럼 의미

- `건물 이름`: 씬 오브젝트 이름과 매칭되는 키다.
  - 예: `Stru_GyoungBokGung`, `House1`, `DistrictOffice`
- `출력 이름`: UI에 표시할 이름이다.
  - `DistrictOffice`는 고정 텍스트가 아니라 `{지역이름}구청` 템플릿 처리 필요
- `해금 기술력`: 건설 가능 목록에 표시되기 위한 최소 누적 기술력
- `건설 비용`: 건설 시작 시 차감할 자금
- `건설 시간`: 건설 완료까지 필요한 연수
- `지원 비용`: 지원 패널에서 `{InvestAmont}`에 표시하고 지원 실행 시 차감할 금액
- `보수 비용`: 보수 패널에서 `{InvestAmont}`에 표시할 금액. 현재는 더미 데이터로만 유지
- `철거 비용`: 철거 패널에서 `{InvestAmont}`에 표시하고 철거 실행 시 차감할 금액
- `자금생산량`: 매년 증가하는 자금
- `인구수 증가량`: 매년 증가하는 인구
- `기술력 증가량`: 매년 증가하는 기술력
- `사랑 증가량`: 매년 증가하는 사랑
- `편의성 증가량`: 매년 증가하는 편의성
- `이미지 링크`: 상세 설명 이미지 경로
- `부연설명`: 상세 설명 본문
- `설립연도`: 설명 UI에 표시할 텍스트. 해금/건설 조건과 무관

### 금액 단위

CSV에는 정수값을 저장한다. UI 표시는 `K` 단위로 축약한다.

예시:

- CSV `650` -> UI `650K`
- CSV `1200` -> UI `1.2M`으로 확장할 수도 있지만, 이번 단계에서는 요청대로 `K` 표기만 우선 사용한다.
- `FormatMoneyK(int amount)` 형태의 공통 포맷터를 만든다.

## CommonSense 건물 추가 설계

아래 행을 CSV에 추가한다.

```csv
House1,주택가1,0,30,1,5,5,3,1,3,0,0,1,,설명글 추가 예정,임시
House2,주택가2,10,45,1,7,7,4,1,4,0,0,1,,설명글 추가 예정,임시
House3,주택가3,25,70,2,10,10,5,2,5,0,0,2,,설명글 추가 예정,임시
House4,주택가4,45,95,2,12,12,6,2,6,0,0,2,,설명글 추가 예정,임시
DistrictOffice,{지역이름}구청,30,150,2,20,20,10,0,10,0,1,8,,설명글 추가 예정,임시
School,학교,40,120,2,15,15,8,0,2,1,0,3,,설명글 추가 예정,임시
University,대학교,120,280,4,35,35,15,0,5,3,1,2,,설명글 추가 예정,임시
```

### DistrictOffice 표시 규칙

`DistrictOffice`는 CSV `출력 이름`에 `{지역이름}구청`을 넣고 런타임에서 치환한다.

예시:

- 선택 지역 표시명 `강남구`
- 오브젝트 이름 `DistrictOffice`
- 표시 이름 `강남구청`

이 치환은 `DistrictStructurePanelManager`에서 `StructDefinition.DisplayName`을 UI에 넣기 직전에 처리한다.

## 밸런스 v0 원칙

아직 전체 경제 루프가 완성되지 않았으므로 처음부터 과도하게 정교한 숫자를 넣지 않는다. v0 목표는 아래 하나다.

- 1945년 시작
- 현재 활성화된 직접 건물 10개와 활성 CommonSense 건물을 생산량에 포함
- `Stru_SeoulNationalCemetery`는 연도 잠금으로 막지 않는다.
- `NextYearBtn`을 눌러 정상적으로 1955년 무렵에 도달했을 때 `Stru_SeoulNationalCemetery`를 지을 만큼 돈과 자원이 모여 있어야 한다.
- 조건이 1955년보다 빨리 충족되면 그 즉시 건설 가능해야 한다.

### 1955 현충원 목표값

`Stru_SeoulNationalCemetery`는 다음 값으로 시작한다.

```csv
Stru_SeoulNationalCemetery,서울국립현충원(국립서울현충원),300,650,2,60,60,25,0,3,3,10,4,Assets/Image/Building/서울국립현충원.png,<기존 설명 유지>,<기존 설립연도 유지>
```

이미지 경로는 실제 파일이 없으면 기존 빈 값 유지가 안전하다. Builder는 실제 이미지 파일 존재 여부를 먼저 확인해야 한다.

### 1955 조건 검증 방식

Builder는 데이터 입력 후 에디터 코드나 테스트 유틸로 아래 시뮬레이션을 해야 한다.

1. 현재 씬의 활성 건물을 기준으로 1945년 연간 생산량을 계산한다.
2. 10번의 `ApplyNextYear`와 동일한 누적 계산을 수행한다.
3. 1955년의 `science >= 300`인지 확인한다.
4. 1955년의 `money >= 650`인지 확인한다.
5. 조건을 만족하지 않으면 CommonSense와 시작 활성 건물의 수치를 먼저 조정한다.

이 검증이 중요한 이유는 현재 UI 텍스트에서 초기 자원을 읽기 때문에, 씬 UI 초기값이 변경되면 실제 도달 시점이 달라질 수 있기 때문이다.

## 기존 35개 건물 밸런스 방향

다음 작업자는 모든 기존 건물을 카테고리별로 나눠 수치를 넣는다.

### 역사/유적/고궁

대상 예시:

- `Stru_AmsaDong`
- `Stru_Sungnyemun`
- `Stru_GyoungBokGung`
- `Stru_Dokripmun`
- `Stru_DongDaeMun`
- `Stru_419`

역할:

- 사랑과 기술력을 적당히 올린다.
- 직접 자금 생산은 낮거나 중간 수준이다.
- 도시 정체성/문화 기반 역할을 한다.

권장 범위:

- 자금: `1~3`
- 인구: `0~2`
- 기술력: `1~3`
- 사랑: `3~8`
- 편의성: `1~3`

### 사찰/공원/문화 공간

대상 예시:

- `Stru_JinGwanSA`
- `Stru_GilSangSA`
- `Stru_BongEnsa`
- `Stru_ChildrenPark`
- `Stru_OlympicPark`
- `Stru_SeoulForest`
- `Stru_YoungMaPark`
- `Stru_ChangPoWon`

역할:

- 사랑과 편의성을 중심으로 올린다.
- 일부 공원은 인구 증가에도 조금 기여한다.

권장 범위:

- 자금: `1~3`
- 인구: `1~3`
- 기술력: `0~1`
- 사랑: `3~7`
- 편의성: `3~7`

### 행정/공공/기념 시설

대상 예시:

- `Stru_SeoulMetropolitanGovernment_Gu`
- `Stru_SeoulMetropolitanGovernment_New`
- `Stru_SeoulNationalCemetery`
- `Stru_NationalAssemblyBuilding`

역할:

- 인구, 편의성, 사랑을 안정적으로 올린다.
- 기술력은 중간 이하로 둔다.

권장 범위:

- 자금: `0~4`
- 인구: `3~8`
- 기술력: `1~3`
- 사랑: `4~10`
- 편의성: `4~8`

### 박물관/교육/연구

대상 예시:

- `Stru_NationalMuseam`
- `Stru_SeoulScienceCenter`
- `Stru_SeoulUniv`

역할:

- 기술력 중심이다.
- 국립중앙박물관은 사랑도 높인다.

권장 범위:

- 자금: `1~3`
- 인구: `1~4`
- 기술력: `5~12`
- 사랑: `2~6`
- 편의성: `2~5`

### 현대 랜드마크/상업/교통/산업

대상 예시:

- `Stru_LotteTower`
- `Stru_Coex`
- `Stru_63building`
- `Stru_GimPoAirPlane`
- `Stru_GaSanDigitalComplex`
- `Stru_GuroDigitalComplex`
- `Stru_DDP`
- `Stru_RedRoad`

역할:

- 자금 생산과 편의성을 크게 올린다.
- 산업단지는 기술력도 올린다.
- 랜드마크는 사랑도 일부 올린다.

권장 범위:

- 자금: `5~18`
- 인구: `2~8`
- 기술력: `2~8`
- 사랑: `1~6`
- 편의성: `3~8`

## 구현 작업 순서

### 1단계: CSV 파서 리팩터

현재 가장 먼저 해야 할 작업이다.

해야 할 일:

- `StructStageManager.cs`에서 `columns[2]`, `columns[3]` 같은 고정 인덱스 파싱을 제거한다.
- CSV 헤더명을 기준으로 값을 읽는 공통 파서 또는 `StructDefinition` 로더를 만든다.
- `DistrictStructurePanelManager.cs`와 `StructStageManager.cs`가 같은 구조체를 쓰도록 정리한다.
- quoted CSV와 쉼표가 들어간 설명 필드가 깨지지 않도록 기존 `ParseCsvLine` 로직을 공통화한다.
- `generateData.ps1`의 필수 컬럼 목록도 새 스키마로 갱신한다.

성공 기준:

- 기존 35개 건물이 새 CSV 스키마로 정상 로드된다.
- `부연설명`에 쉼표가 있어도 파싱이 깨지지 않는다.
- `StructStageManager`와 `DistrictStructurePanelManager`가 같은 수치를 표시한다.

### 2단계: CSV 밸런스 데이터 입력

해야 할 일:

- `해금 년도` 컬럼 삭제
- 새 컬럼 추가
  - `해금 기술력`
  - `건설 비용`
  - `건설 시간`
  - `지원 비용`
  - `보수 비용`
  - `철거 비용`
- 기존 35개 건물 수치를 카테고리별 기준에 맞춰 입력
- `House1~4`, `DistrictOffice`, `School`, `University` 행 추가
- `Stru_SeoulNationalCemetery`는 1955년 목표값으로 조정

성공 기준:

- `generateData.bat` 성공
- CSV 행 수가 기존 35개 + CommonSense 7개 = 최소 42개가 된다.
- 모든 건물의 비용/시간/해금 기술력/생산량이 비어 있지 않다.

### 3단계: CommonSense 포함 규칙 구현

현재 코드는 `Stru_` prefix만 생산량과 목록에 포함한다. 이 규칙을 바꿔야 한다.

해야 할 일:

- `StructStageManager`가 `Stru_CommonSense` 하위 활성 건물도 생산량에 포함하도록 수정한다.
- `DistrictStructurePanelManager`가 선택 지역의 `Stru_CommonSense` 하위 건물을 현재/건축 가능 목록에 포함하도록 수정한다.
- `House1~4`, `DistrictOffice`, `School`, `University`는 `Stru_` prefix가 없어도 CSV 키로 매칭한다.
- `Stru_CommonSense` 루트 자체는 여전히 건물로 표시하지 않는다.

성공 기준:

- 시작 시 활성 CommonSense 건물이 `CurStruc`에 표시된다.
- 비활성 CommonSense 건물이 `CanBuildStruc`에 표시된다.
- `House1`은 `주택가1`로 표시된다.
- `DistrictOffice`는 선택 지역 이름에 따라 `강남구청`, `종로구청`처럼 표시된다.

### 4단계: 자원/돈 관리 API 정리

현재 자원은 `StructStageManager` 내부 필드와 UI 텍스트에 묶여 있다. 건설/지원/철거에서 돈 차감이 필요하므로 최소한의 API가 필요하다.

추천 메서드:

```csharp
public int CurrentYear { get; }
public int Money { get; }
public int Science { get; }
public bool TrySpendMoney(int amount);
public void RefreshPendingValues();
public StatValues GetStructureProduction(string structureKey);
```

주의:

- UI 텍스트를 직접 파싱해서 돈을 차감하지 말고 `StructStageManager`의 내부 값을 바꾼 뒤 UI를 갱신한다.
- `NextYearBtn` 처리와 건설 완료 처리 순서를 명확히 한다.

추천 순서:

1. 연도 증가
2. 건설 대기 턴 감소
3. 완료된 건물 활성화
4. 활성 건물 생산량 적용
5. 지원 버프 남은 기간 감소
6. UI 갱신

### 5단계: 건설 시스템 구현

새 스크립트를 만드는 것이 가장 안전하다.

추천 이름:

- `StructureActionManager.cs`
- 위치: `Assets/Scripts/Core`

담당 기능:

- 선택된 지역과 선택된 건물 기억
- `BuildPanel` 열기
- 건축 비용/시간 표시
- `BuildBtn` 클릭 시 돈 차감
- 건설 대기 목록 등록
- 건설 시간이 끝나면 해당 GameObject `SetActive(true)`
- 건설 완료 후 `CurStruc`, `CanBuildStruc`, 자원 PlusMinus 갱신

건설 대기 데이터 예시:

```csharp
private class ConstructionJob
{
    public string RegionPath;
    public string StructureKey;
    public GameObject TargetObject;
    public int RemainingYears;
}
```

Build 버튼 흐름:

1. `CanBuildStruc`의 건물 row에서 `BuildBtn` 클릭
2. `BuildPanel` 활성화
3. `StruName` 텍스트의 `{StruName}`을 표시명으로 치환
4. `Desc` 텍스트의 `{InvestAmont}`를 `건설 비용`의 `K` 표기로 치환
5. `Year` 텍스트의 `{건설시간}`을 `건설 시간`으로 치환
6. `BuildPanel/BuildBtn` 클릭
7. `TrySpendMoney(건설 비용)` 성공 시 건설 시작
8. 건설 대기 중인 건물은 계속 비활성 상태 유지
9. `건설 시간`만큼 해가 지나면 `SetActive(true)`

### 6단계: 지원/보수/철거 시스템 구현

`CurStruc` row 버튼은 현재 row 전체 클릭이 `DescStruc` 상세 설명을 연다. 자식 버튼 `InvestBtn`, `RepairBtn`, `DestructBtn`을 누를 때 row 전체 버튼 이벤트와 충돌하지 않도록 주의해야 한다.

권장 방식:

- row 전체 버튼은 상세 설명 유지
- 자식 버튼은 `Button.onClick`에서 해당 액션 패널을 연다.
- 자식 버튼 클릭 시 필요하면 `EventSystem.current.currentSelectedGameObject` 또는 별도 이벤트 구조로 row 상세 클릭과 중복 실행을 막는다.

#### 지원

지원 버튼 흐름:

1. `CurStruc` row의 `InvestBtn` 클릭
2. `InvestPanel` 활성화
3. `StruName` 텍스트의 `{StruName}`을 표시명으로 치환
4. `Desc` 텍스트의 `{InvestAmont}`를 `지원 비용`의 `K` 표기로 치환
5. `InvestPanel/InvestBtn` 클릭
6. `TrySpendMoney(지원 비용)` 성공 시 지원 버프 적용
7. 버프 기간은 `Random.Range(1, 6)`으로 1~5년
8. 해당 건물이 제공하는 모든 자원 생산량을 1.5배로 계산

버프 계산 규칙은 반드시 하나로 고정한다.

추천:

```csharp
boostedValue = Mathf.CeilToInt(baseValue * 1.5f);
```

이유:

- 값이 1인 건물도 지원 효과가 눈에 보인다.
- 밸런스 초기에 체감이 쉽다.

#### 보수

현재 단계에서는 더미 데이터로 둔다.

해야 할 일:

- `RepairPanel` 열기
- `StruName`, `Desc` 치환
- 보수 실행 버튼은 로그만 남기거나 아무 효과 없이 닫기
- CSV에는 `보수 비용`만 미리 둔다.

#### 철거

철거 버튼 흐름:

1. `CurStruc` row의 `DestructBtn` 클릭
2. `DestPanel` 활성화
3. `StruName` 텍스트의 `{StruName}`을 표시명으로 치환
4. `Desc` 텍스트의 `{InvestAmont}`를 `철거 비용`의 `K` 표기로 치환
5. `DestPanel/DestBtn` 클릭
6. `TrySpendMoney(철거 비용)` 성공 시 해당 건물 GameObject `SetActive(false)`
7. 현재 목록/건축 가능 목록/자원 PlusMinus 갱신

## UI 경로 주의사항

사용자 요청의 명칭과 실제 씬 경로가 일부 다르다. Builder는 아래 실제 경로를 기준으로 작업한다.

- 지원 패널: `UI/CurStruc/StruContainer/InvestPanel`
- 보수 패널: `UI/CurStruc/StruContainer/RepairPanel`
- 철거 패널: `UI/CurStruc/StruContainer/DestPanel`
- 건축 패널: `UI/CanBuildStruc/BuildPanel`
- 현재 건물 row 템플릿: `UI/CurStruc/StruContainer/StrTemplate`
- 건축 가능 row 템플릿: `UI/CanBuildStruc/StruContainer/StrTemplate`

`BuildPanel`은 확인 시 활성 상태였으므로, `Awake`나 씬 저장 단계에서 비활성화해야 한다.

## 버튼 바인딩 설계

`DistrictStructurePanelManager.CreateItem()`에서 row를 만들 때 액션 버튼도 같이 연결하는 방식이 좋다.

추천 인자:

```csharp
CreateItem(
    StructDefinition definition,
    Transform structureTransform,
    string regionDisplayName,
    bool showActive,
    ...
)
```

이유:

- `DistrictOffice` 표시명 치환에 지역 이름이 필요하다.
- 철거/건설은 실제 GameObject 참조가 필요하다.
- 지원 버프는 특정 건물 인스턴스를 식별해야 한다.

## 데이터 식별 키

동일한 `House1`이 여러 지역에 존재하므로 버프/건설/철거는 건물 이름만으로 식별하면 안 된다.

추천 키:

```text
Seoul/{DistrictName}/Stru_CommonSense/House1
Seoul/{DistrictName}/Stru_SeoulNationalCemetery
```

즉, 런타임 액션 상태는 `GameObject` 참조 또는 hierarchy path를 기준으로 관리한다.

## 1955 현충원 검증 체크리스트

Builder가 구현 후 반드시 확인할 것:

- 1945년 시작 시 `Stru_SeoulNationalCemetery`가 `CanBuildStruc`에 보이더라도 조건 미달이면 BuildBtn은 비활성 또는 잠금 표시된다.
- 1955년은 밸런스 검증 기준일 뿐이며, 기술력과 자금 조건을 더 빨리 만족하면 그 즉시 BuildBtn을 누를 수 있다.
- BuildBtn 클릭 시 내부 비용 정수값이 차감되고, UI에는 `650K`처럼 K 단위로 표시된다.
- 건설 시간이 2년이면 1957년에 현충원이 활성화된다.
- 활성화 후 `CurStruc`에 현충원이 표시된다.
- 활성화 후 다음 해부터 현충원 생산량이 PlusMinus에 반영된다.

## 권장 검증 순서

1. CSV 헤더/행 검증
2. `generateData.bat` 실행
3. Unity 콘솔 에러 확인
4. `StructStageManager`가 새 CSV 스키마로 로드되는지 확인
5. `DistrictStructurePanelManager`가 기존 35개 + CommonSense 7종을 표시하는지 확인
6. 현재 활성 건물 기준 1945 연간 생산량 로그 출력
7. 1955 도달 시 현충원 건설 조건 충족 여부 로그 출력
8. `CurStruc` 지원 패널 열림 확인
9. 지원 실행 후 1~5년 버프 기간과 1.5배 생산 확인
10. 철거 실행 후 해당 GameObject 비활성 확인
11. `CanBuildStruc` 건축 패널 열림 확인
12. 건설 시작 후 돈 차감, 건설 시간 경과, GameObject 활성 확인

## 구현 중 건드리면 안 되는 것

- 기존 `DescStruc` 상세 설명 기능은 유지한다.
- 기존 `TerrainPanel/Summary`, `TerrainPanel/Build` 진입 흐름은 유지한다.
- 기존 지역 클릭/카메라 줌 좌표는 이 작업 범위가 아니다.
- 보수 기능은 현재 더미로 남긴다. 실제 내구도 시스템을 임의로 추가하지 않는다.

## 다음 작업자에게 남기는 결론

가장 위험한 부분은 CSV 컬럼 변경이다. 현재 런타임 스크립트가 컬럼 인덱스에 의존하므로, 밸런스 숫자를 먼저 입력하면 기존 기능이 깨질 가능성이 높다.

따라서 구현 순서는 반드시 아래 순서를 따른다.

1. CSV 헤더 기반 파서로 전환
2. 새 CSV 스키마 적용
3. CommonSense 건물 포함
4. 자원/돈 API 정리
5. 건설 시스템
6. 지원/보수/철거 시스템
7. 1955 현충원 검증

## 확정된 구현 정책

아래 항목은 사용자 답변으로 확정된 정책이다. 다음 Code Builder는 이 기준으로 구현한다.

1. `Assets/TXT/건물 건축 순서.txt`는 절대 해금 순서가 아니다. 건설 자원과 조건이 충분하면 연도 순서와 무관하게 언제든 건설할 수 있다.
2. `국립서울현충원`의 1955년 기준은 연도 잠금이 아니다. 1945년 시작 후 현재 활성 건물과 CommonSense 초기 상태를 기준으로, 플레이가 정상적으로 진행되면 1955년 무렵에는 현충원을 지을 수 있을 만큼 돈과 자원이 모이도록 밸런싱하라는 의미다. 돈과 자원이 더 빨리 충분하면 1955년 전에도 건설 가능해야 한다.
3. CommonSense 건물의 초기 활성 상태는 현재 씬 상태 그대로 기준으로 삼는다. 비활성화된 CommonSense 건물은 `CanBuildStruc` 건설 UI에서 지을 수 있어야 한다.
4. CSV 비용 숫자는 내부 정수값으로 사용한다. 큰 수는 CSV에서 `,` 없이 저장하고, UI 표시에서만 `K` 단위를 붙여 공간을 절약한다.
5. 지원 효과 1.5배는 해당 건물이 제공하는 모든 자원에 적용한다. 대상 자원은 자금, 인구수, 기술력, 사랑, 편의성이다.

작업 현황을 C:\Users\t3312\NC_Seoul\NC_Seoul_Proj\Assets\handoff\Phase.md 에 명시해라