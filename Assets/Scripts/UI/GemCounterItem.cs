using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NumMatch.Core;

namespace NumMatch.UI
{
    public class GemCounterItem : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _counterText;
        
        public GemType Type { get; private set; }
        
        public void Init(GemType type, Sprite icon, int needed)
        {
            Type = type;
            if (_iconImage != null && icon != null)
            {
                _iconImage.sprite = icon;
            }
            UpdateCounter(0, needed);
        }
        
        public void UpdateCounter(int collected, int needed)
        {
            if (_counterText != null)
            {
                _counterText.text = $"{collected}/{needed}";
            }
        }
    }
}
