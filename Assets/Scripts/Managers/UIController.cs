using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NumMatch.Core;
using NumMatch.UI;
using TMPro;
using DG.Tweening;

namespace NumMatch.Managers
{
    public class UIController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private TextMeshProUGUI _levelText;
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

        [Header("Popup Win")]
        [SerializeField] private GameObject _winPopup;
        [SerializeField] private RectTransform _winPanel;
        [SerializeField] private TextMeshProUGUI _winTitleText;
        [SerializeField] private TextMeshProUGUI _winInfoText;
        [SerializeField] private Button _nextLevelButton;

        [Header("Popup Lose")]
        [SerializeField] private GameObject _losePopup;
        [SerializeField] private RectTransform _losePanel;
        [SerializeField] private TextMeshProUGUI _loseTitleText;
        [SerializeField] private TextMeshProUGUI _loseInfoText;
        [SerializeField] private Button _retryButton;

        private Dictionary<GemType, GemCounterItem> _activeGemCounters = new Dictionary<GemType, GemCounterItem>();

        // Callbacks cho popup buttons
        private Action _onNextLevel;
        private Action _onRetry;

        private void Awake()
        {
            _gemSpriteDict = new Dictionary<GemType, Sprite>();
            foreach (var mapping in _gemSprites)
            {
                _gemSpriteDict[mapping.Type] = mapping.Icon;
            }

            // Setup popup button listeners
            if (_nextLevelButton != null)
                _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);

            // Hide popups on start
            if (_winPopup != null) _winPopup.SetActive(false);
            if (_losePopup != null) _losePopup.SetActive(false);
        }

        /// <summary>Trả về sprite tương ứng với GemType. Null nếu chưa gán.</summary>
        public Sprite GetGemSprite(GemType type)
        {
            _gemSpriteDict.TryGetValue(type, out Sprite s);
            return s;
        }

        /// <summary>Cập nhật text Level.</summary>
        public void SetLevel(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Level: {level}";
                _levelText.color = _mainTextColor;
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

        // ═══════════════════════════════════════
        //  POPUP WIN
        // ═══════════════════════════════════════

        /// <summary>Hiển thị PopupWin với animation scale.</summary>
        public void ShowWinPopup(int level, Action onNextLevel)
        {
            _onNextLevel = onNextLevel;

            if (_winTitleText != null)
                _winTitleText.text = "You Win!";
            if (_winInfoText != null)
                _winInfoText.text = $"Level {level} Complete";

            if (_winPopup != null)
            {
                _winPopup.SetActive(true);
                AnimatePopupIn(_winPanel);
            }
        }

        private void OnNextLevelClicked()
        {
            AudioManager.Instance?.Play(SfxType.PopButton);
            if (_winPopup != null)
                _winPopup.SetActive(false);
            _onNextLevel?.Invoke();
        }

        // ═══════════════════════════════════════
        //  POPUP LOSE
        // ═══════════════════════════════════════

        /// <summary>Hiển thị PopupLose với animation scale.</summary>
        public void ShowLosePopup(int level, Action onRetry)
        {
            _onRetry = onRetry;

            if (_loseTitleText != null)
                _loseTitleText.text = "Game Over";
            if (_loseInfoText != null)
                _loseInfoText.text = $"Failed at Level {level}";

            if (_losePopup != null)
            {
                _losePopup.SetActive(true);
                AnimatePopupIn(_losePanel);
            }
        }

        private void OnRetryClicked()
        {
            AudioManager.Instance?.Play(SfxType.PopButton);
            if (_losePopup != null)
                _losePopup.SetActive(false);
            _onRetry?.Invoke();
        }

        // ═══════════════════════════════════════
        //  SHARED POPUP ANIMATION
        // ═══════════════════════════════════════

        /// <summary>Animate popup panel: scale 0.8→1.0 với Ease.OutBack.</summary>
        private void AnimatePopupIn(RectTransform panel)
        {
            if (panel == null) return;
            panel.localScale = Vector3.one * 0.8f;
            panel.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
    }
}
