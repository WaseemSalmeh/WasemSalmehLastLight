using System.Collections;
using UnityEngine;

namespace LastLight.Core
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour
    {
        [Header("Imported Samples")]
        [SerializeField] private AudioClip gameOpeningClip;
        [SerializeField] private AudioClip menuOptionClip;

        public static AudioManager Instance { get; private set; }

        private AudioSource oneShotSource;
        private float masterVolume = 0.8f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            oneShotSource = GetComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            oneShotSource.loop = false;
            oneShotSource.spatialBlend = 0f;
        }

        private void Start()
        {
            if (LastLightApp.Instance != null)
            {
                SetMasterVolume(LastLightApp.Instance.Progress.settings.masterVolume);
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            oneShotSource.volume = masterVolume;
        }

        public void Opening()
        {
            PlaySample(gameOpeningClip);
        }

        public void Option()
        {
            PlaySample(menuOptionClip);
        }

        public void ModeOption()
        {
            StartCoroutine(PlayToneSequence(
                CreateTone(280f, 610f, 0.2f, Waveform.Triangle, 0.07f),
                CreateTone(860f, 1280f, 0.17f, Waveform.Sine, 0.024f, 0.025f)
            ));
        }

        public void Jump()
        {
            PlayGenerated(CreateTone(380f, 760f, 0.12f, Waveform.Sine, 0.12f));
        }

        public void Land()
        {
            PlayGenerated(CreateTone(160f, 60f, 0.1f, Waveform.Triangle, 0.06f));
        }

        public void Death()
        {
            StartCoroutine(PlayToneSequence(
                CreateTone(220f, 35f, 0.5f, Waveform.Saw, 0.14f),
                CreateTone(90f, 40f, 0.3f, Waveform.Square, 0.08f)
            ));
        }

        public void Milestone()
        {
            StartCoroutine(PlayToneSequence(
                CreateTone(523f, 523f, 0.14f, Waveform.Sine, 0.07f),
                CreateTone(659f, 659f, 0.14f, Waveform.Sine, 0.07f, 0.07f),
                CreateTone(784f, 784f, 0.14f, Waveform.Sine, 0.07f, 0.14f)
            ));
        }

        public void ShieldAppear()
        {
            StartCoroutine(PlayToneSequence(
                CreateTone(360f, 980f, 0.28f, Waveform.Sine, 0.065f),
                CreateTone(760f, 1480f, 0.24f, Waveform.Triangle, 0.038f, 0.03f)
            ));
        }

        public void ShieldWarning(float progress = 0f)
        {
            var clamped = Mathf.Clamp01(progress);
            StartCoroutine(PlayToneSequence(
                CreateTone(760f + clamped * 260f, 1120f + clamped * 320f, 0.12f, Waveform.Triangle, 0.03f + clamped * 0.018f),
                CreateTone(1360f + clamped * 380f, 1360f + clamped * 380f, 0.09f, Waveform.Sine, 0.016f + clamped * 0.012f, 0.01f)
            ));
        }

        public void ShieldEnd()
        {
            StartCoroutine(PlayToneSequence(
                CreateTone(920f, 340f, 0.26f, Waveform.Triangle, 0.06f),
                CreateTone(180f, 82f, 0.23f, Waveform.Sine, 0.08f, 0.03f)
            ));
        }

        private void PlaySample(AudioClip clip)
        {
            if (clip == null || masterVolume <= 0f)
            {
                return;
            }

            oneShotSource.PlayOneShot(clip, masterVolume);
        }

        private void PlayGenerated(AudioClip clip, float delay = 0f)
        {
            if (clip == null || masterVolume <= 0f)
            {
                return;
            }

            StartCoroutine(PlayClipAfterDelay(clip, delay));
        }

        private void PlayGenerated(ScheduledClip scheduledClip)
        {
            PlayGenerated(scheduledClip.clip, scheduledClip.delay);
        }

        private IEnumerator PlayToneSequence(params ScheduledClip[] sequence)
        {
            foreach (var scheduled in sequence)
            {
                PlayGenerated(scheduled.clip, scheduled.delay);
            }

            yield return null;
        }

        private IEnumerator PlayClipAfterDelay(AudioClip clip, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            oneShotSource.PlayOneShot(clip, masterVolume);
        }

        private ScheduledClip CreateTone(
            float startFrequency,
            float endFrequency,
            float duration,
            Waveform waveform,
            float amplitude,
            float delay = 0f)
        {
            var sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
            var samples = new float[sampleCount];
            var phase = 0f;

            for (var i = 0; i < sampleCount; i++)
            {
                var t = sampleCount == 1 ? 1f : i / (float)(sampleCount - 1);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += frequency * 2f * Mathf.PI / sampleRate;

                var envelope = Mathf.SmoothStep(1f, 0f, t);
                samples[i] = SampleWave(waveform, phase) * amplitude * envelope;
            }

            var clip = AudioClip.Create(
                $"tone-{waveform}-{startFrequency:0}-{endFrequency:0}-{duration:0.00}",
                sampleCount,
                1,
                sampleRate,
                false);
            clip.SetData(samples, 0);
            return new ScheduledClip(clip, delay);
        }

        private static float SampleWave(Waveform waveform, float phase)
        {
            switch (waveform)
            {
                case Waveform.Square:
                    return Mathf.Sign(Mathf.Sin(phase));
                case Waveform.Triangle:
                    return 2f * Mathf.Abs(2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f))) - 1f;
                case Waveform.Saw:
                    return 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f));
                default:
                    return Mathf.Sin(phase);
            }
        }

        private readonly struct ScheduledClip
        {
            public ScheduledClip(AudioClip clip, float delay)
            {
                this.clip = clip;
                this.delay = delay;
            }

            public readonly AudioClip clip;
            public readonly float delay;
        }

        private enum Waveform
        {
            Sine = 0,
            Square = 1,
            Triangle = 2,
            Saw = 3,
        }
    }
}
