# 어쩌다 군단장 | Accidental Commander

Unity·C#으로 개발한 **모바일 파티 서바이버**의 게임 코드와 테스트입니다. 전투 중 카드 선택으로 군단원을 모집·강화·진급시키며 보스에 도전합니다.

| 항목 | 내용 |
| --- | --- |
| 구성 | 2인 협업 프로젝트, 개발자 1명 |
| 기간·상태 | 2026년 7월 시작, 현재 개발 중단 |
| 담당 | 이동준 — Unity/C# 개발 전담. 초기 기획을 바탕으로 프로토타입을 구현하고, 이후 게임 상세 설계·구현·테스트를 담당 |
| 협업 | 협업자는 초기 기획과 방향성 제안·검토를 담당 |
| 주요 기술 | Unity 6000.3.19f1, C#, UniTask, Addressables, Unity Test Framework |

## 먼저 볼 코드

| 관심 주제 | 코드 | 확인할 내용 |
| --- | --- | --- |
| 근접 군단의 공격 주기 | [CompanionSquadActionCycle](Assets/_LizzoPV/Gameplay/Legion/Runtime/CompanionRuntime/CompanionSquadActionCycle.cs) | 타겟 확정, 접근, 공격, 회복·복귀 상태와 시간 처리 |
| 군단의 생성과 진행 | [CompanionRuntime 디렉터리](Assets/_LizzoPV/Gameplay/Legion/Runtime/CompanionRuntime) | 모집·강화·진급, 군단 상태와 전투 실행의 연결 |
| 행동 규칙 테스트 | [CompanionRunModuleS3Tests](Assets/_LizzoPV/Editor/Tests/EditMode/CompanionRunModuleS3Tests.cs) | 진급한 군단의 순차 공격, 공통 타겟 유지, 지휘관 이동 후 최신 대형 위치로 복귀하는 조건 |
| 그 밖의 테스트 | [EditMode 테스트](Assets/_LizzoPV/Editor/Tests/EditMode) | 카드, 공격, 상태 전이 등 기능별 검증 사례 |

### 대표 설계: 타겟과 복귀 위치의 분리

군단원이 공격하러 이동하는 동안 지휘관도 움직일 수 있습니다. 이때 공격 대상 위치와 복귀할 대형 위치를 같은 기준으로 갱신하면 공격 도중 타겟이 바뀌거나 이전 대형 위치로 돌아가는 문제가 생깁니다.

공격 시작 시 타겟 위치를 확정하고, 대형의 기준 위치는 별도로 갱신하도록 구성했습니다. `CompanionSquadActionCycle`에서 행동 단계를 관리하고, 관련 테스트에는 이동 중 타겟을 유지하면서 최신 대형 위치로 복귀하는 조건을 담았습니다. 현재 코드는 초기 프로토타입 이후 리팩터링된 버전입니다.

## 버전 선택

- **main**: 개발 중단 시점에 보관한 원본 main의 코드. 공개용 정리의 기준 원본은 `0ec0093b80d693e55860c88fcca1fef2425031c4`입니다.
- **[prototype-20260817](https://github.com/DLE-L/AccidentalCommander-Portfolio/tree/prototype-20260817)**: 2026년 8월 17일 Android 프로토타입 빌드에 대응하는 코드입니다.
- 원본에서 공개 제외 파일을 과거 이력까지 제거했기 때문에 커밋 해시는 달라졌습니다. [원본·공개 커밋 대응표](provenance/commit-map.tsv)로 개발 이력을 연결할 수 있습니다.

### 프로토타입 빌드 근거

| 항목 | 기록 |
| --- | --- |
| 원본 커밋 | `5c787e7d386f0971c99be2df0cb9f427bc735684` |
| 공개본 커밋 | `e7db4bf2e567ad23b46da65f59719f4dd7e2e433` |
| 빌드 기록 시각 | 2026-08-17 12:50 KST |
| 버전 | 0.1.0, versionCode 2 |
| 확인 근거 | Android APK와 빌드 성공 기록이 남아 있으며, APK SHA-256이 기록과 일치 |

이 근거는 8월 17일 빌드에 대한 기록입니다. 이후 리팩터링된 main 전체의 실행 결과를 의미하지 않습니다. 공개본 정리 과정에서는 코드를 수정하거나 Unity 테스트·APK 실행을 새로 수행하지 않았습니다.

## AI 활용과 기여 범위

생성형 AI를 개발 환경에 연동해 구현과 리팩터링에 적극 활용했습니다. 초기에는 협업자가 제공한 기획을 바탕으로 개발했고, 이후 역할 조정에 따라 상세 게임 규칙·요구사항·상태 전이와 검증 기준을 직접 정했습니다. AI 결과는 코드 검토, 테스트와 실행 결과를 통해 확인하며 개발을 진행했습니다. 이 저장소의 코드를 전부 수작업으로 독립 작성했다는 의미는 아닙니다.

## 저장소 범위

이 저장소는 **코드 열람용 포트폴리오**입니다. 구매 에셋과 추출 이미지, Scene·Prefab·게임 데이터, 내부 문서와 로컬 설정을 제외했습니다. 따라서 이 저장소만 내려받아 전체 게임을 실행하거나 포함된 테스트를 그대로 실행할 수는 없습니다. [Packages](Packages)와 [Unity 버전](ProjectSettings/ProjectVersion.txt)은 원래 개발 환경을 설명하기 위한 자료입니다.

개발 기록의 작성자 표시는 당시 사용한 팀 Git 계정을 유지했습니다. 공개본 소개 문서만 개인 계정으로 추가했습니다.
