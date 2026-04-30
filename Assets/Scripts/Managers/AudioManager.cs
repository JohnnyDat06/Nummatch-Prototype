using UnityEngine;
using System;
using System.Collections.Generic;

namespace NumMatch.Managers {
    public enum SfxType {
        Choose,
        PairClear,
        RowClear,
        PopButton,
        Win,
        Lose
    }

    [Serializable]
    public struct SfxMapping {
        public SfxType Type;
        public AudioClip Clip;
    }

    public class AudioManager : MonoBehaviour {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private List<SfxMapping> _sfxMappings;

        private bool _isMuted = false;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
        }

        public void SetMuted(bool muted) {
            _isMuted = muted;
        }

        public void Play(SfxType type) {
            if (_isMuted) return;
            if (_audioSource == null) return;

            foreach (var mapping in _sfxMappings) {
                if (mapping.Type == type && mapping.Clip != null) {
                    _audioSource.PlayOneShot(mapping.Clip);
                    break;
                }
            }
        }
    }
}
