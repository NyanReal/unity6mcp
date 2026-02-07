using UnityEngine;

/// <summary>
/// Grid 아이템 데이터 구조체
/// </summary>
[System.Serializable]
public class GridItemData
{
    public Sprite thumbnail;
    public int quantity;

    public GridItemData(Sprite thumbnail, int quantity)
    {
        this.thumbnail = thumbnail;
        this.quantity = quantity;
    }
}
