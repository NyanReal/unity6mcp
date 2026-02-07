using UnityEngine;
using UnityEngine.UI;

public class GridItemView : MonoBehaviour
{
    public Image iconImage;
    public Text quantityText;

    public void SetData(Sprite icon, int quantity)
    {
        if (iconImage != null) iconImage.sprite = icon;
        if (quantityText != null) quantityText.text = quantity.ToString();
    }
}