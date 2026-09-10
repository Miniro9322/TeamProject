# GyoungYilARK — 로그라이크 타워디펜스 (팀 프로젝트)

Unity로 제작한 4인 팀 프로젝트입니다. 이 README는 팀원 중 **최진우(Miniro9322)** 가 직접 구현한 부분만 정리한 문서이며, Git 커밋 로그를 기준으로 작성되었습니다.

## 프로젝트 개요

- **장르**: 로그라이크 타워디펜스
- **인원**: 4인 팀 프로젝트
- **기간**: 2026-07-03 ~ 2026-09-08 (약 2개월)
- **기술 스택**: C#, Unity(URP), VContainer(DI), UniTask(async)

## 담당 범위

게임 루프 시스템, 자원 시스템, UI

## 주요 구현 내용

### 1. 스탯 · 버프/디버프 시스템

`StatContainer.cs`, `Modifier.cs`, `BuffManager.cs`, `IUnit.cs`

- 기본값(Base)과 수정자(Modifier)를 `Flat → 장착 가산(%) → 버프 가산(%) → 곱연산` 순서로 합성하는 스탯 계산 파이프라인 설계
- 수정자가 변경될 때만 재계산하는 더티 플래그 캐싱으로 매 프레임 재계산 방지
- `IUnit` 인터페이스로 `HeroUnit`/`EnemyUnit`이 하나의 스탯·버프 파이프라인을 공유하도록 추상화
- `BuffManager`를 VContainer의 `ITickable`로 등록해 지속시간 감소 및 만료된 버프 자동 제거 처리
- 스택형 버프(`ApplyStackingModifier`)와 대상/소스/전체 단위 강제 해제 API 구현 (정화 계열 스킬에 활용)

### 2. 생산 시설 · 자원 시스템

`ProductionFacility.cs`, `ResourcesManager.cs`, `FacilityManager.cs`

- 나무/식량/골드/철/석재/특수자원 6종을 관리하는 `ResourcesManager` 구현, `ref int Resource(ProductionType)`로 자원 접근을 통일해 타입별 분기 제거
- 자원 생산 타이밍을 매 틱이 아닌 **낮/밤 전환 시점 1회**로 설계해 연산량 절감 및 게임플레이 리듬 조율
- 월드 직접 배치 + 오브젝트 풀링(`BuildingPool`) 방식에서 UI 기반 시설 배치 방식으로 아키텍처를 전면 전환 (`BaseConstructor`, `RegionFacilitySlot(s)`, `FacilityBuildChoicePanel`)

### 3. 낮/밤 사이클 · 게임 상태 관리 (FSM)

`Util/FSM.cs`, `Util/GameManager.cs`, `GameState/{DayState, NightState, GameOverState, ResultState}.cs`

- 라이브러리 의존 없이 `IState` 인터페이스 기반의 경량 FSM을 직접 설계해 낮/밤/게임오버/결과 상태 관리
- `Enter/Exit/Update` 생명주기로 상태 전환 시 UI 토글, 빌드 가능 여부, Day 카운트 등 부수효과 캡슐화
- 10일 단위 이벤트(지원 요청 등) 트리거를 상태 전이 로직에 통합

### 4. 주민(시민) 시스템

`Citizen/CitizenManager.cs`, `House.cs`, `CitizenWanderManager.cs`

- 주민 수 증가/배치, 주거지(House) 시스템 구현
- 허브 포인트/홈 포인트 기반의 주민 NPC 배회(Wander) 동작 관리 구현

### 5. 튜토리얼 시스템

`TutorialManager.cs`, `TutorialTriggers.cs`, `TutorialGate.cs`, `TutorialRollback.cs`

- 진행 흐름(Manager) / 완료 판정(Triggers) / 입력·HUD 차단(Gate) / 0일차 리셋(Rollback)으로 역할을 분리해 단일 책임 원칙 적용
- 스텝 정의를 데이터화(`TutorialStepDefinition`)해 기획 수정이 코드 변경 없이 가능하도록 구성

### 6. UI 프레임워크 · 인풋 시스템

`UiManager.cs`, `UiPanelStack.cs`, `ExclusiveUiCoordinator.cs`, `HeldLinkBase.cs`

- 패널 스택(`UiPanelStack`)으로 ESC/닫기 입력이 최상단 패널에만 적용되도록 처리
- 인터페이스(`IExclusiveUiPanel`) 기반으로 동시에 하나만 열려야 하는 패널을 관리하는 `ExclusiveUiCoordinator` 구현, 예외 케이스는 `IPersistentAcrossExclusivePanels` 마커로 처리
- `EventSystem.current.IsPointerOverGameObject()`로 UI 클릭이 월드 입력으로 새는 문제 방지

### 7. 타이틀 · 업그레이드 UI

`TitleUI.cs`, `SlotSelectPanel.cs`, `UpgradeUI.cs`, `BaseUpgradeData.cs`

- 타이틀 화면, 거점·영웅 업그레이드 UI 구현
- `UpgradeState`와 연동해 업그레이드 비용/효과를 데이터 기반으로 계산

### 8. 설정 · 단축키 · 게임 속도 제어

`SettingUI.cs`, `GameSpeedUI.cs`, `Speed.cs`

- 설정창(사운드/옵션) UI 제작
- 스페이스바로 시간 정지 후 재개 시 정지 전 배속으로 복귀하는 게임 속도 제어 로직 구현
- 다수 기능에 단축키 지원 추가
