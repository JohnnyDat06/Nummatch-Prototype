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
        private UIController _uiController;

        private void Start() {
            List<int> tempValues = new List<int> { 4,6,1,8,2,1,5,9,2, 1,1,8,5,5,1,5,6,4, 8,9,7,2,5,8,6,3,2 };
            _currentBoard = BoardData.CreateFromValues(tempValues, 1);
            _currentBoard.AddsRemaining = 6;

            // Mục tiêu gem: 3 màu Orange / Pink / Purple (khớp sprite bạn có)
            _currentBoard.GemsNeeded = new Dictionary<GemType, int> {
                { GemType.Orange, 2 },
                { GemType.Pink,   2 },
                { GemType.Purple, 2 }
            };
            _currentBoard.GemsCollected = new Dictionary<GemType, int> {
                { GemType.Orange, 0 },
                { GemType.Pink,   0 },
                { GemType.Purple, 0 }
            };

            _uiController = FindObjectOfType<UIController>();
            _uiController?.SetAddsRemaining(_currentBoard.AddsRemaining);
            _uiController?.SetStage(_currentBoard.Stage);
            _uiController?.InitGemCounters(_currentBoard.GemsNeeded);

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

        public void ScrollToTopPublic() {
            ScrollToTop();
        }

        public void GenerateNewStage(int stage) {
            _currentBoard.Cells.Clear();
            var board = BoardGenerator.GenerateBoard(stage, 27);
            _currentBoard.Cells.AddRange(board.Cells);
            
            // Generate list of all indices for initial spawn
            List<int> newIndices = _currentBoard.Cells.Select(c => c.Index).ToList();
            GemSpawner.SpawnGems(_currentBoard, newIndices);
            
            RenderBoard(_currentBoard);
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

        /// <summary>Animation khi match thành công: pop nhẹ rồi mờ đi (KHÔNG xóa, chỉ dim).</summary>
        public void AnimateCellMatched(CellView view, Action onComplete = null) {
            var rt = view.GetComponent<RectTransform>();
            var cg = view.GetOrAddCanvasGroup();
            
            Sequence seq = DOTween.Sequence();
            // Pop nhẹ rồi thu về kích thước gốc
            seq.Append(rt.DOScale(Vector3.one * 1.2f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(rt.DOScale(Vector3.one, 0.1f).SetEase(Ease.InQuad));
            // Fade xuống mờ (alpha 0.3) — cell và text vẫn hiện nhưng bị dim
            seq.Join(DOTween.To(() => cg.alpha, x => cg.alpha = x, 0.3f, 0.15f));
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

        /// <summary>Lấy Sprite gem tương ứng từ UIController; null nếu chưa gán.</summary>
        private Sprite GetGemSpriteFor(Cell cell) {
            if (!cell.IsGem || cell.GemColor == GemType.None) return null;
            if (_uiController == null) _uiController = FindObjectOfType<UIController>();
            return _uiController?.GetGemSprite(cell.GemColor);
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
                view.Bind(cell, GetGemSpriteFor(cell));
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
            
            // Sync up UI Controller here immediately after decrementing Add uses
            FindObjectOfType<UIController>()?.SetAddsRemaining(_currentBoard.AddsRemaining);
            
            // Trigger GemSpawn cho cells mới BEFORE rendering them
            List<int> appendedIndices = new List<int>();
            for (int i = oldCount; i < _currentBoard.Cells.Count; i++) {
                appendedIndices.Add(i);
            }
            GemSpawner.SpawnGems(_currentBoard, appendedIndices);
            
            for (int i = oldCount; i < _currentBoard.Cells.Count; i++) {
                var cell = _currentBoard.Cells[i];
                var view = Instantiate(_cellPrefab, _boardContainer);
                var rt = view.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                
                Vector2 targetPos = CalcCellPosition(cell.Index);
                view.Bind(cell, GetGemSpriteFor(cell));
                _cellViews.Add(view);
                _inputController?.RegisterCell(view);
                
                float delay = (i - oldCount) * 0.02f;
                AnimateCellSpawn(view, targetPos, delay);
            }
            
            UpdateContentSize();
            DOVirtual.DelayedCall(0.1f, ScrollToBottom);
        }

        private void HandleMatchSuccess(Cell a, Cell b) {
            if (_inputController != null) _inputController.SetInputEnabled(false);
            
            a.IsMatched = true;
            b.IsMatched = true;
            
            // --- Thu thập gem nếu cell là gem ---
            CollectGemIfAny(a);
            CollectGemIfAny(b);
            
            CellView viewA = GetCellView(a.Index);
            CellView viewB = GetCellView(b.Index);
            
            // Dim animation cho 2 cell vừa match (chỉ mờ đi, KHÔNG xóa)
            int completed = 0;
            Action checkDone = () => {
                completed++;
                if (completed < 2) return;
                
                // Rebind để cập nhật trạng thái interactable + gem overlay
                viewA.Bind(a, GetGemSpriteFor(a));
                viewB.Bind(b, GetGemSpriteFor(b));
                
                // Sau khi dim xong, check xem có hàng nào đủ điều kiện clear không
                CheckAndClearRows(() => {
                    GameStateManager.Instance?.OnBoardStateChanged(_currentBoard, this, _uiController);
                    if (_inputController != null) _inputController.SetInputEnabled(true);
                });
            };
            
            AudioManager.Instance?.Play(SfxType.PairClear);
            AnimateCellMatched(viewA, checkDone);
            AnimateCellMatched(viewB, checkDone);
        }
        
        /// <summary>Nếu cell là gem và chưa thu đủ → cộng GemsCollected và cập nhật UI.</summary>
        private void CollectGemIfAny(Cell cell) {
            if (!cell.IsGem || cell.GemColor == GemType.None) return;
            
            GemType type = cell.GemColor;
            
            // Đảm bảo key tồn tại
            if (!_currentBoard.GemsCollected.ContainsKey(type))
                _currentBoard.GemsCollected[type] = 0;
            if (!_currentBoard.GemsNeeded.ContainsKey(type)) return;
            
            int needed = _currentBoard.GemsNeeded[type];
            int current = _currentBoard.GemsCollected[type];
            
            // Chỉ cộng nếu chưa đủ
            if (current < needed) {
                _currentBoard.GemsCollected[type] = current + 1;
                // Cập nhật UI ngay lập tức
                _uiController?.UpdateGemCounter(type, _currentBoard.GemsCollected[type], needed);
            }
            
            // Đánh dấu gem đã được thu (tắt overlay)
            cell.IsGem = false;
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
                            var cell = _currentBoard.Cells[i];
                            _cellViews[i].Bind(cell, GetGemSpriteFor(cell));
                        }
                        
                        AnimateShiftUp();
                        onComplete?.Invoke();
                    }
                });
            }
        }
    }
}
