using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    [Header("연결")]
    public GridManager gridManager;
    public GameManager gameManager; // 움직일 곳 없음 신호 전송용

    [Header("힌트 설정")]
    public GameObject hintParticle; // 힌트 파티클 프리팹

    private List<List<Gem>> allValidMoves; // 가능한 모든 힌트 조합 리스트
    private List<Gem> currentHint;         // 현재 시각적으로 보여줄 힌트
    private Coroutine hintCoroutine;       // 힌트 시각화 코루틴

    void Awake()
    {
        allValidMoves = new List<List<Gem>>();
    }

    /// <summary>
    /// 모든 유효한 이동을 찾아 리스트에 저장하고 움직일 곳이 없는지 검사
    /// (GridManager가 턴이 끝날 때마다 호출)
    /// </summary>
    public void FindAllValidMoves()
    {
        if (gridManager == null)
        {
            return;
        }

        allValidMoves.Clear();
        StopHintVisual(); // 턴이 시작될 때 기존 힌트 시각화 중지

        int width = gridManager.width;
        int height = gridManager.height;

        // 모든 젬을 순회하며 유효한 힌트를 allValidMoves 리스트에 추가
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem currentGem = gridManager.GetGemAt(x, y);
                if (currentGem == null) continue;

                // 오른쪽 젬과 교환 테스트
                Gem rightGem = (x < width - 1) ? gridManager.GetGemAt(x + 1, y) : null;
                if (rightGem != null)
                {
                    // 1순위: 젬 둘 중 하나라도 특수 젬이면 무조건 유효한 힌트
                    if (currentGem.specialType != Gem.SpecialGemType.None || rightGem.specialType != Gem.SpecialGemType.None)
                    {
                        allValidMoves.Add(new List<Gem> { currentGem, rightGem });
                    }
                    // 2순위: 둘 다 일반 젬이면 3매치가 되는지 검사
                    else if (currentGem.typeIndex != rightGem.typeIndex && TestSwapForMatch(currentGem, rightGem))
                    {
                        allValidMoves.Add(new List<Gem> { currentGem, rightGem });
                    }
                }

                // 위쪽 젬과 교환 테스트
                Gem upperGem = (y < height - 1) ? gridManager.GetGemAt(x, y + 1) : null;
                if (upperGem != null)
                {
                    if (currentGem.specialType != Gem.SpecialGemType.None || upperGem.specialType != Gem.SpecialGemType.None)
                    {
                        allValidMoves.Add(new List<Gem> { currentGem, upperGem });
                    }
                    else if (currentGem.typeIndex != upperGem.typeIndex && TestSwapForMatch(currentGem, upperGem))
                    {
                        allValidMoves.Add(new List<Gem> { currentGem, upperGem });
                    }
                }
            }
        }

        // 힌트 검사 후 움직일 곳이 없는지 판별
        if (allValidMoves.Count == 0)
        {
            Debug.Log("HintManager: No valid moves found!");
            if (gameManager != null && !gameManager.IsGameOver())
            {
                gameManager.NoMoreMoves(); // 움직일 곳 없음 신호 전송
            }
        }
        else
        {
            Debug.Log($"HintManager: Found {allValidMoves.Count} valid moves.");
        }
    }

    /// <summary>
    /// 두 젬의 스왑을 시뮬레이션하고 매치가 발생하는지 확인
    /// </summary>
    private bool TestSwapForMatch(Gem gem1, Gem gem2)
    {
        // 젬의 타입만 임시로 교환
        SwapGemData(gem1, gem2);

        // 두 젬의 새로운 위치에서 3-in-a-row(직선) 매치가 생기는지만 단순 체크
        bool matchFound = CheckForLinearMatch(gem1) ||
                          CheckForLinearMatch(gem2);

        // 시뮬레이션 종료 후 젬 타입을 반드시 원상복구
        SwapGemData(gem1, gem2);

        return matchFound;
    }

    /// <summary>
    /// 젬의 타입 데이터만 임시로 교환
    /// </summary>
    private void SwapGemData(Gem gem1, Gem gem2)
    {
        int tempType = gem1.typeIndex;
        gem1.typeIndex = gem2.typeIndex;
        gem2.typeIndex = tempType;
    }

    /// <summary>
    /// startGem의 현재 위치에서 3개 이상의 직선 그룹이 만들어지는지 확인
    /// 이 함수는 시뮬레이션 중인 젬의 타입을 읽어옴
    /// </summary>
    private bool CheckForLinearMatch(Gem startGem)
    {
        if (startGem == null) return false;

        int x = startGem.x;
        int y = startGem.y;
        int targetType = startGem.typeIndex; // 시뮬레이션 중인(바뀐) 타입

        // 가로(Horizontal) 체크
        int horizontalCount = 1;
        // 왼쪽 탐색
        for (int i = x - 1; i >= 0; i--)
        {
            Gem neighbor = gridManager.GetGemAt(i, y);
            // 일반 젬이고 타입이 같을 때만 카운트
            if (neighbor != null && neighbor.typeIndex == targetType && neighbor.specialType == Gem.SpecialGemType.None)
                horizontalCount++;
            else
                break;
        }
        // 오른쪽 탐색
        for (int i = x + 1; i < gridManager.width; i++)
        {
            Gem neighbor = gridManager.GetGemAt(i, y);
            if(neighbor != null && neighbor.typeIndex == targetType && neighbor.specialType == Gem.SpecialGemType.None)
                horizontalCount++;
            else
                break;
        }

        if (horizontalCount >= 3) return true;

        // 세로(Vertical) 체크
        int verticalCount = 1;
        // 아래쪽 탐색
        for (int i = y - 1; i >= 0; i--)
        {
            Gem neighbor = gridManager.GetGemAt(x, i);
            if (neighbor != null && neighbor.typeIndex == targetType && neighbor.specialType == Gem.SpecialGemType.None)
                verticalCount++;
            else
                break;
        }
        // 위쪽 탐색
        for (int i = y + 1; i < gridManager.height; i++)
        {
            Gem neighbor = gridManager.GetGemAt(x, i);
            if (neighbor != null && neighbor.typeIndex == targetType && neighbor.specialType == Gem.SpecialGemType.None)
                verticalCount++;
            else
                break;
        }

        if (verticalCount >= 3) return true;

        return false;
    }


    /// <summary>
    /// (UI 버튼에서 호출)힌트 요청 함수
    /// </summary>
    public void RequestHintFromButton()
    {
        // 힌트 코루틴이 돌고 있다면 중지
        StopHintVisual();

        if (allValidMoves == null || allValidMoves.Count == 0)
        {
            Debug.Log("HintManager: No hints available to show.");
            return;
        }

        // 새로운 랜덤 힌트 뽑아서 설정
        currentHint = allValidMoves[Random.Range(0, allValidMoves.Count)];

        // 즉시 힌트 표시
        ShowHint(true);
    }


    /// <summary>
    /// 힌트 시각화 시작
    /// </summary>
    void ShowHint(bool immediate = false)
    {
        // 새 힌트 코루틴 시작
        hintCoroutine = StartCoroutine(ShowHintCoroutine(immediate));
    }

    /// <summary>
    /// 힌트 시각화 코루틴
    /// </summary>
    IEnumerator ShowHintCoroutine(bool immediate = false)
    {
        if (currentHint == null || currentHint.Count < 2) yield break;

        Gem gem1 = currentHint[0];
        Gem gem2 = currentHint[1];

        // 힌트 시각화 젬 크기 키우기
        if (gem1 != null) gem1.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        if (gem2 != null) gem2.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

        // 힌트 시각화 파티클 생성
        if (hintParticle != null)
        {
            if (gem1 != null) Destroy(Instantiate(hintParticle, gem1.transform.position, Quaternion.identity), 1.0f);
            if (gem2 != null) Destroy(Instantiate(hintParticle, gem2.transform.position, Quaternion.identity), 1.0f);
        }

        yield return new WaitForSeconds(0.5f);

        // 힌트 시각화 원상복구
        // 힌트가 표시되는 도중 젬이 터져서 사라졌을 경우 고려
        if (gem1 != null) gem1.transform.localScale = Vector3.one;
        if (gem2 != null) gem2.transform.localScale = Vector3.one;

        hintCoroutine = null;
    }

    /// <summary>
    /// 현재 실행 중인 힌트 시각화 중지하고 젬 원상복구
    /// </summary>
    public void StopHintVisual()
    {
        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }

        // 힌트가 표시되던 중이었다면 원상복구
        if (currentHint != null && currentHint.Count > 0)
        {
            if (currentHint[0] != null)
            {
                currentHint[0].transform.localScale = Vector3.one;
            }
            if (currentHint.Count > 1 && currentHint[1] != null)
            {
                currentHint[1].transform.localScale = Vector3.one;
            }
            
            currentHint = null;
        }
    }
}