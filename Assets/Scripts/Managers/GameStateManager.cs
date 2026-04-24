using UnityEngine;
using NumMatch.Core;

namespace NumMatch.Managers
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }
        
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
        
        public void GoHome()
        {
            AudioManager.Instance?.Play(SfxType.PopButton);
            Debug.Log("GoHome called!");
            // TODO: Implement actual GoHome logic when needed (e.g., SceneManager.LoadScene("HomeScene"))
        }

        public void OnBoardStateChanged(BoardData board, BoardManager boardManager, UIController uiController)
        {
            if (board.IsBoardAllMatched())
            {
                if (!board.IsAllGemsCollected())
                {
                    // Transition to next stage
                    board.Stage++;
                    board.AddsRemaining = 6; // DEFAULT_ADDS
                    
                    boardManager.GenerateNewStage(board.Stage);
                    
                    uiController?.SetStage(board.Stage);
                    uiController?.SetAddsRemaining(board.AddsRemaining);
                    boardManager.ScrollToTopPublic();
                    
                    Debug.Log($"Transitioned to Stage {board.Stage}");
                }
                else
                {
                    uiController?.ShowWinPopup();
                }
            }
        }
    }
}
