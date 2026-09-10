<div align="center">

# 로그라이크 타워디펜스
팀 프로젝트로 제작한 탑 다운뷰 로그라이크 타워디펜스 게임입니다. 프로젝트 안에서 게임 루프 시스템, 자원 시스템, UI를 맡아 작업했습니다.

</div>

---

## 목차
- [프로젝트 개요](#프로젝트-개요)
- [담당 범위](#담당-범위)
- [핵심 기술 스택](#핵심-기술-스택)
- [주요 구현 내용](#주요-구현-내용)
  - [1. 스탯 · 버프/디버프 시스템](#1-스탯--버프디버프-시스템)
  - [2. 생산 시설 · 자원 시스템](#2-생산-시설--자원-시스템)
  - [3. 낮/밤 사이클 · 게임 상태 관리(FSM)](#3-낮밤-사이클--게임-상태-관리fsm)
  - [4. 주민(시민) 시스템](#4-주민시민-시스템)
  - [5. 튜토리얼 시스템](#5-튜토리얼-시스템)
  - [6. UI 프레임워크 · 인풋 시스템](#6-ui-프레임워크--인풋-시스템)
  - [7. 거점(베이스) UI · 건설 시스템](#7-거점베이스-ui--건설-시스템)
  - [8. 타이틀 · 세이브 슬롯 · 업그레이드 UI](#8-타이틀--세이브-슬롯--업그레이드-ui)
  - [9. 설정 · 단축키 · 게임 속도 제어](#9-설정--단축키--게임-속도-제어)
  - [10. 시스템 전체 조립 — VContainer DI 컴포지션 루트](#10-시스템-전체-조립--vcontainer-di-컴포지션-루트)
  - [11. 로컬라이제이션 · 버그 픽스](#11-로컬라이제이션--버그-픽스)
- [아키텍처 리팩토링 경험](#아키텍처-리팩토링-경험)
- [폴더 구조](#폴더-구조)
- [정량 요약](#정량-요약)

## 프로젝트 개요

- **장르**: 로그라이크 타워디펜스
- **개발 인원**: 4인 팀 프로젝트
- **개발 기간**: 2026.07.03 ~ 2026.09.08 (약 2개월)
- **엔진 / 언어**: Unity 6000.3.15f1(URP) · C#
- **비동기 / DI**: UniTask(Cysharp) · VContainer

## 담당 범위

게임 루프 시스템, 자원 시스템, UI

## 핵심 기술 스택

| 분류 | 사용 기술 |
|---|---|
| 엔진 | Unity 6000.3.15f1 (URP) |
| 의존성 주입 | VContainer — DI 컴포지션 루트(`GameLifeTimeScope`) 직접 설계 |
| 비동기 처리 | UniTask (Cysharp) |
| 상태 관리 | 직접 구현한 `IState`/`FSM` — 낮/밤/게임오버/결과 상태 |

## 주요 구현 내용

### 1. 스탯 · 버프/디버프 시스템

- 기본값(Base)과 수정자(Modifier)를 `Flat → 장착 가산(%) → 버프 가산(%) → 곱연산` 순서로 합성하는 스탯 계산 파이프라인 설계
- 수정자가 변경될 때만 재계산하는 더티 플래그 캐싱으로 매 프레임 재계산 방지
- `IUnit` 인터페이스로 `HeroUnit`/`EnemyUnit`이 하나의 스탯·버프 파이프라인을 공유하도록 추상화
- `BuffManager`를 VContainer의 `ITickable`로 등록해 지속시간 감소 및 만료된 버프 자동 제거 처리
- 스택형 버프(`ApplyStackingModifier`)와 대상/소스/전체 단위 강제 해제 API 구현 (정화 계열 스킬에 활용)

`Assets/GameLoop(ChoiJinWoo)/Scripts/StatContainer/`

### 2. 생산 시설 · 자원 시스템

- 나무/식량/골드/철/석재/특수자원 6종을 관리하는 `ResourcesManager` 구현, `ref int Resource(ProductionType)`로 자원 접근을 통일해 타입별 분기 제거
- 자원 생산 타이밍을 매 틱이 아닌 **낮/밤 전환 시점 1회**로 설계해 연산량 절감 및 게임플레이 리듬 조율
- 월드 직접 배치 + 오브젝트 풀링(`BuildingPool`) 방식에서 UI 기반 시설 배치 방식으로 아키텍처를 전면 전환 (`BaseConstructor`, `RegionFacilitySlot(s)`, `FacilityBuildChoicePanel`)

`Assets/GameLoop(ChoiJinWoo)/Scripts/ProductionFacility/`

### 3. 낮/밤 사이클 · 게임 상태 관리(FSM)

- 라이브러리 의존 없이 `IState` 인터페이스 기반의 경량 FSM을 직접 설계해 낮/밤/게임오버/결과 상태 관리
- `Enter/Exit/Update` 생명주기로 상태 전환 시 UI 토글, 빌드 가능 여부, Day 카운트 등 부수효과 캡슐화
- 10일 단위 이벤트(지원 요청 등) 트리거를 상태 전이 로직에 통합

`Assets/GameLoop(ChoiJinWoo)/Scripts/Util/`

### 4. 주민(시민) 시스템

- 주민 수 증가/배치, 주거지(House) 시스템 구현
- 영웅 배치 시 주민 수 검증 로직에서 발견한 버그(주민 부족해도 배치되는 문제) 직접 수정
- 허브 포인트/홈 포인트 기반의 주민 NPC 배회(Wander) 동작 관리 구현

`Assets/GameLoop(ChoiJinWoo)/Scripts/Citizen/`

### 5. 튜토리얼 시스템

- 진행 흐름(Manager) / 완료 판정(Triggers) / 입력·HUD 차단(Gate) / 0일차 리셋(Rollback)으로 역할을 분리해 단일 책임 원칙 적용
- 스텝 정의를 데이터화(`TutorialStepDefinition`)해 기획 수정이 코드 변경 없이 가능하도록 구성
- 전투 튜토리얼(CombatTutorial)을 별도 트랙으로 확장, 세이브 데이터 호환성까지 처리

`Assets/GameLoop(ChoiJinWoo)/Scripts/Tutorial/`

### 6. UI 프레임워크 · 인풋 시스템

- 패널 스택(`UiPanelStack`)으로 ESC/닫기 입력이 최상단 패널에만 적용되도록 처리
- 인터페이스(`IExclusiveUiPanel`) 기반으로 동시에 하나만 열려야 하는 패널을 관리하는 `ExclusiveUiCoordinator` 구현, 예외 케이스는 `IPersistentAcrossExclusivePanels` 마커로 처리
- 버튼 홀드 인터랙션(재배치/철거 등)을 애니메이터·색상 틴트와 동기화하는 `HeldLinkBase` 추상 클래스 설계, 다수의 파생 클래스로 재사용
- `EventSystem.current.IsPointerOverGameObject()`로 UI 클릭이 월드 입력으로 새는 문제 방지
- 툴팁, 클릭 이펙트, 커스텀 커서 등 UX 디테일 다수 구현

`Assets/GameLoop(ChoiJinWoo)/Scripts/UI/`

### 7. 거점(베이스) UI · 건설 시스템

- 월드 직접 배치 → UI 기반 거점 관리로 전환하는 과정에서 지역(Region) 개념과 슬롯 기반 건설 시스템 신규 설계
- 건설/철거/재배치를 확인창과 연동하고, 건설 직후 정보 패널이 자동으로 열리도록 UX 개선

`Assets/GameLoop(ChoiJinWoo)/Scripts/Infrastructure/`

### 8. 타이틀 · 업그레이드 UI

- 타이틀 화면, 거점·영웅 업그레이드 UI 구현
- `UpgradeState`와 연동해 업그레이드 비용/효과를 데이터 기반으로 계산

`Assets/GameLoop(ChoiJinWoo)/Scripts/Title/`

### 9. 설정 · 단축키 · 게임 속도 제어

- 설정창(사운드/옵션) UI 제작
- 스페이스바로 시간 정지 후 재개 시 정지 전 배속으로 복귀하는 게임 속도 제어 로직 구현
- 다수 기능에 단축키 지원 추가

### 10. 시스템 전체 조립 — VContainer DI 컴포지션 루트

- 자원, 시민, UI, 환경, 버프, 튜토리얼, 세이브, 웨이브 스포너 등 30개 이상의 매니저/시스템을 하나의 `LifetimeScope`에서 등록·조립하는 DI 컴포지션 루트를 직접 관리
- `RegisterComponentInNewPrefab` / `RegisterComponentInHierarchy` / `RegisterBuildCallback` 등 VContainer의 다양한 등록 방식을 상황에 맞게 구분 적용
- `.As<ITickable>()` 누락, MonoBehaviour 주입 시 필요한 `RegisterComponentInHierarchy`, 다중 인스턴스로 인한 이벤트 구독 누락 등 팀에서 반복적으로 발생한 VContainer 이슈를 직접 해결

`Assets/GameLoop(ChoiJinWoo)/Scripts/VContainer/GameLifeTimeScope.cs`
