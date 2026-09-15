# 어쩌다 군단장 | Accidental Commander

Unity·C#으로 개발한 **모바일 파티 서바이버**의 게임 코드와 테스트입니다. 전투 중 카드 선택으로 군단원을 모집·강화·진급시키며 보스에 도전합니다.

| 항목 | 내용 |
| --- | --- |
| 구성 | 2인 협업 프로젝트, 개발자 1명 |
| 기간·상태 | 2026년 7월 시작 · Android 프로토타입 제작 후 개발 중단 |
| 담당 | 이동준 — Unity/C# 개발 전담. 초기 기획을 바탕으로 프로토타입을 구현하고, 이후 게임 상세 설계·구현·테스트를 담당 |
| 협업 | 협업자는 초기 기획과 방향성 제안·검토를 담당 |
| 주요 기술 | Unity 6, C#, UniTask, Addressables, Unity Test Framework |

## 먼저 볼 코드

| 관심 주제 | 코드 | 확인할 내용 |
| --- | --- | --- |
| 근접 군단의 공격 주기 | [CompanionSquadActionCycle](Assets/_LizzoPV/Gameplay/Legion/Runtime/CompanionRuntime/CompanionSquadActionCycle.cs) | 타겟 확정, 접근, 공격, 회복·복귀 상태와 시간 처리 |
| 군단의 생성과 진행 | [CompanionRuntime 디렉터리](Assets/_LizzoPV/Gameplay/Legion/Runtime/CompanionRuntime) | 모집·강화·진급, 군단 상태와 전투 실행의 연결 |
| 행동 규칙 테스트 | [CompanionRunModuleS3Tests](Assets/_LizzoPV/Editor/Tests/EditMode/CompanionRunModuleS3Tests.cs) | 진급한 군단의 순차 공격, 공통 타겟 유지, 지휘관 이동 후 최신 대형 위치로 복귀하는 조건 |

### 대표 설계: 타겟과 복귀 위치의 분리

군단원이 공격하러 이동하는 동안 지휘관도 움직일 수 있습니다. 공격 대상과 복귀 위치를 구분하지 않으면 타겟이 도중에 바뀌거나 이전 대형 위치로 돌아가는 문제가 생깁니다.

공격 시작 시 타겟을 확정하고, 복귀할 대형 위치는 별도로 갱신하도록 구성했습니다. 행동 규칙 테스트에는 **타겟 유지·최신 대형 위치로 복귀·진급한 군단의 순차 공격**을 확인하는 사례를 담았습니다.

## AI 활용과 기여 범위

생성형 AI를 개발 환경에 연동해 구현과 리팩터링에 적극 활용했습니다. 상세 게임 규칙·요구사항·상태 전이와 검증 기준을 정하고, 코드·테스트·실행 결과를 검토하는 역할을 담당했습니다.

## 코드 열람 안내

- `main`은 프로토타입 이후 리팩터링한 코드입니다. [8월 17일 Android 프로토타입 코드](https://github.com/DLE-L/AccidentalCommander-Portfolio/tree/prototype-20260817)도 별도로 볼 수 있습니다.
- 에셋·Scene·Prefab·게임 데이터를 제외한 **코드 열람용 저장소**로, 전체 게임이나 테스트를 단독 실행할 수 없습니다.
