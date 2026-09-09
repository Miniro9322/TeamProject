# 팀 프로젝트 포트폴리오 소재 정리 — GyoungYilARK (로그라이크 타워디펜스)

> Git 커밋 로그(`justly2000@naver.com`, 계정명 최진우/Miniro9322)를 기준으로 본인이 작업한 범위만 추린 자료입니다.
> 담당 폴더는 `Assets/GameLoop(ChoiJinWoo)/` 전체이며, 팀원 4명 중 커밋 수 기준 3위(215건, 병합 제외 144건)로 참여했습니다.

## 0. 프로젝트 개요

- **장르**: Unity 로그라이크 타워디펜스 (4인 팀 프로젝트)
- **기술 스택**: C#, VContainer(DI), UniTask(async), URP
- **기간**: 2026-07-03 ~ 2026-09-08 (약 2개월)
- **저장소**: github.com/Miniro9322/TeamProject (origin: KimKwangHwan/GyoungYilARK)
- **본인 담당**: 커밋 144건(병합 제외) / 215건(병합 포함), 1,100여 개 파일 변경 이력
- **주요 작업 브랜치**: `ExpeditionSystem`, `HeroUnlock`, `GameSpeed`, `Setting`, `BaseUpgradeSystem`, `BaseUI`, `SceneDesign`, `Tutorial`, `CombatTutorial`, `HotKey`, `GuideDetail`, `TitleUI`, `UIDetail`

---

## 1. 스탯 · 버프/디버프 시스템

**핵심 파일**: `StatContainer/Stat.cs`, `StatContainer.cs`, `Modifier.cs`, `BuffManager.cs`, `IUnit.cs`

- 기본값(Base) + 수정자(Modifier) 조합으로 계산되는 범용 스탯 컨테이너 설계. `Flat → 장착 계열 가산(%) → 버프 계열 가산(%) → 곱연산` 순서로 값을 합성하는 계산 파이프라인을 직접 설계.
- `isModifierChanged` 더티 플래그를 둬서 수정자가 바뀔 때만 재계산하고, 그 외에는 캐시된 값을 반환하도록 처리 (매 프레임 재계산 방지).
- `IUnit` 인터페이스로 `HeroUnit`/`EnemyUnit`이 스탯 시스템을 공유하도록 추상화 — 아군/적군 로직 분기 없이 하나의 스탯·버프 파이프라인 재사용.
- `BuffManager`를 VContainer의 `ITickable`로 등록해 프레임마다 지속시간을 감소시키고 만료된 버프의 Modifier를 자동 제거하는 구조로 구현.
- 스택형 버프(`ApplyStackingModifier`) 지원: 동일 소스(source)의 버프가 중복 적용될 때 `maxStacks`까지 스택을 쌓고, 이후엔 지속시간만 갱신.
- 강제 해제 API(`RemoveBuff`, `RemoveBuffs`, `RemoveAllBuffs`) 제공 — 대상별/소스별/전체 단위로 선택적 해제가 가능해 정화(cleanse) 계열 스킬 구현에 활용.

**포트폴리오 어필 포인트**: "성능(캐싱)과 확장성(인터페이스 추상화)을 함께 고려한 데이터 기반 스탯/버프 아키텍처 설계"

---

## 2. 생산 시설 · 자원 시스템

**핵심 파일**: `ProductionFacility/ProductionFacility.cs`, `ResourcesManager.cs`, `FacilityManager.cs`, `ProductionValue.cs`, `ResourceCost.cs`

- 나무/식량/골드/철/석재/특수자원 6종 자원을 관리하는 `ResourcesManager` 구현. 자원 접근을 `ref int Resource(ProductionType)`로 통일해 타입별 분기 코드 중복을 제거.
- 자원 생산 타이밍을 매 틱이 아닌 **낮/밤 전환 시점 1회**로 설계 — 불필요한 연산을 줄이고 플레이어가 하루 단위로 생산량을 체감하도록 게임플레이 리듬을 의도적으로 설계.
- 초기 버전은 월드에 건물을 직접 배치하고 오브젝트 풀링(`BuildingPool`)으로 재사용하는 방식으로 구현했으나, 이후 "명일방주" 스타일의 **UI 기반 시설 배치 방식으로 아키텍처를 전면 전환** — 기존 배치 시스템을 걷어내고 `BaseConstructor`, `RegionFacilitySlot(s)`, `FacilityBuildChoicePanel` 기반의 새 구조로 재설계.
- 세이브/로드 시 자원 상태를 복원하는 `RestoreResources` 등 저장 시스템과의 연동 지점 구현.

**포트폴리오 어필 포인트**: "플레이 테스트 피드백을 반영해 기존 배치 시스템을 과감히 폐기하고 UI 기반 구조로 재설계한 경험" — 요구사항 변화에 대응한 리팩토링 사례로 좋음.

---

## 3. 낮/밤 사이클 · 게임 상태 관리(FSM)

**핵심 파일**: `Util/FSM.cs`, `Util/GameManager.cs`, `Util/GameState/{DayState, NightState, GameOverState, ResultState}.cs`, `Enviroment/EnviromentManager.cs`, `DayNightData.cs`

- 직접 설계한 경량 FSM(`IState` 인터페이스 + `FSM.ChangeState`)으로 낮(Day)/밤(Night)/게임오버/결과 상태를 관리. `Enter/Exit/Update` 생명주기로 상태 전환 시 UI 토글, 빌드 가능 여부, 일차(Day) 카운트 증가 등 부수효과를 상태 클래스 안에 캡슐화.
- `DirectionalLight` 회전 + 커스텀 HLSL 블렌딩 스카이박스 셰이더로 낮/밤 전환을 시각적으로 구현.
- 10일 단위 이벤트(지원 요청 등) 트리거를 상태 전이 로직에 통합.

**포트폴리오 어필 포인트**: "라이브러리 의존 없이 직접 구현한 FSM으로 게임 루프의 핵심 상태를 관리"

---

## 4. 주민(시민) 시스템

**핵심 파일**: `Citizen/CitizenManager.cs`, `AddCitizen.cs`, `House.cs`, `HouseConfig.cs`, `CitizenWanderManager.cs`, `CitizenNPC.cs`

- 주민 수 증가/배치와 주거지(House) 시스템 구현. 영웅 배치 시 필요한 주민 수 검증 로직에서 발견된 버그(주민 부족해도 배치되는 문제)를 직접 수정.
- 주민 NPC의 배회(Wander) 동작을 허브 포인트/홈 포인트 기반으로 관리하는 `CitizenWanderManager` 구현.

---

## 5. 튜토리얼 시스템

**핵심 파일**: `Tutorial/TutorialManager.cs`, `TutorialState.cs`, `TutorialTriggers.cs`, `TutorialGate.cs`, `TutorialRollback.cs`, `TutorialOverlayUI.cs`, `TutorialActivityWatcher.cs`

- **책임 분리 설계**: 진행 흐름(TutorialManager) / 완료 판정(TutorialTriggers) / 입력·HUD 차단(TutorialGate) / 0일차 리셋(TutorialRollback)을 역할별 클래스로 분리해 단일 책임 원칙을 적용.
- 스텝 정의를 `TutorialStepDefinition`(ScriptableObject 추정)으로 데이터화해 기획 수정이 코드 변경 없이 가능하도록 구성.
- 최초 구현 이후 **이벤트 버스 기반으로 리팩토링**하여 시스템 간 직접 참조를 줄이고 결합도를 낮춤.
- 전투 튜토리얼(CombatTutorial) 별도 트랙으로 확장, 세이브 데이터와의 호환성까지 처리.

**포트폴리오 어필 포인트**: "단일 책임 원칙에 따라 역할을 4개 클래스로 분리한 튜토리얼 시스템, 이후 이벤트 버스로 리팩토링한 경험" — 설계 개선 스토리로 좋은 소재.

---

## 6. UI 프레임워크 · 인풋 시스템

**핵심 파일**: `UI/UiManager.cs`, `UiPanelStack.cs`, `ExclusiveUiCoordinator.cs`, `HeldLinkBase.cs`, `Tooltip/*`, `BuildModePanel.cs`, 다수의 Panel 클래스

- 패널 스택(`UiPanelStack`)으로 "가장 위에 열린 패널"을 추적해 ESC/닫기 입력이 올바른 패널에만 적용되도록 처리.
- `ExclusiveUiCoordinator`: 한 번에 하나만 열려야 하는 패널들을 인터페이스(`IExclusiveUiPanel`) 기반으로 관리, 새 패널이 열리면 다른 배타적 패널을 자동으로 닫는 정적 코디네이터 구현. `IPersistentAcrossExclusivePanels` 마커 인터페이스로 예외 처리까지 고려.
- `HeldLinkBase`: 버튼을 "누르고 있는" 상태(재배치/철거 등 홀드 인터랙션)를 애니메이터 파라미터·색상 틴트와 동기화하는 추상 클래스 설계 — 이를 상속받는 여러 HeldLink 파생 클래스(`ReplaceHeldLink`, `RemoveHeldLink`, `BuildOptionHeldLink` 등)로 재사용.
- `EventSystem.current.IsPointerOverGameObject()`를 활용해 UI 위 클릭이 월드 입력으로 새는 것을 방지.
- 툴팁, 클릭 이펙트, 커스텀 커서, 버튼 사운드/스케일 연출 등 UX 디테일 다수 구현.

**포트폴리오 어필 포인트**: "인터페이스 기반의 배타적 패널 관리, 홀드 인터랙션 추상 클래스 등 재사용 가능한 UI 인프라 설계"

---

## 7. 거점(베이스) UI · 건설 시스템

**핵심 파일**: `Infrastructure/BaseConstructor.cs`, `BuildableFacility.cs`, `RegionFacilitySlot(s).cs`, `UI/Base/{RegionOverviewPanel, RegionDetailPanel, CenterHubPanel, FacilityBuildChoicePanel, BuildingPanel}.cs`

- 월드 직접 배치 → UI 기반 거점 관리로 전환하는 과정에서 지역(Region) 개념과 슬롯 기반 건설 시스템을 새로 설계.
- 건물 건설/철거/재배치를 확인창(`ConfirmPopUp`)과 연동, 건설 직후 정보 패널이 자동으로 열리도록 UX 개선.

---

## 8. 타이틀 · 세이브 슬롯 · 업그레이드 UI

**핵심 파일**: `Title/TitleUI.cs`, `SlotSelectPanel.cs`, `SlotRowView.cs`, `DaySelectPanel.cs`, `UpgradeUI.cs`, `BaseUpgradeButton.cs`, `BaseUpgradeData.cs`

- 타이틀 화면, 세이브 슬롯 선택/로드 UI, 기초(거점) 업그레이드 및 영웅 업그레이드 UI를 담당.
- `UpgradeState`와 연동해 업그레이드 비용/효과를 데이터 기반으로 계산.

---

## 9. 설정 · 단축키 · 게임 속도 제어

**핵심 파일**: `UI/SettingUI.cs`, `GameSpeedUI.cs`, `Speed.cs`

- 설정창(사운드/옵션) UI 제작.
- 스페이스바로 시간 정지 후 재개 시 정지 전 배속으로 복귀하는 로직 등 게임 속도 제어 시스템 구현.
- 다수의 기능에 단축키 지원 추가.

---

## 10. 시스템 전체 조립 — VContainer DI Composition Root

**핵심 파일**: `VContainer/GameLifeTimeScope.cs`

- 게임의 거의 모든 매니저/시스템(자원, 시민, UI, 환경, 버프, 튜토리얼, 세이브, 웨이브 스포너 등 30개 이상)을 하나의 `LifetimeScope`에서 등록·조립하는 **DI 컴포지션 루트를 직접 관리** — 사실상 게임 전체의 의존성 그래프를 담당.
- `RegisterComponentInNewPrefab` / `RegisterComponentInHierarchy` / `RegisterBuildCallback` 등 VContainer의 다양한 등록 방식을 상황에 맞게 구분해 사용.
- 팀 세션 전반에서 반복적으로 겪은 VContainer 이슈(`.As<ITickable>()` 누락, MonoBehaviour 주입 시 `RegisterComponentInHierarchy` 필요성, 다중 인스턴스로 인한 이벤트 구독 누락 등)를 직접 해결하며 DI 트러블슈팅 경험 축적.

**포트폴리오 어필 포인트**: "30개 이상의 시스템을 아우르는 DI 컴포지션 루트를 설계·유지보수하며 팀 전체의 의존성 주입 이슈를 해결" — 아키텍처/인프라 역량을 보여주는 핵심 소재.

---

## 11. 로컬라이제이션 · 버그 픽스 다수

- `StringTable.csv` 기반 다국어 텍스트 적용 (버튼, 설정창, 툴팁 등).
- 언어별 텍스트 크기 조정, 타이틀 전환 버그, 단축키 충돌, 튜토리얼 회귀 버그, 업그레이드 자원 소모 누락 등 다수의 버그를 커밋 단위로 추적·수정.

---

## 정량 지표 요약 (이력서/포폴 한 줄 요약용)

- 4인 팀 프로젝트, 약 2개월간 커밋 215건(병합 제외 144건) 기여
- 담당 영역: 스탯/버프 시스템, 생산·자원 시스템, 낮/밤 FSM, 주민 시스템, 튜토리얼 시스템, UI 프레임워크, 거점 건설 UI, 타이틀/세이브 UI, VContainer DI 컴포지션 루트 전체
- 게임 아키텍처 리팩토링 경험 2건: (1) 건물 배치 방식을 월드 배치 → UI 기반으로 전면 전환 (2) 튜토리얼 시스템을 이벤트 버스 기반으로 리팩토링

---

## 다음 단계 제안

1. 이 중 2~3개 시스템(추천: 스탯/버프 시스템, VContainer DI 컴포지션 루트, 튜토리얼 리팩토링)을 "문제 → 설계 결정 → 결과"의 스토리로 구체화하면 포폴 임팩트가 커집니다.
2. 실제 플레이 영상/스크린샷, 코드 스니펫(위 경로 참고)을 곁들이면 좋습니다.
3. 원하시면 이 중 하나를 골라 포폴용 상세 설명(문제 상황, 대안 비교, 최종 설계, 배운 점)으로 확장해 드릴 수 있습니다.
