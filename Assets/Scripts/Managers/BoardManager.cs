using System.Collections.Generic;
using UnityEngine;
using NumMatch.Core;
using NumMatch.UI;

namespace NumMatch.Managers {
    public class BoardManager : MonoBehaviour {
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _boardContainer;
        [SerializeField] private InputController _inputController;

        private BoardData _currentBoard;
        private List<CellView> _cellViews = new List<CellView>();

        private void Start() {
            List<int> tempValues = new List<int> { 4,6,1,8,2,3,2,9,2, 1,1,8,5,5,1,5,6,4, 8,9,7,2,5,8,6,3,2 };
            _currentBoard = BoardData.CreateFromValues(tempValues, 1);
            RenderBoard(_currentBoard);
            
            if (_inputController != null) {
                _inputController.Init(_currentBoard);
                foreach (var cellView in _cellViews) {
                    _inputController.RegisterCell(cellView);
                }
                _inputController.OnMatchSuccess += HandleMatchSuccess;
                _inputController.OnMatchFailed += HandleMatchFailed;
            }
        }

        private void OnDestroy() {
            if (_inputController != null) {
                _inputController.OnMatchSuccess -= HandleMatchSuccess;
                _inputController.OnMatchFailed -= HandleMatchFailed;
            }
        }

        private void HandleMatchSuccess(Cell a, Cell b) {
            a.IsMatched = true;
            b.IsMatched = true;
            RefreshCellViews();
            Debug.Log($"Matched: {a.Value} + {b.Value}");
        }

        private void HandleMatchFailed(Cell a, Cell b) {
            Debug.Log($"Failed match: {a.Value} + {b.Value}");
        }

        private void RefreshCellViews() {
            for (int i = 0; i < _cellViews.Count; i++) {
                _cellViews[i].Bind(_currentBoard.Cells[i]);
            }
        }

        /// <summary>Khởi tạo prefab tương ứng với data</summary>
        public void RenderBoard(BoardData data) {
            // Xoá view cũ nếu có
            foreach(var v in _cellViews) {
                if (_inputController != null) {
                    _inputController.UnregisterCell(v);
                }
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
