using UnityEngine;
using UnityEngine.UI;

public class PopupOpener : MonoBehaviour
{
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private Transform popupParent;  // Canvas

    private void Awake()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenPopup);
        }
    }

    public void OpenPopup()
    {
        if (popupPrefab == null) return;

        Transform parent = popupParent != null ? popupParent : transform;
        Instantiate(popupPrefab, parent);
    }

    private void OnDestroy()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPopup);
        }
    }
}
