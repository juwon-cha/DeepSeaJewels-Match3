using UnityEngine;

/// <summary>
/// 개별 젬(보석)의 동작 관리
/// </summary>
public class Gem : MonoBehaviour
{
    public enum SpecialGemType
    {
        None,
        HorizontalLine,
        VerticalLine
    }

    [Header("Gem 데이터")]
    public int typeIndex; // GridManager의 gemPrefabs 배열 인덱스
    public int x;
    public int y;

    public SpecialGemType specialType = SpecialGemType.None;

    private GridManager gridManager;
    private Vector2 targetPosition;
    private bool isMoving = false;

    /// <summary>
    /// 젬 초기화 (GridManager가 호출)
    /// </summary>
    public void Initialize(int type, int x, int y, GridManager manager)
    {
        typeIndex = type;
        this.x = x;
        this.y = y;
        gridManager = manager;

        targetPosition = gridManager.GetWorldPosition(x, y);
        isMoving = false;
    }

    void Update()
    {
        if (isMoving)
        {
            if (Vector2.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
            else
            {
                transform.position = Vector2.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
            }
        }
    }
    public void MoveTo(int newX, int newY)
    {
        this.x = newX;
        this.y = newY;
        targetPosition = gridManager.GetWorldPosition(newX, newY);
        isMoving = true;
    }
    public bool IsMoving()
    {
        return isMoving;
    }
}
