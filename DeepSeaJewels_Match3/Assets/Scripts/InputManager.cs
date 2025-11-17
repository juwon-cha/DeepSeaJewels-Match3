using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 사용자의 입력(클릭, 드래그/스와이프) 관리
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("연결")]
    public GridManager gridManager;
    public GameManager gameManager;

    private Gem selectedGem;
    private Gem targetGem;

    private InputAction clickAction;
    private InputAction positionAction;

    void Awake()
    {
        clickAction = new InputAction("Click");
        positionAction = new InputAction("Position");

        clickAction.AddBinding("<Mouse>/leftButton");
        clickAction.AddBinding("<Touchscreen>/primaryTouch/press");

        positionAction.AddBinding("<Mouse>/position");
        positionAction.AddBinding("<Touchscreen>/primaryTouch/position");

        clickAction.performed += OnClickPerformed;
        clickAction.canceled += OnClickCanceled;

        clickAction.Enable();
        positionAction.Enable();
    }

    private void OnDestroy()
    {
        if (clickAction != null)
        {
            clickAction.performed -= OnClickPerformed;
            clickAction.canceled -= OnClickCanceled;
            clickAction.Disable();
        }
        if (positionAction != null)
        {
            positionAction.Disable();
        }
    }

    void Update()
    {
        if (selectedGem != null && clickAction.IsPressed())
        {
            HandleInput(positionAction.ReadValue<Vector2>());
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        HandleInput(positionAction.ReadValue<Vector2>());
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        ReleaseMouse();
    }

    /// <summary>
    /// 스크린 좌표를 받아 젬 선택/진입을 처리하는 핵심 함수
    /// </summary>
    private void HandleInput(Vector2 screenPos)
    {
        // 게임오버 시 입력 방지
        if (gridManager.IsProcessing() || (gameManager != null && gameManager.IsGameOver()))
        {
            return;
        }

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider == null)
        {
            return;
        }

        Gem hitGem = hit.collider.GetComponent<Gem>();
        if (hitGem == null)
        {
            return;
        }

        if (selectedGem == null)
        {
            SelectGem(hitGem);
        }
        else
        {
            EnterGem(hitGem);
        }
    }

    /// <summary>
    /// 젬 선택
    /// </summary>
    public void SelectGem(Gem gem)
    {
        if (gridManager.IsProcessing()) return;
        selectedGem = gem;
    }

    /// <summary>
    /// 다른 젬 위로 마우스 진입 (스와이프 감지)
    /// </summary>
    public void EnterGem(Gem gem)
    {
        if (selectedGem != null && selectedGem != gem)
        {
            if (IsAdjacent(selectedGem, gem))
            {
                targetGem = gem;
                gridManager.SwapGems(selectedGem, targetGem);
                selectedGem = null;
                targetGem = null;
            }
        }
    }

    /// <summary>
    /// 마우스/터치 릴리즈
    /// </summary>
    public void ReleaseMouse()
    {
        selectedGem = null;
        targetGem = null;
    }

    /// <summary>
    /// 두 젬이 상하좌우로 인접했는지 확인
    /// </summary>
    bool IsAdjacent(Gem gem1, Gem gem2)
    {
        return (Mathf.Abs(gem1.x - gem2.x) == 1 && gem1.y == gem2.y) ||
               (Mathf.Abs(gem1.y - gem2.y) == 1 && gem1.x == gem2.x);
    }
}
