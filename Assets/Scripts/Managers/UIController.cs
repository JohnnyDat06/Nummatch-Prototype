using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NumMatch.Core;
using NumMatch.UI;
using TMPro;

namespace NumMatch.Managers
{
    public class UIController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private RectTransform _gemCounterContainer;
        [SerializeField] private GemCounterItem _gemCounterItemPrefab;
        [SerializeField] private Color _mainTextColor = Color.white;
        
        [System.Serializable]
        public struct GemSpriteMapping
        {
            public GemType Type;
            public Sprite Icon;
        }
        [SerializeField] private List<GemSpriteMapping> _gemSprites = new List<GemSpriteMapping>();
        private Dictionary<GemType, Sprite> _gemSpriteDict;

        [Header("Bottom Bar")]
        [SerializeField] private TextMeshProUGUI _addsRemainingText;
        [SerializeField] private Button _addButton;
        [SerializeField] private CanvasGroup _addButtonCanvasGroup;

        [Header("Popups")]
        [SerializeField] private GameObject _settingPopup;
        [SerializeField] private GameObject _winPopup;
        [SerializeField] private GameObject _losePopup;

        private Dictionary<GemType, GemCounterItem> _activeGemCounters = new Dictionary<GemType, GemCounterItem>();

        private void Awake()
        {
            _gemSpriteDict = new Dictionary<GemType, Sprite>();
            foreach (var mapping in _gemSprites)
            {
                _gemSpriteDict[mapping.Type] = mapping.Icon;
            }
        }

        /// <summary>Cập nhật text Stage counter.</summary>
        public void SetStage(int stage)
        {
            if (_stageText != null)
            {
                _stageText.text = $"Stage: {stage}";
                _stageText.color = _mainTextColor;
            }
        }

        /// <summary>Cập nhật badge số lượt Add còn lại.</summary>
        public void SetAddsRemaining(int count)
        {
            if (_addsRemainingText != null)
            {
                _addsRemainingText.text = count.ToString();
            }

            if (_addButton != null)
            {
                _addButton.interactable = count > 0;
            }

            if (_addButtonCanvasGroup != null)
            {
                _addButtonCanvasGroup.alpha = count > 0 ? 1.0f : 0.4f;
            }
        }

        /// <summary>Khởi tạo lại toàn bộ gem counter row theo stage mới.</summary>
        public void InitGemCounters(Dictionary<GemType, int> needed)
        {
            foreach (var existing in _activeGemCounters.Values)
            {
                if (existing != null)
                {
                    Destroy(existing.gameObject);
                }
            }
            _activeGemCounters.Clear();

            if (_gemCounterContainer == null || _gemCounterItemPrefab == null) return;

            foreach (var kvp in needed)
            {
                GemType type = kvp.Key;
                int amountNeeded = kvp.Value;
                
                var item = Instantiate(_gemCounterItemPrefab, _gemCounterContainer);
                Sprite icon = _gemSpriteDict.ContainsKey(type) ? _gemSpriteDict[type] : null;
                item.Init(type, icon, amountNeeded);
                
                _activeGemCounters[type] = item;
            }
        }

        /// <summary>Cập nhật 1 gem counter item theo type.</summary>
        public void UpdateGemCounter(GemType type, int collected, int needed)
        {
            if (_activeGemCounters.TryGetValue(type, out var item))
            {
                item.UpdateCounter(collected, needed);
            }
        }

        /// <summary>Mở PopupSetting.</summary>
        public void OpenSettingPopup()
        {
            AudioManager.Instance?.Play(SfxType.PopButton);
            if (_settingPopup != null)
            {
                _settingPopup.SetActive(true);
            }
        }

        /// <summary>Mở PopupWin.</summary>
        public void ShowWinPopup()
        {
            if (_winPopup != null)
            {
                _winPopup.SetActive(true);
            }
        }

        /// <summary>Mở PopupLose.</summary>
        public void ShowLosePopup()
        {
            if (_losePopup != null)
            {
                _losePopup.SetActive(true);
            }
        }
    }
}
