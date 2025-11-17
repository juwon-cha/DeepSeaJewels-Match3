using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 매치3 게임의 핵심 로직(그리드, 매치, 리필) 관리
/// </summary>
public class GridManager : MonoBehaviour
{
    // 셔플 시 특수 젬 데이터를 임시 저장하기 위한 구조체
    private struct PreservedGemData
    {
        public int typeIndex;
        public Gem.SpecialGemType specialType;

        public PreservedGemData(int type, Gem.SpecialGemType special)
        {
            typeIndex = type;
            specialType = special;
        }
    }

    [Header("그리드 설정")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1.0f;
    public Vector2 gridOffset;

    [Header("젬 프리팹")]
    public GameObject[] gemPrefabs;

    [Header("연결")]
    public InputManager inputManager;
    public HintManager hintManager;
    public GameManager gameManager;

    [Header("특수 젬 프리팹 (필수)")]
    public GameObject horizontalGemPrefab; // 가로줄 젬 프리팹
    public GameObject verticalGemPrefab;   // 세로줄 젬 프리팹

    private Gem[,] grid;
    private bool isProcessing = false;
    private List<GameObject> activeGemPrefabs;

    void Start()
    {
        if (gemPrefabs == null || gemPrefabs.Length < 6)
        {
            Debug.LogError("[GridManager] gemPrefabs 배열에 최소 6개 이상의 젬 프리팹이 할당되어야 합니다.");
            return;
        }
        if (horizontalGemPrefab == null || verticalGemPrefab == null)
        {
            Debug.LogError("[GridManager] 특수 젬 프리팹(Horizontal/Vertical)이 할당되지 않았습니다!");
            return;
        }
        if (inputManager == null)
        {
            Debug.LogWarning("[GridManager] InputManager가 연결되지 않았습니다.");
        }
        if (gameManager == null)
        {
            Debug.LogWarning("[GridManager] GameManager가 연결되지 않았습니다.");
        }

        if (hintManager != null)
        {
            if (gameManager != null)
            {
                hintManager.gameManager = this.gameManager;
            }
            else
            {
                Debug.LogWarning("[GridManager] GameManager가 연결되지 않아 'No More Moves' 감지가 비활성화됩니다.");
            }
        }
        else
        {
            Debug.LogWarning("[GridManager] HintManager가 연결되지 않았습니다.");
        }

        SelectActiveGems();

        grid = new Gem[width, height];
        InitializeBoard();
    }

    /// <summary>
    /// gemPrefabs (마스터 리스트)에서 6개 랜덤으로 선택
    /// </summary>
    void SelectActiveGems()
    {
        activeGemPrefabs = new List<GameObject>();
        List<GameObject> tempMasterList = new List<GameObject>(gemPrefabs);

        for (int i = 0; i < 6; i++)
        {
            int randomIndex = Random.Range(0, tempMasterList.Count);
            GameObject chosenPrefab = tempMasterList[randomIndex];

            activeGemPrefabs.Add(chosenPrefab);
            tempMasterList.RemoveAt(randomIndex);
        }
    }


    /// <summary>
    /// 게임 보드 초기화
    /// </summary>
    void InitializeBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateGem(x, y);
            }
        }

        // 초기 매치 방지 (Flood Fill 기준)
        while (FindAllMatches().Count > 0)
        {
            ClearBoard();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CreateGem(x, y);
                }
            }
        }

        if (hintManager != null)
        {
            // 힌트 찾기 및 NoMoreMoves 검사 시작
            hintManager.FindAllValidMoves();
        }
    }

    /// <summary>
    /// 특정 위치에 젬 생성 (프리팹 배열에서 스폰)
    /// </summary>
    void CreateGem(int x, int y)
    {
        int randomTypeIndex = GetRandomGemType(x, y);
        GameObject prefabToSpawn = activeGemPrefabs[randomTypeIndex];

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[GridManager] activeGemPrefabs 배열의 {randomTypeIndex}번 인덱스가 비어있습니다(None).");
            return;
        }

        GameObject newGemObj = Instantiate(prefabToSpawn, GetWorldPosition(x, y), Quaternion.identity, this.transform);
        Gem newGem = newGemObj.GetComponent<Gem>();

        if (newGem == null)
        {
            Debug.LogError($"[GridManager] '{prefabToSpawn.name}' 프리팹에 Gem.cs 스크립트가 없습니다. 프리팹을 확인하세요.");
            Destroy(newGemObj);
            return;
        }

        newGem.Initialize(randomTypeIndex, x, y, this);
        grid[x, y] = newGem;
    }

    /// <summary>
    /// 랜덤 젬 타입 인덱스 반환
    /// </summary>
    int GetRandomGemType(int x, int y)
    {
        int randomTypeIndex;
        do
        {
            randomTypeIndex = Random.Range(0, activeGemPrefabs.Count);

        } while ((x > 1 && grid[x - 1, y] != null && grid[x - 1, y].typeIndex == randomTypeIndex && grid[x - 2, y] != null && grid[x - 2, y].typeIndex == randomTypeIndex) ||
                 (y > 1 && grid[x, y - 1] != null && grid[x, y - 1].typeIndex == randomTypeIndex && grid[x, y - 2] != null && grid[x, y - 2].typeIndex == randomTypeIndex));

        return randomTypeIndex;
    }

    public Vector2 GetWorldPosition(int x, int y)
    {
        return new Vector2(x * cellSize, y * cellSize) + gridOffset;
    }

    void ClearBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
        }
    }

    public void SwapGems(Gem gem1, Gem gem2)
    {
        if (isProcessing || (gameManager != null && gameManager.IsGameOver()))
        {
            return;
        }

        StartCoroutine(SwapGemsCoroutine(gem1, gem2));
    }

    /// <summary>
    /// (스와이프 시 특수 젬과 일반 매치를 함께 처리하도록 수정
    /// </summary>
    IEnumerator SwapGemsCoroutine(Gem gem1, Gem gem2)
    {
        isProcessing = true;

        int gem1X = gem1.x;
        int gem1Y = gem1.y;
        int gem2X = gem2.x;
        int gem2Y = gem2.y;

        grid[gem1X, gem1Y] = gem2;
        grid[gem2X, gem2Y] = gem1;

        gem1.MoveTo(gem2X, gem2Y);
        gem2.MoveTo(gem1X, gem1Y);

        yield return new WaitForSeconds(0.3f);

        // 스와이프로 새로 생긴 일반 매치 목록
        List<Gem> allFoundMatches = FindAllMatches();

        // 스와이프에 관여한 특수 젬 목록
        List<Gem> specialGemsSwiped = new List<Gem>();
        if (gem1.specialType != Gem.SpecialGemType.None)
        {
            specialGemsSwiped.Add(gem1);
        }
        if (gem2.specialType != Gem.SpecialGemType.None)
        {
            specialGemsSwiped.Add(gem2);
        }

        // 두 목록을 합쳐서 처리할 젬 목록을 만듦
        List<Gem> gemsToProcess = new List<Gem>(allFoundMatches);
        gemsToProcess.AddRange(specialGemsSwiped);
        gemsToProcess = gemsToProcess.Distinct().ToList(); // 중복 제거

        if (gemsToProcess.Count > 0)
        {
            if (gameManager != null) gameManager.DecreaseMoves();
            yield return StartCoroutine(ProcessMatches(gemsToProcess, gem1));
        }
        else
        {
            // 젬을 원위치로 되돌림
            grid[gem1X, gem1Y] = gem1;
            grid[gem2X, gem2Y] = gem2;

            gem1.MoveTo(gem1X, gem1Y);
            gem2.MoveTo(gem2X, gem2Y);

            yield return new WaitForSeconds(0.3f);
        }

        if (gameManager != null)
        {
            gameManager.CheckForGameOver();
        }

        isProcessing = false;

        if (hintManager != null && (gameManager != null && !gameManager.IsGameOver()))
        {
            hintManager.FindAllValidMoves();
        }
    }

    /// <summary>
    /// 3개 이상 연결된 모든 그룹을 찾음 (Flood Fill - BFS)
    /// </summary>
    List<Gem> FindAllMatches()
    {
        HashSet<Gem> allMatchesSet = new HashSet<Gem>();
        bool[,] visited = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null && !visited[x, y])
                {
                    List<Gem> group = FindConnectedGroup(x, y, visited);
                    if (group.Count >= 3)
                    {
                        allMatchesSet.UnionWith(group);
                    }
                }
            }
        }

        return allMatchesSet.ToList();
    }

    /// <summary>
    /// (x, y)에서 시작하여 인접한 동일 타입의 일반 젬 그룹을 BFS로 찾음
    /// </summary>
    List<Gem> FindConnectedGroup(int startX, int startY, bool[,] visited)
    {
        List<Gem> group = new List<Gem>();
        Queue<Gem> queue = new Queue<Gem>();

        Gem startGem = grid[startX, startY];
        if (startGem == null)
        {
            return group;
        }

        // 일반 젬이 아닌 경우 그룹(0)을 반환
        if (startGem.specialType != Gem.SpecialGemType.None)
        {
            return group;
        }

        queue.Enqueue(startGem);
        visited[startX, startY] = true;
        int targetType = startGem.typeIndex;

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            Gem currentGem = queue.Dequeue();
            group.Add(currentGem);

            for (int i = 0; i < 4; i++)
            {
                int nx = currentGem.x + dx[i];
                int ny = currentGem.y + dy[i];

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;
                if (visited[nx, ny])
                    continue;

                Gem neighbor = grid[nx, ny];

                // 같은 타입의 일반 젬만 그룹에 포함
                if (neighbor != null &&
                    neighbor.typeIndex == targetType &&
                    neighbor.specialType == Gem.SpecialGemType.None)
                {
                    visited[nx, ny] = true;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return group;
    }


    /// <summary>
    /// 매치 처리 및 연쇄 반응 코루틴
    /// </summary>
    IEnumerator ProcessMatches(List<Gem> initialMatches, Gem clickedGem = null)
    {
        // initialMatches는 스와이프로 발생한 일반 매치 + 스와이프된 특수 젬
        List<Gem> gemsToProcess = new List<Gem>(initialMatches);
        bool firstIteration = true;

        // 연쇄 매치 루프
        while (gemsToProcess.Count > 0)
        {
            // 이 턴에 파괴될 젬과 활성화될 젬 리스트 비움
            HashSet<Gem> gemsToDestroy = new HashSet<Gem>();
            Queue<Gem> gemsToActivate = new Queue<Gem>();

            // gemsToProcess를 올바르게 분배
            foreach (Gem g in gemsToProcess)
            {
                if (g.specialType != Gem.SpecialGemType.None)
                {
                    // 특수 젬 -> 활성화 큐로
                    if (!gemsToActivate.Contains(g))
                    {
                        gemsToActivate.Enqueue(g);
                    }
                }
                else
                {
                    // 일반 젬 -> 즉시 파괴 목록으로
                    gemsToDestroy.Add(g);
                }
            }

            // 연쇄 반응 시작
            while (gemsToActivate.Count > 0)
            {
                Gem gemToActivate = gemsToActivate.Dequeue();

                // 큐에서 꺼낸 젬을 파괴 목록에 추가
                // Add()가 false를 반환하면 (이미 목록에 있으면) 무한 루프를 방지하기 위해 건너뜀
                if (!gemsToDestroy.Add(gemToActivate))
                {
                    continue; // 이미 이 턴에 처리된 젬(무한 루프 방지)
                }

                // 특수 젬 1개 활성화
                List<Gem> newlyDestroyedGems = ActivateSpecialGem(gemToActivate);

                // 새로 파괴된 젬들 검사
                foreach (Gem newGem in newlyDestroyedGems)
                {
                    // newGem이 null이 아니고 이미 파괴 목록에 있는 젬도 아니어야 함
                    if (newGem != null && !gemsToDestroy.Contains(newGem))
                    {
                        // 만약 새로 터진 젬이 또 다른 특수 젬이라면 큐에 추가
                        if (newGem.specialType != Gem.SpecialGemType.None)
                        {
                            if (!gemsToActivate.Contains(newGem))
                                gemsToActivate.Enqueue(newGem);
                        }
                        // 일반 젬이라면 파괴 목록에 추가
                        else
                        {
                            gemsToDestroy.Add(newGem);
                        }
                    }
                }
            }
            // 연쇄 반응 종료

            // 특수 젬 생성 조건 검사
            Gem gemToTransform = null;

            bool[,] visited = new bool[width, height];
            List<List<Gem>> allGroupsInThisCascade = new List<List<Gem>>();
            foreach (Gem gem in gemsToProcess) // 초기 매치 젬들만 검사
            {
                if (gem != null && grid[gem.x, gem.y] != null && !visited[gem.x, gem.y] &&
                    gem.specialType == Gem.SpecialGemType.None)
                {
                    List<Gem> group = FindConnectedGroup(gem.x, gem.y, visited);
                    allGroupsInThisCascade.Add(group);
                }
            }

            // 클릭한 젬이 4+ 그룹에 속하는지(첫 턴 우선권)
            if (firstIteration && clickedGem != null)
            {
                foreach (var group in allGroupsInThisCascade)
                {
                    if (group.Count >= 4 && group.Contains(clickedGem) && clickedGem.specialType == Gem.SpecialGemType.None)
                    {
                        gemToTransform = clickedGem; break;
                    }
                }
            }
            // 연쇄 반응 중 4+ 그룹이 생겼는지
            if (gemToTransform == null)
            {
                foreach (var group in allGroupsInThisCascade)
                {
                    if (group.Count >= 4)
                    {
                        gemToTransform = group.FirstOrDefault(g => g.specialType == Gem.SpecialGemType.None);
                        if (gemToTransform != null)
                        {
                            break;
                        }
                    }
                }
            }
            firstIteration = false;

            // 젬을 파괴하고 새 특수 젬 프리팹으로 교체
            if (gemToTransform != null)
            {
                gemsToDestroy.Remove(gemToTransform);

                int x = gemToTransform.x;
                int y = gemToTransform.y;
                int typeIndex = gemToTransform.typeIndex;

                Destroy(gemToTransform.gameObject);

                Gem.SpecialGemType newType = (Random.Range(0, 2) == 0) ? Gem.SpecialGemType.HorizontalLine : Gem.SpecialGemType.VerticalLine;
                GameObject prefabToSpawn = (newType == Gem.SpecialGemType.HorizontalLine) ? horizontalGemPrefab : verticalGemPrefab;
                GameObject newGemObj = Instantiate(prefabToSpawn, GetWorldPosition(x, y), Quaternion.identity, this.transform);
                Gem newGem = newGemObj.GetComponent<Gem>();

                newGem.Initialize(typeIndex, x, y, this);
                grid[x, y] = newGem;
            }

            // 최종 파괴 리스트 정리 및 보드 리필
            yield return StartCoroutine(ClearAndRefill(gemsToDestroy.ToList()));

            // 다음 연쇄 반응 검사
            gemsToProcess = FindAllMatches();
        }
    }

    /// <summary>
    /// 하나의 특수 젬을 활성화하고 그로 인해 파괴될 젬 목록 반환
    /// </summary>
    private List<Gem> ActivateSpecialGem(Gem gem)
    {
        List<Gem> gemsToDestroy = new List<Gem>();

        if (gem == null)
        {
            return gemsToDestroy;
        }

        Debug.Log("Activating Special Gem at: (" + gem.x + "," + gem.y + ")");

        if (gem.specialType == Gem.SpecialGemType.HorizontalLine)
        {
            for (int x = 0; x < width; x++)
            {
                Gem target = GetGemAt(x, gem.y);
                if (target != null)
                {
                    gemsToDestroy.Add(target);
                }
            }
        }
        else if (gem.specialType == Gem.SpecialGemType.VerticalLine)
        {
            for (int y = 0; y < height; y++)
            {
                Gem target = GetGemAt(gem.x, y);
                if (target != null)
                {
                    gemsToDestroy.Add(target);
                }
            }
        }

        return gemsToDestroy;
    }

    /// <summary>
    /// 매치된 젬 제거 및 보드 리필
    /// </summary>
    IEnumerator ClearAndRefill(List<Gem> matches)
    {
        int scoreToAdd = 0;
        foreach (Gem gem in matches)
        {
            if (gem != null && grid[gem.x, gem.y] != null)
            {
                grid[gem.x, gem.y] = null;
                Destroy(gem.gameObject);
                scoreToAdd += 10;
            }
        }

        if (gameManager != null && scoreToAdd > 0)
        {
            gameManager.AddScore(scoreToAdd);
        }

        for (int x = 0; x < width; x++)
        {
            int emptySpotY = -1;
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null && emptySpotY == -1)
                {
                    emptySpotY = y;
                }

                if (grid[x, y] != null && emptySpotY != -1)
                {
                    Gem gemToMove = grid[x, y];
                    grid[x, y] = null;
                    grid[x, emptySpotY] = gemToMove;
                    gemToMove.MoveTo(x, emptySpotY);
                    emptySpotY++;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    Vector2 startPos = GetWorldPosition(x, height + Random.Range(1, 4));

                    int randomTypeIndex = GetRandomGemType(x, y);
                    GameObject prefabToSpawn = activeGemPrefabs[randomTypeIndex];

                    if (prefabToSpawn == null)
                    {
                        Debug.LogError($"[GridManager] ClearAndRefill: activeGemPrefabs 배열의 {randomTypeIndex}번 인덱스가 비어있습니다(None).");
                        continue;
                    }

                    GameObject newGemObj = Instantiate(prefabToSpawn, startPos, Quaternion.identity, this.transform);
                    Gem newGem = newGemObj.GetComponent<Gem>();

                    if (newGem == null)
                    {
                        Debug.LogError($"[GridManager] ClearAndRefill: '{prefabToSpawn.name}' 프리팹에 Gem.cs 스크립트가 없습니다.");
                        Destroy(newGemObj);
                        continue;
                    }

                    newGem.Initialize(randomTypeIndex, x, y, this);

                    grid[x, y] = newGem;
                    newGem.MoveTo(x, y);
                }
            }
        }

        yield return StartCoroutine(WaitForGemsToSettle());
    }

    /// <summary>
    /// 모든 젬이 시각적으로 이동을 멈출 때까지 대기하는 코루틴
    /// </summary>
    private IEnumerator WaitForGemsToSettle()
    {
        while (true)
        {
            bool allSettled = true;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null && grid[x, y].IsMoving())
                    {
                        allSettled = false;
                        break;
                    }
                }
                if (!allSettled) break;
            }

            if (allSettled)
            {
                break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// GameManager에 의해 호출되는 보드 섞기 진입점
    /// </summary>
    public void ShuffleBoard()
    {
        if (isProcessing) return;
        StartCoroutine(ShuffleBoardCoroutine());
    }

    /// <summary>
    /// 보드를 섞는 실제 로직을 처리하는 코루틴
    /// </summary>
    private IEnumerator ShuffleBoardCoroutine()
    {
        isProcessing = true;
        Debug.Log("Shuffling board...");

        if (gameManager != null && gameManager.shufflingText != null)
        {
            gameManager.shufflingText.gameObject.SetActive(true);
        }

        // 기존 특수 젬 데이터 백업
        List<PreservedGemData> preservedGems = new List<PreservedGemData>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null && grid[x, y].specialType != Gem.SpecialGemType.None)
                {
                    // 젬의 typeIndex와 특수 타입 저장
                    preservedGems.Add(new PreservedGemData(grid[x, y].typeIndex, grid[x, y].specialType));
                }
            }
        }

        ClearBoard();
        yield return new WaitForSeconds(0.2f);

        // 매치 없는 일반 젬 보드 생성
        while (true)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CreateGem(x, y); // 일반 젬 생성
                }
            }

            if (FindAllMatches().Count == 0)
            {
                break; // 매치가 없으면 성공
            }

            Debug.Log("Shuffle resulted in initial matches. Retrying...");
            ClearBoard();
            yield return new WaitForSeconds(0.2f);
        }

        // 백업한 특수 젬을 랜덤 위치에 복원
        if (preservedGems.Count > 0)
        {
            // 모든 위치 리스트 생성
            List<Vector2Int> allPositions = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    allPositions.Add(new Vector2Int(x, y));
                }
            }

            // 위치 리스트 셔플 (Fisher-Yates Shuffle)
            for (int i = 0; i < allPositions.Count; i++)
            {
                int randomIndex = Random.Range(i, allPositions.Count);
                Vector2Int temp = allPositions[i];
                allPositions[i] = allPositions[randomIndex];
                allPositions[randomIndex] = temp;
            }

            // 백업한 젬 개수만큼 랜덤 위치에 주입
            for (int i = 0; i < preservedGems.Count && i < allPositions.Count; i++)
            {
                PreservedGemData data = preservedGems[i];
                Vector2Int pos = allPositions[i]; // 셔플된 랜덤 위치

                // 해당 위치의 일반 젬 파괴
                Gem gemToReplace = grid[pos.x, pos.y];
                Vector2 worldPos = gemToReplace.transform.position; // 위치 기억
                Destroy(gemToReplace.gameObject);

                // 어떤 특수 젬 프리팹을 스폰할지 결정
                GameObject prefabToSpawn = (data.specialType == Gem.SpecialGemType.HorizontalLine)
                    ? horizontalGemPrefab
                    : verticalGemPrefab;

                // 새 특수 젬 생성
                GameObject newGemObj = Instantiate(prefabToSpawn, worldPos, Quaternion.identity, this.transform);
                Gem newGem = newGemObj.GetComponent<Gem>();

                // 백업한 데이터(색상, 위치)로 초기화
                newGem.Initialize(data.typeIndex, pos.x, pos.y, this);
                grid[pos.x, pos.y] = newGem;
            }
        }

        if (gameManager != null && gameManager.shufflingText != null)
        {
            gameManager.shufflingText.gameObject.SetActive(false);
        }

        if (hintManager != null)
        {
            hintManager.FindAllValidMoves();
        }

        Debug.Log("Shuffle complete.");
        isProcessing = false;
    }


    /// <summary>
    /// HintManager가 그리드 데이터를 안전하게 읽음
    /// </summary>
    public Gem GetGemAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return null;
        }
        return grid[x, y];
    }

    /// <summary>
    /// 현재 로직 처리 중인지 반환
    /// </summary>
    public bool IsProcessing()
    {
        return isProcessing;
    }
}
