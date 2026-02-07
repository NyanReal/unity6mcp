using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GridScrollView 테스트용 스크립트
/// cookicons 폴더의 스프라이트들을 로드하여 테스트
/// </summary>
public class GridScrollViewSceneTester : MonoBehaviour
{
    [SerializeField] private GridScrollViewController scrollViewController;
    [SerializeField] private Button openButton;
    [SerializeField] private Sprite[] testSprites;

    private void Start()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(OnOpenButtonClicked);
        }
    }

    private void OnOpenButtonClicked()
    {
        if (scrollViewController == null || testSprites == null || testSprites.Length == 0)
        {
            Debug.LogWarning("ScrollViewController or testSprites not assigned!");
            return;
        }

        List<GridItemData> items = new List<GridItemData>();
        
        foreach (var sprite in testSprites)
        {
            if (sprite != null)
            {
                int randomQuantity = Random.Range(1, 100);
                items.Add(new GridItemData(sprite, randomQuantity));
            }
        }

        scrollViewController.Open(items);
    }

    private void OnDestroy()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OnOpenButtonClicked);
        }
    }
}
