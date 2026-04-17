using UnityEngine;

namespace NumMatch.Managers
{
    public enum SFX
    {
        ChooseNumber,
        PairClear,
        RowClear,
        Pop2
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        private void Awake()
        {
            Instance = this;
        }

        public void Play(SFX sfx)
        {
            Debug.Log($"[AudioManager] Played SFX: {sfx}");
        }
    }
}
