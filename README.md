# DeepSea Jewels: Match3

<a href="https://littlebigcha.itch.io/undertheseamatch3" target="_blank" rel="noopener noreferrer"> itch.io에서 지금 플레이하기 (WebGL)</a>

이 프로젝트는 Unity로 개발된 기능 완성형 매치3 게임입니다. 핵심 기능 구현과 더불어 연쇄 반응 큐와 같은 알고리즘의 안정적인 처리와 데이터 기반 확장성을 고려하여 설계되었습니다.

# 🎮 **게임 플레이**

![Image](https://github.com/user-attachments/assets/b4424758-4367-423a-b9a2-427c5f157909)

# ✨ **핵심 기능**

*   **스와이프 & 매치**: 새로운 Unity Input System을 사용한 부드러운 스와이프 조작.
*   **Flood Fill 매치**: 선 매치뿐만 아니라 3개 이상 연결된 모든 일반 젬(십자가, L자 등)을 찾는 Flood Fill (BFS) 알고리즘 적용.
*   **특수 젬 생성**: 4개 이상의 일반 젬이 매치되면 클릭 위치 또는 연쇄 반응 위치에 가로/세로 특수 젬이 생성됩니다.
*   **특수 젬 발동**: 생성된 특수 젬을 스와이프하면 해당 가로/세로줄이 모두 파괴됩니다.
*   **연쇄 반응**: 특수 젬의 폭발이 또 다른 특수 젬을 터뜨릴 경우 모든 특수 젬이 연쇄적으로 발동합니다.
*   **다이나믹 보드**: 마스터 젬 프리팹 중 6종만 랜덤으로 선택되어 매 게임마다 새로운 경험을 제공합니다.
*   **힌트 시스템**: 힌트 버튼 클릭 시 알고리즘이 찾아낸 유효한 매치 중 하나를 랜덤으로 시각화합니다.
*   **보드 셔플**: 더 이상 매치할 수 있는 젬이 없으면(NoMoreMoves) 보드가 자동으로 섞입니다.
*   **게임 관리**: 이동 횟수(Moves) 차감, 점수 계산, PlayerPrefs를 이용한 최고 점수 저장 및 게임 오버 처리를 관리합니다.

# 🌟 **핵심 아키텍처 및 기술 구현**

이 프로젝트는 실제 라이브 서비스에서 요구되는 고도화된 아키텍처와 알고리즘을 구현한 결과물입니다.

1.  **정교한 특수 젬 처리: 연쇄 반응 큐(Queue)**

    `GridManager.cs`의 `ProcessMatches` 함수는 이 프로젝트의 핵심 로직입니다.

    *   **문제**: 특수 젬(A)이 다른 특수 젬(B)을 터뜨릴 때 (B)도 발동해야 합니다. 만약 (B)의 폭발이 또 다른 특수 젬(C)을 터뜨린다면 (C)도 발동해야 합니다.
    *   **해결**: 이 복잡한 연쇄 반응(Chain Reaction)을 처리하기 위해 `Queue<Gem> gemsToActivate` (활성화 대기열)을 구현했습니다.
        *   스와이프된 특수 젬과 일반 매치로 터진 특수 젬을 큐에 넣습니다.
        *   큐가 빌 때까지 `while` 루프를 돕니다.
        *   큐에서 젬(A)을 꺼내 발동시키고 그로 인해 파괴된 젬 목록을 받습니다.
        *   이 목록에서 새로운 특수 젬(B, C)이 발견되면 즉시 `gemsToActivate` 큐에 다시 추가합니다.
    *   **의의**: 이 큐(Queue) 기반 접근 방식은 [가로] -> [세로] -> [가로]처럼 복잡하게 얽힌 연쇄 폭발도 깊이에 상관없이 모두 정확하고 순차적으로 처리할 수 있는 안정적이고 확장 가능한 알고리즘입니다.

2.  **Flood Fill (BFS) 매치 알고리즘**

    선 매치만 검사하는 대신 `FindConnectedGroup` (BFS) 알고리즘을 사용합니다.

    *   **작동**: `specialType == None` (일반 젬)인 젬을 기준으로 상하좌우로 인접한 같은 `typeIndex`의 젬을 모두 탐색하여 하나의 그룹으로 묶습니다.
    *   **의의**: 십자가(†) 모양, L자(L) 모양 등 3개 이상 연결된 모든 형태의 매치를 정확하게 찾아내어 `FindAllMatches`가 이를 반환합니다.

3.  **힌트 시스템**

    `HintManager.cs`는 런타임 성능 저하를 유발하는 리플렉션(Reflection)을 사용하지 않는 고성능 힌트 탐색 로직을 갖추고 있습니다.

    *   **'NoMoreMoves' 감지**: `FindAllValidMoves` 함수는 단순히 힌트를 찾는 것을 넘어 보드 전체를 시뮬레이션하여 유효한 스와이프가 0개일 경우 `GameManager`에 `NoMoreMoves()` 신호를 보내 보드 셔플을 유도합니다.
    *   **시뮬레이션**: `TestSwapForMatch` 함수는 젬의 `transform`을 실제로 움직이지 않고 두 일반 젬의 `typeIndex` 데이터만 임시로 교환한 뒤 `CheckForLinearMatch`를 호출하여 매치 발생 여부를 예측합니다.
    *   **특수 젬 인식**: 힌트 탐색 시 일반 젬 3매치 뿐만 아니라 특수 젬 스와이프 자체도 유효한 움직임으로 정확하게 인식합니다.

4.  **데이터 기반(Prefab-Driven) 설계**

    이 프로젝트는 특정 색상이나 스프라이트에 종속되지 않습니다.

    *   **프리팹 배열**: `GridManager`는 `Color[]`가 아닌 `GameObject[] gemPrefabs` (젬 프리팹 마스터 리스트)를 참조합니다.
    *   **다이나믹 스폰**: `SelectActiveGems` 함수는 게임 시작 시 마스터 프리팹 중 6개만 랜덤으로 `activeGemPrefabs` 리스트에 담습니다. 모든 젬 생성 로직은 이 활성 리스트를 참조합니다.
    *   **확장성**: 젬의 종류를 100개로 늘리거나 젬마다 다른 로직(예: 얼음 젬)을 `Gem.cs`에 추가해도 `GridManager`의 핵심 로직을 수정할 필요가 없는 확장 가능한 구조입니다.

5.  **모던 Unity 입력: New Input System**

    레거시 `OnMouseDown` 대신 Unity의 새로운 InputSystem 패키지를 사용했습니다.

    *   `InputManager.cs`는 `InputAction`을 사용하여 마우스 클릭(`leftButton`)과 모바일 터치(`primaryTouch`)를 동일한 로직으로 처리합니다.
    *   `HandleInput` 함수는 `Camera.main.ScreenToWorldPoint`와 `Physics2D.Raycast`를 통해 정확한 `Gem` 오브젝트를 찾아내어 `GridManager.SwapGems`를 호출합니다.

# 🛠️ **주요 코드 구조 (관심사 분리)**

*   **`GameManager.cs`**: 게임의 상태(점수, 이동 횟수, 게임 오버)와 UI, PlayerPrefs를 관리합니다.
*   **`GridManager.cs`**: 게임의 핵심 두뇌입니다. 모든 젬 데이터(`grid[,]`), 매치 알고리즘(BFS), 연쇄 반응(Queue), 셔플 로직을 담당합니다.
*   **`Gem.cs`**: 데이터 객체입니다. 자신의 좌표(`x`, `y`), 타입(`typeIndex`), 특수 젬 여부(`specialType`)만 알고 있습니다.
*   **`InputManager.cs`**: 입력만 전담합니다. 사용자의 스와이프를 감지하여 `GridManager`에 젬 교환을 요청합니다.
*   **`HintManager.cs`**: 알고리즘을 담당합니다. 보드를 시뮬레이션하여 유효한 움직임을 찾고 'NoMoreMoves' 상태를 감지합니다.

# 🚀 **실행 방법**

1.  Unity 2022.3.x 이상 버전에서 이 프로젝트를 엽니다.
2.  InputSystem 패키지가 `Packages/manifest.json`을 통해 설치되었는지 확인합니다. (설치가 안 되어 있다면 Package Manager에서 설치)
3.  `Assets/Scenes/My3Match.unity` 씬을 엽니다.
4.  Play 버튼을 눌러 게임을 실행합니다.
