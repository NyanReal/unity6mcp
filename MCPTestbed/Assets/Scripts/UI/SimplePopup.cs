using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class SimplePopup : MonoBehaviour
    {
        [SerializeField] private Button okButton;
        [SerializeField] private Image messageImage;

        private void Awake()
        {
            // Auto-wire if not set in inspector, assuming standard naming convention
            if (okButton == null)
                okButton = transform.Find("Panel/OKButton")?.GetComponent<Button>();
            
            if (messageImage == null)
                messageImage = transform.Find("Panel/MessageImage")?.GetComponent<Image>();
        }

        private void Start()
        {
            if (okButton != null)
            {
                okButton.onClick.AddListener(OnOkClicked);
            }
            else
            {
                Debug.LogWarning("SimplePopup: OK Button not assigned!");
            }
        }

        private void OnOkClicked()
        {
            // For now, just destroy the popup object
            Destroy(gameObject);
        }

        public void SetImage(Sprite sprite)
        {
            if (messageImage != null)
            {
                messageImage.sprite = sprite;
            }
        }

        private void OnDestroy()
        {
            if (okButton != null)
            {
                okButton.onClick.RemoveListener(OnOkClicked);
            }
        }
    }
}
