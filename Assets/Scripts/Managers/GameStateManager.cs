using UnityEngine;
using System.Collections.Generic;
using NumMatch.Core;

namespace NumMatch.Managers
{
    /// <summary>Quản lý Level, Stage, Win/Lose flow và điều phối game state.</summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        /// <summary>Level hiện tại (bắt đầu từ 1, tăng khi Win → Next Level).</summary>
        public int CurrentLevel { get; private set; } = 1;

        private const int DEFAULT_ADDS = 6;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Lấy GemsNeeded cho một level (scale theo level).</summary>
        public static Dictionary<GemType, int> GetGemsNeededForLevel(int level)
        {
            // Level 1: 2 mỗi loại, Level 2+: tăng dần
            int baseCount = 2;
            int extra = Mathf.Min(level - 1, 3); // tối đa thêm 3

            return new Dictionary<GemType, int> {
                { GemType.Orange, baseCount + extra },
                { GemType.Pink,   baseCount + extra },
                { GemType.Purple, baseCount + extra }
            };
        }

        /// <summary>Khởi tạo game cho Level 1 (gọi từ BoardManager.Start).</summary>
        public void InitGame(BoardData board, BoardManager boardManager, UIController uiController)
        {
            CurrentLevel = 1;
            SetupLevel(board, boardManager, uiController);
        }

        /// <summary>Setup board + UI cho level hiện tại.</summary>
        private void SetupLevel(BoardData board, BoardManager boardManager, UIController uiController)
        {
            board.Stage = 1;
            board.AddsRemaining = DEFAULT_ADDS;

            var needed = GetGemsNeededForLevel(CurrentLevel);
            board.GemsNeeded = needed;
            board.GemsCollected = new Dictionary<GemType, int>();
            foreach (var key in needed.Keys)
                board.GemsCollected[key] = 0;

            boardManager.GenerateNewStage(board.Stage);

            uiController?.SetStage(board.Stage);
            uiController?.SetAddsRemaining(board.AddsRemaining);
            uiController?.InitGemCounters(board.GemsNeeded);
            uiController?.SetLevel(CurrentLevel);

            boardManager.ScrollToTopPublic();
        }

        public void GoHome()
        {
            AudioManager.Instance?.Play(SfxType.PopButton);
            Debug.Log("GoHome called!");
        }

        /// <summary>Gọi sau mỗi match/row clear. Check stage transition nếu board hết số.</summary>
        public void OnBoardStateChanged(BoardData board, BoardManager boardManager, UIController uiController)
        {
            // Win đã được check riêng ở CheckWinCondition, chỉ check stage transition ở đây
            if (board.IsBoardAllMatched() && !board.IsAllGemsCollected())
            {
                // Transition to next stage
                board.Stage++;
                board.AddsRemaining = DEFAULT_ADDS;

                boardManager.GenerateNewStage(board.Stage);

                uiController?.SetStage(board.Stage);
                uiController?.SetAddsRemaining(board.AddsRemaining);
                boardManager.ScrollToTopPublic();

                Debug.Log($"Transitioned to Stage {board.Stage}");
            }
        }

        /// <summary>Check Win: collected[t] >= needed[t] cho MỌI type.</summary>
        public bool CheckWinCondition(BoardData board)
        {
            return board.IsAllGemsCollected();
        }

        /// <summary>Check Lose: ĐỒNG THỜI không còn pair + AddsRemaining=0 + chưa đủ gem.</summary>
        public bool CheckLoseCondition(BoardData board)
        {
            if (board.IsAllGemsCollected()) return false; // Chưa thua nếu đã thắng
            if (board.AddsRemaining > 0) return false;    // Còn lượt add → chưa thua
            if (MatchValidator.HasAnyMatchablePair(board)) return false; // Còn cặp → chưa thua
            return true;
        }

        /// <summary>Xử lý khi Win: dừng input, hiện popup.</summary>
        public void HandleWin(BoardData board, BoardManager boardManager, UIController uiController, InputController inputController)
        {
            inputController?.SetInputEnabled(false);
            AudioManager.Instance?.Play(SfxType.Win); // PHÁT SFX THẮNG!
            uiController?.ShowWinPopup(CurrentLevel, () => {
                OnNextLevel(board, boardManager, uiController, inputController);
            });
        }

        /// <summary>Xử lý khi Lose: dừng input, hiện popup.</summary>
        public void HandleLose(BoardData board, BoardManager boardManager, UIController uiController, InputController inputController)
        {
            inputController?.SetInputEnabled(false);
            AudioManager.Instance?.Play(SfxType.Lose); // PHÁT SFX THUA!
            uiController?.ShowLosePopup(CurrentLevel, () => {
                OnRetry(board, boardManager, uiController, inputController);
            });
        }

        /// <summary>Next Level: Level+1 → reset toàn bộ → board mới.</summary>
        private void OnNextLevel(BoardData board, BoardManager boardManager, UIController uiController, InputController inputController)
        {
            CurrentLevel++;
            SetupLevel(board, boardManager, uiController);
            inputController?.SetInputEnabled(true);
            Debug.Log($"Started Level {CurrentLevel}");
        }

        /// <summary>Retry: về Level 1 hoàn toàn.</summary>
        private void OnRetry(BoardData board, BoardManager boardManager, UIController uiController, InputController inputController)
        {
            CurrentLevel = 1;
            SetupLevel(board, boardManager, uiController);
            inputController?.SetInputEnabled(true);
            Debug.Log("Retried → back to Level 1");
        }
    }
}
