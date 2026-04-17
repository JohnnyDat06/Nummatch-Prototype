using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NumMatch.Core;

namespace NumMatch.UI {
    public class CellView : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private Image _background;
        
        private Cell _data;
        
        /// <summary>Khởi tạo hiển thị của cell dựa trên data</summary>
        public void Bind(Cell cell) {
            _data = cell;
            _valueText.text = cell.IsMatched ? "" : cell.Value.ToString();
            gameObject.SetActive(true);
        }
        
        /// <summary>Trả về data thật của cell này</summary>
        public Cell GetData() => _data;
    }
}
