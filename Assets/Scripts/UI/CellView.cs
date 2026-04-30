using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NumMatch.Core;
using DG.Tweening;

namespace NumMatch.UI {
    /// <summary>View hiển thị 1 cell trên board. Quản lý text số, highlight, gem overlay và dim animation.</summary>
    public class CellView : MonoBehaviour {
        [Header("Core")]
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private Image _background;
        [SerializeField] private Button _button;
        [SerializeField] private Image _selectHighlight;
        [SerializeField] private Image _selectHighlightGem;

        [Header("Gem")]
        [SerializeField] private Image _gemOverlay;

        private Cell _data;
        private CanvasGroup _canvasGroup;
        private Color _defaultTextColor;

        // Màu mặc định cho từng GemType (dùng khi không có sprite)
        private static readonly Dictionary<GemType, Color> GemColors = new Dictionary<GemType, Color> {
            { GemType.Orange, new Color(1f,    0.55f, 0.1f) },
            { GemType.Pink,   new Color(0.95f, 0.4f,  0.7f) },
            { GemType.Red,    new Color(0.9f,  0.2f,  0.2f) },
            { GemType.Blue,   new Color(0.2f,  0.5f,  0.95f) },
            { GemType.Green,  new Color(0.2f,  0.8f,  0.3f) },
            { GemType.Yellow, new Color(0.95f, 0.85f, 0.1f) },
            { GemType.Purple, new Color(0.6f,  0.2f,  0.9f) },
        };

        public event Action<CellView> OnClicked;
        public bool IsSelected { get; private set; }

        private void Awake() {
            if (_button != null) {
                _button.onClick.AddListener(HandleClick);
            }
            // Lưu màu text gốc (set trong Inspector) để restore khi cell không còn là gem
            if (_valueText != null)
                _defaultTextColor = _valueText.color;
        }

        /// <summary>Khởi tạo hiển thị của cell dựa trên data. Truyền gemSprite nếu có.</summary>
        public void Bind(Cell cell, Sprite gemSprite = null) {
            _data = cell;

            // Luôn hiển thị số; nếu là gem → đổi màu chữ sang trắng để nổi trên overlay
            _valueText.text = cell.Value.ToString();
            bool isGem = cell.IsGem && cell.GemColor != GemType.None;
            _valueText.color = isGem ? Color.white : _defaultTextColor;
            _valueText.gameObject.SetActive(true);

            SetSelected(false);
            SetInteractable(!cell.IsMatched);
            gameObject.SetActive(true);

            // Dim matched cells
            var cg = GetOrAddCanvasGroup();
            cg.alpha = cell.IsMatched ? 0.3f : 1f;

            // Cập nhật gem overlay với sprite đúng màu
            RefreshGemOverlay(cell, gemSprite);
        }

        /// <summary>Cập nhật gem overlay theo trạng thái IsGem / GemColor của cell.
        /// Truyền sprite thật nếu có; nếu null sẽ fallback sang màu cứng.</summary>
        public void RefreshGemOverlay(Cell cell, Sprite gemSprite = null) {
            if (_gemOverlay == null) return;

            bool showGem = cell.IsGem && cell.GemColor != GemType.None;
            _gemOverlay.gameObject.SetActive(showGem);

            if (!showGem) return;

            if (gemSprite != null) {
                // Dùng sprite thật (Orange/Pink/Purple asset)
                _gemOverlay.sprite = gemSprite;
                _gemOverlay.color  = Color.white; // Để sprite hiện đúng màu gốc
            } else if (GemColors.TryGetValue(cell.GemColor, out Color c)) {
                // Fallback: không có sprite → dùng màu solid
                _gemOverlay.sprite = null;
                _gemOverlay.color  = c;
            }
        }

        public void SetSelected(bool selected) {
            IsSelected = selected;
            
            bool isGem = _data != null && _data.IsGem;

            if (_selectHighlight != null) {
                _selectHighlight.gameObject.SetActive(selected && !isGem);
            }
            if (_selectHighlightGem != null) {
                _selectHighlightGem.gameObject.SetActive(selected && isGem);
            }
        }

        public void SetInteractable(bool value) {
            if (_button != null) {
                _button.interactable = value;
            }
        }

        private void HandleClick() {
            if (_data == null || _data.IsMatched) return;
            OnClicked?.Invoke(this);
        }

        /// <summary>Trả về data thật của cell này.</summary>
        public Cell GetData() => _data;

        public CanvasGroup GetOrAddCanvasGroup() {
            if (_canvasGroup == null) {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            return _canvasGroup;
        }
    }
}
