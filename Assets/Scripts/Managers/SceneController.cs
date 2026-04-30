using UnityEngine;
using UnityEngine.SceneManagement;

namespace NumMatch.Managers
{
    /// <summary>
    /// Quản lý việc chuyển đổi Scene trong game NumMatch.
    /// Script này có thể gắn vào một GameObject trống (vd: "SceneManager") trong cả scene Home và Main.
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        [Header("Scene Names")]
        [Tooltip("Tên của scene màn hình chính (chơi game)")]
        public string mainSceneName = "Main";
        
        [Tooltip("Tên của scene menu (trang chủ)")]
        public string homeSceneName = "Home";

        /// <summary>
        /// Gọi hàm này từ sự kiện OnClick của nút Start ở scene Home.
        /// </summary>
        public void LoadMainScene()
        {
            // Reset Time.timeScale phòng trường hợp game đang pause
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainSceneName);
        }

        /// <summary>
        /// Gọi hàm này từ sự kiện OnClick của nút Home ở scene Main.
        /// </summary>
        public void LoadHomeScene()
        {
            // Reset Time.timeScale phòng trường hợp game đang pause
            Time.timeScale = 1f;
            SceneManager.LoadScene(homeSceneName);
        }

        /// <summary>
        /// Gọi hàm này từ sự kiện OnClick của nút Quit ở scene Home.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Đang thoát game...");
            
#if UNITY_EDITOR
            // Thoát Play Mode nếu đang ở trong Unity Editor
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // Thoát game thực tế khi đã build
            Application.Quit();
#endif
        }
    }
}
