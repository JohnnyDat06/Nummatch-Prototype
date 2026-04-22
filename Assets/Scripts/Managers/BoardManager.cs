using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using NumMatch.Core;
using NumMatch.UI;

namespace NumMatch.Managers {
    public class BoardManager : MonoBehaviour {
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _boardContainer;
        [SerializeField] private InputController _inputController;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Manual Layout")]
        [SerializeField] private float _cellWidth = 110f;
        [SerializeField] private float _cellHeight = 110f;
        [SerializeField] private float _spacingX = 8f;
        [SerializeField] private float _spacingY = 8f;
        [SerializeField] private Vector2 _padding = new Vector2(16f, 16f);

        private BoardData _currentBoard;
        private List<CellView> _cellViews = new List<CellView>();

        private void Start() {
            List<int> tempValues = new List<int> { 4,6,1,8,2,3,2,9,2, 1,1,8,5,5,1,5,6,4, 8,9,7,2,5,8,6,3,2 };
            _currentBoard = BoardData.CreateFromValues(tempValues, 1);
            _currentBoard.AddsRemaining = 3; // Give some initial adds for testing
            
            if (_inputController != null) {
                _inputController.Init(_currentBoard);
                _inputController.OnMatchSuccess += HandleMatchSuccess;
                _inputController.OnMatchFailed += HandleMatchFailed;
            }

            RenderBoard(_currentBoard);
        }

        private void OnDestroy() {
            if (_inputController != null) {
                _inputController.OnMatchSuccess -= HandleMatchSuccess;
                _inputController.OnMatchFailed -= HandleMatchFailed;
            }
        }

        private Vector2 CalcCellPosition(int index) {
            int col = index % 9;
            int row = index / 9;
            float x = _padding.x + col * (_cellWidth + _spacingX) + _cellWidth / 2f;
            float y = -(_padding.y + row * (_cellHeight + _spacingY) + _cellHeight / 2f);
            return new Vector2(x, y);
        }

        private void UpdateContentSize() {
            int totalRows = _currentBoard.Rows;
            float contentHeight = _padding.y * 2 + totalRows * (_cellHeight + _spacingY) - _spacingY;
            var contentRT = _boardContainer.GetComponent<RectTransform>();
            if (contentRT != null) {
                contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, Mathf.Max(contentRT.sizeDelta.y, contentHeight));
            }
        }

        private void ScrollToBottom() {
            if (_scrollRect != null) {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void ScrollToTop() {
            if (_scrollRect != null) {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void AnimateCellSpawn(CellView view, Vector2 targetPos, float delay) {
            var rt = view.GetComponent<RectTransform>();
            var cg = view.GetOrAddCanvasGroup();
            
            rt.localScale = Vector3.zero;
            cg.alpha = 0f;
            rt.anchoredPosition = targetPos + new Vector2(0, 30f);
            
            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(delay);
            seq.Append(rt.DOAnchorPos(targetPos, 0.3f).SetEase(Ease.OutBack));
            seq.Join(rt.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
            seq.Join(DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, 0.2f));
        }

        public void AnimateCellMatched(CellView view, Action onComplete = null) {
            var rt = view.GetComponent<RectTransform>();
            var cg = view.GetOrAddCanvasGroup();
            
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOScale(Vector3.one * 1.2f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(rt.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            seq.Join(DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 0.2f));
            seq.OnComplete(() => onComplete?.Invoke());
        }

        private void AnimateRowClear(int row, Action onComplete) {
            int startIdx = row * 9;
            int endIdx = Mathf.Min(startIdx + 9, _cellViews.Count);
            int count = endIdx - startIdx;
            if (count <= 0) {
                onComplete?.Invoke();
                return;
            }
            int completed = 0;
            
            for (int i = startIdx; i < endIdx; i++) {
                float delay = (i - startIdx) * 0.05f;
                var rt = _cellViews[i].GetComponent<RectTransform>();
                
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(delay);
                seq.Append(rt.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
                seq.OnComplete(() => {
                    completed++;
                    if (completed >= count) onComplete?.Invoke();
                });
            }
        }

        private void AnimateShiftUp() {
            for (int i = 0; i < _cellViews.Count; i++) {
                Vector2 newPos = CalcCellPosition(i);
                var rt = _cellViews[i].GetComponent<RectTransform>();
                if (rt.anchoredPosition != newPos) {
                    rt.DOAnchorPos(newPos, 0.3f).SetEase(Ease.OutCubic);
                }
            }
            UpdateContentSize();
        }

        private CellView GetCellView(int cellIndex) {
            if (cellIndex >= 0 && cellIndex < _cellViews.Count)
                return _cellViews[cellIndex];
            return null;
        }

        public void RenderBoard(BoardData data) {
            foreach (var v in _cellViews) {
                if (v != null) {
                    DOTween.Kill(v.GetComponent<RectTransform>());
                    _inputController?.UnregisterCell(v);
                    Destroy(v.gameObject);
                }
            }
            _cellViews.Clear();

            foreach (var cell in data.Cells) {
                var view = Instantiate(_cellPrefab, _boardContainer);
                var rt = view.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                
                Vector2 targetPos = CalcCellPosition(cell.Index);
                view.Bind(cell);
                _cellViews.Add(view);
                _inputController?.RegisterCell(view);
                
                float delay = cell.Index * 0.02f;
                AnimateCellSpawn(view, targetPos, delay);
            }
            
            UpdateContentSize();
            ScrollToTop();
        }

        public void OnAddPressed() {
            AudioManager.Instance?.Play(SfxType.PopButton);
            if (_currentBoard.AddsRemaining <= 0) return;
            
            var unmatchedValues = _currentBoard.GetUnmatchedCells()
                .Select(c => c.Value).ToList();
            
            int oldCount = _currentBoard.Cells.Count;
            _currentBoard.AppendCells(unmatchedValues);
            _currentBoard.AddsRemaining--;
            
            for (int i = oldCount; i < _currentBoard.Cells.Count; i++) {
                var cell = _currentBoard.Cells[i];
                var view = Instantiate(_cellPrefab, _boardContainer);
                var rt = view.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                
                Vector2 targetPos = CalcCellPosition(cell.Index);
                view.Bind(cell);
                _cellViews.Add(view);
                _inputController?.RegisterCell(view);
                
                float delay = (i - oldCount) * 0.02f;
                AnimateCellSpawn(view, targetPos, delay);
            }
            
            UpdateContentSize();
            DOVirtual.DelayedCall(0.1f, ScrollToBottom);
            
            // TODO: Trigger GemSpawn cho cells mới
        }

        private void HandleMatchSuccess(Cell a, Cell b) {
            if (_inputController != null) _inputController.SetInputEnabled(false);
            
            a.IsMatched = true;
            b.IsMatched = true;
            
            CellView viewA = GetCellView(a.Index);
            CellView viewB = GetCellView(b.Index);
            
            int completed = 0;
            Action checkDone = () => {
                completed++;
                if (completed < 2) return;
                
                viewA.Bind(a);
                viewB.Bind(b);
                
                CheckAndClearRows(() => {
                    // CheckWinLose();
                    if (_inputController != null) _inputController.SetInputEnabled(true);
                });
            };
            
            AudioManager.Instance?.Play(SfxType.PairClear);
            AnimateCellMatched(viewA, checkDone);
            AnimateCellMatched(viewB, checkDone);
        }

        private void HandleMatchFailed(Cell a, Cell b) {
            Debug.Log($"Failed match: {a.Value} + {b.Value}");
        }

        private void CheckAndClearRows(Action onComplete) {
            List<int> rowsToClear = new List<int>();
            for (int r = _currentBoard.Rows - 1; r >= 0; r--) {
                if (_currentBoard.IsRowAllMatched(r)) {
                    rowsToClear.Add(r);
                }
            }
            
            if (rowsToClear.Count == 0) {
                onComplete?.Invoke();
                return;
            }
            
            AudioManager.Instance?.Play(SfxType.RowClear);
            
            int clearedCount = 0;
            foreach (int row in rowsToClear) {
                AnimateRowClear(row, () => {
                    clearedCount++;
                    if (clearedCount >= rowsToClear.Count) {
                        // All row animations completed, safely remove data
                        // Since rowsToClear is descending (e.g. 5, 2), we can remove safely without index shift
                        foreach (int r in rowsToClear) {
                            int start = r * 9;
                            int end = Mathf.Min(start + 9, _cellViews.Count);
                            for (int j = end - 1; j >= start; j--) {
                                _inputController?.UnregisterCell(_cellViews[j]);
                                Destroy(_cellViews[j].gameObject);
                                _cellViews.RemoveAt(j);
                            }
                            _currentBoard.RemoveRow(r);
                        }
                        
                        // Rebind remaining cells explicitly
                        for (int i = 0; i < _cellViews.Count; i++) {
                            _cellViews[i].Bind(_currentBoard.Cells[i]);
                        }
                        
                        AnimateShiftUp();
                        onComplete?.Invoke();
                    }
                });
            }
        }
    }
}
