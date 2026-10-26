# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity **6000.3.22f1** (URP 17.3) 기반 3D FPS 학습용 게임 프로젝트. 씬은 `Assets/01.Scenes/GameScene.unity` 하나이며, 게임 코드는 전부 `Assets/02.Scripts/` 아래에 있다 (asmdef 없이 `Assembly-CSharp` 어셈블리로 컴파일됨). `Assets/02.Scripts/Example/`은 C# 문법 연습용 코드로 게임 로직과 무관하다.

## 작업 지침

- 답변은 한국어로 작성한다.
- 요청받지 않았다면 직접 코드를 변경하거나 커밋하지 않는다. 요청과 관련 없는 파일은 수정하지 않는다.
- Unity 에디터에서 실행 결과를 확인하지 않았다면 "실행했다/동작한다"고 표현하지 않는다. (CLI에서 Unity 빌드·플레이 테스트는 불가)
- 코드 리뷰 시에는 `Docs/CodeReviewGuide.md`의 기준(Critical/High/Medium/Low 중요도, 매직 넘버, 묻지 말고 시켜라, SOLID, 과도한 추상화 지양 등)을 따른다.
- `.unity`, `.prefab`, `.mat`, `.meta` 파일은 Unity가 관리하는 YAML이므로 직접 편집하지 않는다. 스크립트 파일을 추가/이동하면 `.meta`는 Unity가 생성한다.

## 코드 스타일 (`.editorconfig`, `Assets/02.Scripts/**.cs`에만 적용)

- 들여쓰기 4칸, 중괄호는 Allman 스타일(새 줄).
- private 필드 `_camelCase`, private const / private static readonly는 `PascalCase`, 지역변수 `camelCase`, 타입 이름에 `_` 금지 (단, 기존 `UI_HitEffect`, `UI_Minimap`처럼 `UI_` 접두 클래스가 존재).
- 단일 행 조기 리턴(`if (...) return;`)은 중괄호 없이 허용.
- Inspector 노출 값은 `[SerializeField] private` 필드로 선언한다.
- 주석은 한국어로, 필요한 곳에만 짧게 단다.

## 커밋 규칙

Husky.Net(`dotnet-tools.json`, `.husky/commit-msg`)이 커밋 메시지 형식을 강제한다:

```
<type>(<scope>)?: <설명>
type: asset | build | chore | ci | docs | feat | fix | perf | refactor | revert | style | test
```

기존 커밋은 `feat: 미니맵 카메라 확대 축소`처럼 설명을 한국어로 쓴다. 훅 설치: `dotnet tool restore && dotnet husky install`.

## CI

`.github/workflows/gemini-commit-review.yml`: `main`에 `Assets/02.Scripts/**/*.cs` 변경이 push되면 Gemini가 커밋 diff를 `Docs/CodeReviewGuide.md` 기준으로 리뷰한다. 빌드/테스트 CI는 없다.

## 아키텍처

### 게임 상태 게이트
`GameManager`(싱글톤, `GameManager.Instance`)가 `GameState`(Ready → Playing → GameOver)를 관리한다. 시작 시 "준비/시작" 코루틴 후 `Playing`으로 전환된다. 입력을 받는 거의 모든 컴포넌트(`PlayerMove`, `PlayerRotate`, `PlayerGunFire`, `PlayerBombFire`, `CameraRotate`, `CameraViewSwitcher`, `CursorManager`, `Zombie`)가 `Update` 첫 줄에서 `if (GameManager.Instance.State != GameState.Playing) return;`으로 동작을 막는다. 새 입력/AI 컴포넌트도 이 패턴을 따른다.

### 대미지 시스템
`Core/IDamageable.TakeDamage(DamageInfo)` 인터페이스로 통일되어 있다. `DamageInfo`는 값(`Value`), 맞은 위치(`HitPoint`), 법선(`HitNormal`)을 담는 struct. 공격 측(`PlayerGunFire` 레이캐스트, `Bomb` 폭발, `Zombie` 공격)은 대상의 구체 타입 대신 `IDamageable`을 찾아 호출하고, 피격 측(`Player`, `Zombie`, `Box`, `Drum`)이 각자 반응한다. 플레이어 체력이 0이 되면 `Player`가 `GameManager.GameOver()`를 호출한다.

### 기타 구조
- **입력**: Active Input Handling = Both. 현재 코드는 레거시 `Input.GetAxis/GetKeyDown/GetButton`을 사용한다 (`InputSystem_Actions.inputactions`는 존재하지만 미사용).
- **커서**: `CursorManager`가 커서를 잠그고, LeftAlt로 해제 / Esc로 다시 잠근다. 커서가 풀린 동안 총이 발사되지 않도록 처리되어 있다.
- **수류탄**: `BombPool`이 `Queue<Bomb>` 기반 직접 구현 오브젝트 풀. `Bomb`은 풀 참조를 받아 스스로 반환한다. (`Plugins/CW`의 LeanPool은 플러그인으로만 포함되어 있고 게임 코드에서 사용하지 않음)
- **파괴 오브젝트**: `Box`는 OpenFracture 패키지(`Fragmenter.Fracture`)로 메시를 잘라 파편을 생성한다.
- **카메라**: `CameraViewSwitcher`가 여러 시점(1/3인칭 등)을 순환 전환. 미니맵은 `MinimapCamera`(플레이어 위에서 내려다보며 회전 추종) → RenderTexture → `UI_Minimap`(마스크, 확대/축소) 구성.
