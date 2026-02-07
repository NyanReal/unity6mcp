using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Grid ScrollView 컨트롤러
/// 아이템 리스트를 받아 동적으로 Grid 형태로 표시
/// </summary>
public class GridScrollViewController : MonoBehaviour
{
    [SerializeField] private GameObject gridItemPrefab;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private Button closeButton;

    private List<GameObject> spawnedItems = new List<GameObject>();

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    /// <summary>
    /// ScrollView 열기 - 아이템 데이터 리스트로 Grid 생성
    /// </summary>
    /// <param name="items">표시할 아이템 데이터 리스트</param>
    public void Open(List<GridItemData> items)
    {
        // 기존 아이템 정리
        ClearItems();

        // 새 아이템 생성
        if (items != null && gridItemPrefab != null && contentContainer != null)
        {
            foreach (var itemData in items)
            {
                GameObject itemObj = Instantiate(gridItemPrefab, contentContainer);
                GridItemView itemView = itemObj.GetComponent<GridItemView>();
                if (itemView != null)
                {
                    itemView.SetData(itemData);
                }
                spawnedItems.Add(itemObj);
            }
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// ScrollView 닫기
    /// </summary>
    public void Close()
    {
        ClearItems();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 생성된 아이템들 정리
    /// </summary>
    private void ClearItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedItems.Clear();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }
}
