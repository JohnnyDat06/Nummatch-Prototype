using UnityEngine;

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
    }
}
