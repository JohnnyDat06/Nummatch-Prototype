using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NumMatch.Core;
using DG.Tweening;

namespace NumMatch.UI {
    public class CellView : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private Image _background;
        [SerializeField] private Button _button;
        [SerializeField] private Image _selectHighlight;
        
        private Cell _data;
        private CanvasGroup _canvasGroup;
        
        public event Action<CellView> OnClicked;
        public bool IsSelected { get; private set; }

        private void Awake() {
            if (_button != null) {
                _button.onClick.AddListener(HandleClick);
            }
        }
        
        /// <summary>Khởi tạo hiển thị của cell dựa trên data</summary>
        public void Bind(Cell cell) {
            _data = cell;
            _valueText.text = cell.IsMatched ? "" : cell.Value.ToString();
            SetSelected(false);
            SetInteractable(!cell.IsMatched);
            gameObject.SetActive(true);
        }
        
        public void SetSelected(bool selected) {
            IsSelected = selected;
            if (_selectHighlight != null) {
                _selectHighlight.gameObject.SetActive(selected);
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

        /// <summary>Trả về data thật của cell này</summary>
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
