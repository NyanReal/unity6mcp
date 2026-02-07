using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 Grid 아이템 뷰 컴포넌트
/// 썸네일 이미지와 수량 텍스트를 표시
/// </summary>
public class GridItemView : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Text quantityText;

    /// <summary>
    /// 아이템 데이터 설정
    /// </summary>
    /// <param name="sprite">썸네일 스프라이트</param>
    /// <param name="quantity">수량</param>
    public void SetData(Sprite sprite, int quantity)
    {
        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = sprite;
        }

        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
        }
    }

    /// <summary>
    /// GridItemData로 아이템 설정
    /// </summary>
    /// <param name="data">아이템 데이터</param>
    public void SetData(GridItemData data)
    {
        if (data != null)
        {
            SetData(data.thumbnail, data.quantity);
        }
    }
}
