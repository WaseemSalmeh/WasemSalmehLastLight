using System;
using LastLight.Data;
using UnityEngine;

namespace LastLight.Core
{
    public sealed class LastLightApp : MonoBehaviour
    {
        public static LastLightApp Instance { get; private set; }

        public AppState State { get; private set; } = AppState.Intro;
        public GameMode ActiveMode { get; private set; } = GameMode.Horizontal;
        public PlayerProgress Progress { get; private set; }
        public int LastScore { get; private set; }

        public event Action<AppState> StateChanged;
        public event Action<GameSettings> SettingsChanged;
        public event Action<GameMode, int, bool> ScoreRegistered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Progress = SaveService.LoadOrCreate();
            ApplySettingsToAudio();
        }

        public void SetState(AppState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            State = nextState;
            StateChanged?.Invoke(State);
        }

        public void StartGame(GameMode mode)
        {
            ActiveMode = mode;
            LastScore = 0;
            SetState(AppState.Playing);
        }

        public void ReturnToMenu()
        {
            SetState(AppState.Menu);
        }

        public void OpenSettings()
        {
            SetState(AppState.Settings);
        }

        public void FinishGame(int score)
        {
            LastScore = Mathf.Max(0, score);
            Progress.totalRuns += 1;

            var isNewBest = Progress.highScores.TrySet(ActiveMode, LastScore);
            SaveService.Save(Progress);

            ScoreRegistered?.Invoke(ActiveMode, LastScore, isNewBest);
            SetState(AppState.GameOver);
        }

        public int GetBestScore(GameMode mode)
        {
            return Progress.highScores.Get(mode);
        }

        public void UpdateSettings(Action<GameSettings> mutate)
        {
            if (Progress == null)
            {
                Progress = new PlayerProgress();
            }

            mutate?.Invoke(Progress.settings);
            ApplySettingsToAudio();
            SaveService.Save(Progress);
            SettingsChanged?.Invoke(Progress.settings);
        }

        private void ApplySettingsToAudio()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(Progress.settings.masterVolume);
            }
        }
    }
}

