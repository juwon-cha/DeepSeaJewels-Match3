using UnityEngine;
using TMPro;

/// <summary>
/// 게임의 전반적인 상태(점수, 남은 횟수, 게임오버)를 관리합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("게임 설정")]
    public int initialMoves = 30;

    [Header("UI (인스펙터에서 연결)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI movesText;
    public TextMeshProUGUI shufflingText;
    public GameObject gameOverPanel;

    [Header("게임오버 UI (인스펙터에서 연결)")]
    public TextMeshProUGUI finalScoreText; // 최종 점수(게임오버 패널)
    public TextMeshProUGUI highScoreText;  // 최고 점수(게임오버 패널)

    [Header("연결 (인스펙터에서)")]
    public GridManager gridManager;
    public HintManager hintManager;

    private int currentScore = 0;
    private int currentMoves = 0;
    private bool isGameOver = false;

    private int highScore = 0;
    private const string HighScoreKey = "Match3HighScore";

    void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("[GameManager] GridManager가 인스펙터에 연결되지 않았습니다!");
        }
        if (hintManager == null)
        {
            Debug.LogWarning("[GameManager] HintManager가 연결되지 않았습니다! (힌트 버튼/셔플 작동 안 함)");
        }

        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        InitializeGame();
    }

    /// <summary>
    /// 게임 초기화
    /// </summary>
    void InitializeGame()
    {
        currentScore = 0;
        currentMoves = initialMoves;
        isGameOver = false;

        UpdateUI();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (shufflingText != null)
        {
            shufflingText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 점수 추가
    /// </summary>
    public void AddScore(int amount)
    {
        if (isGameOver)
        {
            return;
        }

        currentScore += amount;
        UpdateUI();
    }

    /// <summary>
    /// 이동 횟수 차감
    /// </summary>
    public void DecreaseMoves()
    {
        if (isGameOver)
        {
            return;
        }

        currentMoves--;
        if (currentMoves < 0)
        {
            currentMoves = 0;
        }

        UpdateUI();
    }

    /// <summary>
    /// UI 텍스트 업데이트
    /// </summary>
    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
        if (movesText != null)
        {
            movesText.text = "Moves: " + currentMoves;
        }
    }

    /// <summary>
    /// 게임오버 처리
    /// </summary>
    void GameOver()
    {
        if (isGameOver)
        {
            return; // 중복 호출 방지
        }

        isGameOver = true;
        Debug.Log("Game Over!");

        // 최고 점수 확인 및 저장
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save(); // 변경 사항 즉시 저장
            Debug.Log("New High Score: " + highScore);
        }

        // 게임오버 UI 텍스트 업데이트
        if (finalScoreText != null)
        {
            finalScoreText.text = "Score: " + currentScore;
        }
        if (highScoreText != null)
        {
            highScoreText.text = "High Score: " + highScore;
        }

        // 게임오버 패널 활성화
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 턴의 마지막에 GridManager가 호출하는 게임오버 검사 함수
    /// </summary>
    public void CheckForGameOver()
    {
        if (currentMoves <= 0)
        {
            GameOver();
        }
    }

    /// <summary>
    /// (HintManager가 호출) 더 이상 움직일 수 있는 젬이 없을 때
    /// </summary>
    public void NoMoreMoves()
    {
        Debug.LogWarning("No more moves! Shuffling board...");
        if (gridManager != null && !isGameOver)
        {
            gridManager.ShuffleBoard();
        }
    }

    /// <summary>
    /// 게임오버 상태 반환
    /// </summary>
    public bool IsGameOver()
    {
        return isGameOver;
    }

    /// <summary>
    /// (UI 버튼에서 호출) 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// (UI 버튼에서 호출) 힌트 버튼 클릭 시
    /// </summary>
    public void OnHintButtonPressed()
    {
        if (hintManager != null && !IsGameOver())
        {
            hintManager.RequestHintFromButton();
        }
    }
}
