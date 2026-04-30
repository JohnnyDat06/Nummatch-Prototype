using System;
using UnityEngine;
using DG.Tweening;
using NumMatch.Core;
using NumMatch.UI;

namespace NumMatch.Managers {
    public class InputController : MonoBehaviour {
        private CellView _firstSelected;
        private BoardData _currentBoard;
        
        public event Action<Cell, Cell> OnMatchSuccess; // để BoardManager xử lý remove
        public event Action<Cell, Cell, System.Collections.Generic.List<int>> OnMatchFailed;  // để SFX/animation feedback

        private bool _inputEnabled = true;

        public void SetInputEnabled(bool enabled) {
            _inputEnabled = enabled;
            if (!enabled) ResetSelection();
        }
        
        public void Init(BoardData boardData) {
            _currentBoard = boardData;
        }

        public void RegisterCell(CellView view) {
            view.OnClicked += HandleCellClicked;
        }
        
        public void UnregisterCell(CellView view) {
            view.OnClicked -= HandleCellClicked;
        }
        
        public void ResetSelection() {
            if (_firstSelected != null) {
                _firstSelected.SetSelected(false);
                _firstSelected = null;
            }
        }
        
        private void HandleCellClicked(CellView view) {
            if (!_inputEnabled) return;

            // Case 1: chưa có cell nào selected → select
            if (_firstSelected == null) {
                AudioManager.Instance?.Play(SfxType.Choose);
                _firstSelected = view;
                view.SetSelected(true);
                return;
            }
            
            // Case 2: click lại chính cell đang selected → deselect
            if (_firstSelected == view) {
                view.SetSelected(false);
                _firstSelected = null;
                return;
            }
            
            // Case 3: click cell thứ 2 → thử match
            AudioManager.Instance?.Play(SfxType.Choose);
            Cell a = _firstSelected.GetData();
            Cell b = view.GetData();
            
            bool matched = false;
            if (_currentBoard != null) {
                matched = MatchValidator.CanMatch(_currentBoard, a.Index, b.Index);
            }
            
            if (matched) {
                OnMatchSuccess?.Invoke(a, b);
                if(_firstSelected != null) _firstSelected.SetSelected(false);
                view.SetSelected(false);
                _firstSelected = null;
            } else {
                var blockingCells = new System.Collections.Generic.List<int>();
                bool isValueMatch = _currentBoard != null && (a.Value == b.Value || a.Value + b.Value == 10);
                
                if (isValueMatch) {
                    blockingCells = MatchValidator.GetBlockingCells(_currentBoard, a.Index, b.Index);
                }
                
                OnMatchFailed?.Invoke(a, b, blockingCells);
                
                if (isValueMatch) {
                    // Hiện select cho ô thứ 2, block input trong 0.4s chờ hiệu ứng shake, sau đó deselect cả hai
                    view.SetSelected(true);
                    
                    var first = _firstSelected;
                    var second = view;
                    _firstSelected = null;
                    _inputEnabled = false;
                    
                    DOVirtual.DelayedCall(0.4f, () => {
                        if (first != null) first.SetSelected(false);
                        if (second != null) second.SetSelected(false);
                        _inputEnabled = true;
                    });
                } else {
                    // Đổi lựa chọn sang ô mới click nếu giá trị hoàn toàn sai
                    if (_firstSelected != null) _firstSelected.SetSelected(false);
                    _firstSelected = view;
                    view.SetSelected(true);
                }
            }
        }
    }
}
