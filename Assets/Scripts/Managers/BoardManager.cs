using System.Collections.Generic;
using UnityEngine;
using NumMatch.Core;
using NumMatch.UI;

namespace NumMatch.Managers {
    public class BoardManager : MonoBehaviour {
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _boardContainer;

        private BoardData _currentBoard;
        private List<CellView> _cellViews = new List<CellView>();

        private void Start() {
            List<int> tempValues = new List<int> { 4,6,1,8,2,3,2,9,2, 1,1,8,5,5,1,5,6,4, 8,9,7,2,5,8,6,3,2 };
            _currentBoard = BoardData.CreateFromValues(tempValues, 1);
            RenderBoard(_currentBoard);
        }

        /// <summary>Khởi tạo prefab tương ứng với data</summary>
        public void RenderBoard(BoardData data) {
            // Xoá view cũ nếu có
            foreach(var v in _cellViews) {
                Destroy(v.gameObject);
            }
            _cellViews.Clear();

            // Sinh view mới
            foreach(var cell in data.Cells) {
                var view = Instantiate(_cellPrefab, _boardContainer);
                view.Bind(cell);
                _cellViews.Add(view);
            }
        }
    }
}
