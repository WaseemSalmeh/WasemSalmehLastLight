using System;
using LastLight.Core;
using LastLight.Data;
using LastLight.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace LastLight.UI
{
    public static class LastLightRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (LastLightApp.Instance == null)
            {
                new GameObject("LastLightApp").AddComponent<LastLightApp>();
            }

            if (AudioManager.Instance == null)
            {
                var audioRoot = new GameObject("AudioManager");
                audioRoot.AddComponent<AudioSource>();
                audioRoot.AddComponent<AudioManager>();
            }

            if (UnityEngine.Object.FindAnyObjectByType<LastLightGameController>() == null)
            {
                new GameObject("LastLightGameController").AddComponent<LastLightGameController>();
            }

            EnsureEventSystem();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                UnityEngine.Object.Destroy(legacyModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }

    public sealed class LastLightGameController : MonoBehaviour
    {
        private readonly LastLightGameSession session = new();

        private Canvas canvas;
        private RectTransform frame;
        private LastLightGameplayView gameplayView;
        private Text scoreText;
        private Text horizontalBestText;
        private Text verticalBestText;
        private Text gameOverTitleText;
        private Text finalScoreText;
        private Text finalBestText;
        private Text volumeValueText;
        private Text glowValueText;
        private Text flashesValueText;
        private Slider volumeSlider;
        private Slider glowSlider;
        private GameObject introPanel;
        private GameObject menuPanel;
        private GameObject settingsPanel;
        private GameObject gameOverPanel;
        private GameObject playingControls;
        private GameObject exitModal;
        private GameObject restartModal;
        private GameObject deathFlashOnButton;
        private GameObject deathFlashOffButton;

        private AppState visibleState = (AppState)(-1);
        private bool showExitConfirm;
        private bool showRestartConfirm;
        private bool lastRunNewBest;
        private bool updatingSettingsControls;
        private float introEndsAt;

        private Font defaultFont;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var legacyMenu = GameObject.Find("MainMenuCanvas");
            if (legacyMenu != null)
            {
                Destroy(legacyMenu);
            }

            BuildInterface();

            session.GameOver += HandleSessionGameOver;
            session.ScoreChanged += RefreshScore;

            if (LastLightApp.Instance != null)
            {
                LastLightApp.Instance.StateChanged += HandleStateChanged;
                LastLightApp.Instance.SettingsChanged += HandleSettingsChanged;
                LastLightApp.Instance.ScoreRegistered += HandleScoreRegistered;
            }
        }

        private void Start()
        {
            introEndsAt = Time.unscaledTime + 2.55f;
            AudioManager.Instance?.Opening();
            RefreshHighScores();
            RefreshSettingsControls();
            UpdateVisibleState(force: true);
        }

        private void OnDestroy()
        {
            session.GameOver -= HandleSessionGameOver;
            session.ScoreChanged -= RefreshScore;

            if (LastLightApp.Instance != null)
            {
                LastLightApp.Instance.StateChanged -= HandleStateChanged;
                LastLightApp.Instance.SettingsChanged -= HandleSettingsChanged;
                LastLightApp.Instance.ScoreRegistered -= HandleScoreRegistered;
            }
        }

        private void Update()
        {
            if (LastLightApp.Instance == null)
            {
                return;
            }

            if (LastLightApp.Instance.State == AppState.Intro && Time.unscaledTime >= introEndsAt)
            {
                LastLightApp.Instance.ReturnToMenu();
            }

            var frameSize = GetFrameSize();
            session.Resize(frameSize.x, frameSize.y);
            session.IsPaused = showExitConfirm || showRestartConfirm;
            session.Update(Time.unscaledDeltaTime);

            gameplayView.Configure(
                session,
                LastLightApp.Instance.Progress.settings.glowIntensity,
                LastLightApp.Instance.Progress.settings.deathFlashesEnabled);

            HandleKeyboardInput();
            UpdateVisibleState();
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("LastLightCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var root = (RectTransform)canvasObject.transform;
            Stretch(root);

            CreateImage("ScreenBackground", root, new Color(0f, 0f, 0f, 1f), stretch: true);

            var frameObject = CreateImage("GameFrame", root, Color.black, stretch: false);
            frame = frameObject.GetComponent<RectTransform>();
            frame.anchorMin = new Vector2(0.5f, 0.5f);
            frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(1080f, 1920f);
            var fitter = frameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 9f / 16f;

            var gameplayObject = new GameObject("GameplayView", typeof(RectTransform), typeof(LastLightGameplayView), typeof(LastLightGameplayInput));
            gameplayObject.transform.SetParent(frame, false);
            Stretch(gameplayObject.GetComponent<RectTransform>());
            gameplayView = gameplayObject.GetComponent<LastLightGameplayView>();
            gameplayView.raycastTarget = true;
            gameplayObject.GetComponent<LastLightGameplayInput>().Initialize(this);

            scoreText = CreateText("Score", frame, "00", 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(420f, 110f));
            AddGlow(scoreText.gameObject, 0.55f);

            playingControls = CreateRect("PlayingControls", frame, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 150f));
            CreateButton("HomeButton", playingControls.transform, "HOME", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(92f, -76f), new Vector2(128f, 72f), () => RunOption(() => SetExitConfirm(true)));
            CreateButton("RestartButton", playingControls.transform, "RESTART", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-112f, -76f), new Vector2(164f, 72f), () => RunOption(() => SetRestartConfirm(true)));

            introPanel = BuildIntroPanel();
            menuPanel = BuildMenuPanel();
            settingsPanel = BuildSettingsPanel();
            gameOverPanel = BuildGameOverPanel();
            exitModal = BuildConfirmModal("ExitModal", "ABANDON RUN?", "YOUR CURRENT PROGRESS WILL BE LOST.", "QUIT", () => RunOption(ReturnToMenu), () => RunOption(() => SetExitConfirm(false)));
            restartModal = BuildConfirmModal("RestartModal", "RESTART?", "ARE YOU SURE YOU WANT TO RESTART?", "RESTART", () => RunOption(RestartCurrentMode), () => RunOption(() => SetRestartConfirm(false)));
        }

        private GameObject BuildIntroPanel()
        {
            var panel = CreatePanel("IntroPanel", frame);
            CreateText("IntroTitle", panel.transform, "LAST\nLIGHT", 126, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 360f));
            var title = panel.transform.Find("IntroTitle").gameObject;
            AddGlow(title, 0.75f);
            CreateText("IntroCredit", panel.transform, "CORRIATOU", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(360f, 60f), new Color(1f, 1f, 1f, 0.38f));
            return panel;
        }

        private GameObject BuildMenuPanel()
        {
            var panel = CreatePanel("MenuPanel", frame);
            CreateText("Title", panel.transform, "LAST\nLIGHT", 116, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.71f), new Vector2(0.5f, 0.71f), Vector2.zero, new Vector2(780f, 320f));
            AddGlow(panel.transform.Find("Title").gameObject, 0.75f);

            var scoreCards = CreateRect("ScoreCards", panel.transform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(700f, 160f));
            horizontalBestText = CreateScoreCard(scoreCards.transform, "HorizontalBest", "HORIZONTAL", new Vector2(-180f, 0f));
            verticalBestText = CreateScoreCard(scoreCards.transform, "VerticalBest", "VERTICAL", new Vector2(180f, 0f));

            CreateButton("VerticalButton", panel.transform, "VERTICAL", new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(650f, 108f), () => RunModeOption(() => StartMode(GameMode.Vertical)));
            CreateButton("HorizontalButton", panel.transform, "HORIZONTAL\n(HARD)", new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(650f, 126f), () => RunModeOption(() => StartMode(GameMode.Horizontal)), 37);
            CreateButton("SettingsButton", panel.transform, "SETTINGS", new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(480f, 82f), () => RunOption(() => LastLightApp.Instance.OpenSettings()), 30);
            return panel;
        }

        private GameObject BuildSettingsPanel()
        {
            var panel = CreatePanel("SettingsPanel", frame);
            CreateText("SettingsTitle", panel.transform, "SETTINGS", 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(620f, 110f));
            AddGlow(panel.transform.Find("SettingsTitle").gameObject, 0.55f);

            volumeValueText = CreateText("VolumeValue", panel.transform, "", 26, TextAnchor.MiddleRight, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(210f, 42f), new Vector2(240f, 50f), new Color(1f, 1f, 1f, 0.45f));
            CreateText("VolumeLabel", panel.transform, "MASTER VOLUME", 26, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(-170f, 42f), new Vector2(360f, 50f), new Color(1f, 1f, 1f, 0.45f));
            volumeSlider = CreateSlider("VolumeSlider", panel.transform, new Vector2(0.5f, 0.64f), new Vector2(640f, 36f), 0f, 1f, value => UpdateSetting(settings => settings.masterVolume = value));

            glowValueText = CreateText("GlowValue", panel.transform, "", 26, TextAnchor.MiddleRight, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), new Vector2(210f, 42f), new Vector2(240f, 50f), new Color(1f, 1f, 1f, 0.45f));
            CreateText("GlowLabel", panel.transform, "GLOW INTENSITY", 26, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), new Vector2(-170f, 42f), new Vector2(360f, 50f), new Color(1f, 1f, 1f, 0.45f));
            glowSlider = CreateSlider("GlowSlider", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(640f, 36f), 0f, 1.5f, value => UpdateSetting(settings => settings.glowIntensity = value));

            flashesValueText = CreateText("FlashesValue", panel.transform, "", 26, TextAnchor.MiddleRight, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), new Vector2(210f, 42f), new Vector2(240f, 50f), new Color(1f, 1f, 1f, 0.45f));
            CreateText("FlashesLabel", panel.transform, "DEATH FLASHES", 26, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), new Vector2(-170f, 42f), new Vector2(360f, 50f), new Color(1f, 1f, 1f, 0.45f));
            deathFlashOffButton = CreateButton("DeathFlashOff", panel.transform, "OFF", new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.34f), new Vector2(-170f, 0f), new Vector2(280f, 78f), () => RunOption(() => UpdateSetting(settings => settings.deathFlashesEnabled = false)), 28);
            deathFlashOnButton = CreateButton("DeathFlashOn", panel.transform, "ON", new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.34f), new Vector2(170f, 0f), new Vector2(280f, 78f), () => RunOption(() => UpdateSetting(settings => settings.deathFlashesEnabled = true)), 28);

            CreateButton("BackButton", panel.transform, "BACK", new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(520f, 90f), () => RunOption(() => LastLightApp.Instance.ReturnToMenu()), 30);
            return panel;
        }

        private GameObject BuildGameOverPanel()
        {
            var panel = CreatePanel("GameOverPanel", frame, new Color(0f, 0f, 0f, 0.78f));
            gameOverTitleText = CreateText("GameOverTitle", panel.transform, "FAILED", 82, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(700f, 120f));
            AddGlow(gameOverTitleText.gameObject, 0.75f);
            finalScoreText = CreateText("FinalScore", panel.transform, "00", 150, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(560f, 170f));
            AddGlow(finalScoreText.gameObject, 0.75f);
            finalBestText = CreateText("FinalBest", panel.transform, "", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(680f, 60f), new Color(1f, 1f, 1f, 0.42f));
            CreateButton("RetryButton", panel.transform, "RETRY", new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(-150f, 0f), new Vector2(250f, 92f), () => RunOption(RestartCurrentMode), 31);
            CreateButton("MenuButton", panel.transform, "HOME", new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(150f, 0f), new Vector2(250f, 92f), () => RunOption(ReturnToMenu), 31);
            return panel;
        }

        private GameObject BuildConfirmModal(string name, string title, string detail, string confirmLabel, Action confirm, Action cancel)
        {
            var panel = CreatePanel(name, frame, new Color(0f, 0f, 0f, 0.76f));
            var box = CreateImage("Dialog", panel.transform, new Color(0f, 0f, 0f, 0.95f), stretch: false);
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(690f, 360f);
            box.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.16f);

            CreateText("Title", box.transform, title, 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(620f, 70f));
            CreateText("Detail", box.transform, detail, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(620f, 60f), new Color(1f, 1f, 1f, 0.42f));
            CreateButton("CancelButton", box.transform, "CANCEL", new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(-160f, 0f), new Vector2(260f, 82f), cancel, 27);
            CreateButton("ConfirmButton", box.transform, confirmLabel, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(160f, 0f), new Vector2(260f, 82f), confirm, 27, true);
            return panel;
        }

        private Text CreateScoreCard(Transform parent, string name, string label, Vector2 position)
        {
            var card = CreateImage(name, parent, new Color(1f, 1f, 1f, 0.035f), stretch: false);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(320f, 150f);
            card.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.45f);
            CreateText("Label", card.transform, label, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(280f, 48f), new Color(1f, 1f, 1f, 0.45f));
            var score = CreateText("Score", card.transform, "0", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(280f, 70f));
            AddGlow(score.gameObject, 0.45f);
            return score;
        }

        private Slider CreateSlider(string name, Transform parent, Vector2 anchor, Vector2 size, float min, float max, Action<float> onValueChanged)
        {
            var sliderObject = CreateRect(name, parent, anchor, anchor, Vector2.zero, size);
            var slider = sliderObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;

            var background = CreateImage("Background", sliderObject.transform, new Color(1f, 1f, 1f, 0.12f), stretch: true);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.offsetMin = new Vector2(0f, 13f);
            backgroundRect.offsetMax = new Vector2(0f, -13f);

            var fillArea = CreateRect("Fill Area", sliderObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(fillArea.GetComponent<RectTransform>());
            var fill = CreateImage("Fill", fillArea.transform, Color.white, stretch: true);

            var handleArea = CreateRect("Handle Slide Area", sliderObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(handleArea.GetComponent<RectTransform>());
            var handle = CreateImage("Handle", handleArea.transform, Color.white, stretch: false);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 34f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(value =>
            {
                if (!updatingSettingsControls)
                {
                    onValueChanged(value);
                }
            });

            return slider;
        }

        private GameObject CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Action onClick,
            int fontSize = 34,
            bool filled = false)
        {
            var buttonObject = CreateImage(name, parent, filled ? Color.white : new Color(1f, 1f, 1f, 0.02f), stretch: false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, filled ? 0f : 0.6f);
            outline.effectDistance = new Vector2(2f, 2f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = filled ? Color.white : new Color(1f, 1f, 1f, 0.02f);
            colors.highlightedColor = filled ? new Color(0.9f, 0.9f, 0.9f, 1f) : new Color(1f, 1f, 1f, 0.12f);
            colors.pressedColor = filled ? new Color(0.72f, 0.72f, 0.72f, 1f) : new Color(1f, 1f, 1f, 0.22f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());

            var text = CreateText("Label", buttonObject.transform, label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, filled ? Color.black : Color.white);
            Stretch(text.rectTransform);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize - 12);
            text.resizeTextMaxSize = fontSize;

            if (!filled)
            {
                AddGlow(buttonObject, 0.28f);
            }

            return buttonObject;
        }

        private GameObject CreatePanel(string name, Transform parent, Color? color = null)
        {
            return CreateImage(name, parent, color ?? Color.black, stretch: true);
        }

        private GameObject CreateImage(string name, Transform parent, Color color, bool stretch)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.GetComponent<RectTransform>();
            if (stretch)
            {
                Stretch(rect);
            }

            var image = imageObject.GetComponent<Image>();
            image.color = color;
            return imageObject;
        }

        private GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            var rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rectObject;
        }

        private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color? color = null)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color ?? Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddGlow(GameObject target, float alpha)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, alpha);
            outline.effectDistance = new Vector2(2f, 2f);
        }

        private void HandleKeyboardInput()
        {
            if (LastLightApp.Instance.State != AppState.Playing || showExitConfirm || showRestartConfirm)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (session.Mode == GameMode.Horizontal)
                {
                    if (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                    {
                        session.PressHorizontalJump();
                    }
                }
                else
                {
                    if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                    {
                        session.MoveVerticalTo(VerticalObstacleSide.Left);
                    }
                    else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                    {
                        session.MoveVerticalTo(VerticalObstacleSide.Right);
                    }
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (session.Mode == GameMode.Horizontal)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    session.PressHorizontalJump();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    session.MoveVerticalTo(VerticalObstacleSide.Left);
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    session.MoveVerticalTo(VerticalObstacleSide.Right);
                }
            }
#endif
        }

        public void HandleGameplayPointerDown()
        {
            if (LastLightApp.Instance == null || LastLightApp.Instance.State != AppState.Playing || showExitConfirm || showRestartConfirm)
            {
                return;
            }

            if (session.Mode == GameMode.Horizontal)
            {
                session.PressHorizontalJump();
            }
            else
            {
                session.ToggleVerticalSide();
            }
        }

        private void StartMode(GameMode mode)
        {
            var size = GetFrameSize();
            lastRunNewBest = false;
            showExitConfirm = false;
            showRestartConfirm = false;
            session.Start(mode, size.x, size.y);
            LastLightApp.Instance.StartGame(mode);
            RefreshScore();
            UpdateVisibleState(force: true);
        }

        private void RestartCurrentMode()
        {
            StartMode(LastLightApp.Instance.ActiveMode);
        }

        private void ReturnToMenu()
        {
            showExitConfirm = false;
            showRestartConfirm = false;
            session.Stop();
            LastLightApp.Instance.ReturnToMenu();
            UpdateVisibleState(force: true);
        }

        private void SetExitConfirm(bool value)
        {
            showExitConfirm = value;
            if (value)
            {
                showRestartConfirm = false;
            }

            UpdateVisibleState(force: true);
        }

        private void SetRestartConfirm(bool value)
        {
            showRestartConfirm = value;
            if (value)
            {
                showExitConfirm = false;
            }

            UpdateVisibleState(force: true);
        }

        private void RunOption(Action action)
        {
            AudioManager.Instance?.Option();
            action?.Invoke();
        }

        private void RunModeOption(Action action)
        {
            AudioManager.Instance?.ModeOption();
            action?.Invoke();
        }

        private void UpdateSetting(Action<GameSettings> mutate)
        {
            LastLightApp.Instance.UpdateSettings(mutate);
            RefreshSettingsControls();
        }

        private void HandleSessionGameOver(int score)
        {
            LastLightApp.Instance.FinishGame(score);
            RefreshGameOver();
        }

        private void HandleScoreRegistered(GameMode mode, int score, bool isNewBest)
        {
            lastRunNewBest = isNewBest;
            RefreshHighScores();
            RefreshGameOver();
        }

        private void HandleStateChanged(AppState _)
        {
            UpdateVisibleState(force: true);
        }

        private void HandleSettingsChanged(GameSettings _)
        {
            RefreshSettingsControls();
        }

        private void RefreshScore()
        {
            if (scoreText != null)
            {
                scoreText.text = session.DisplayScore.ToString("00");
            }
        }

        private void RefreshHighScores()
        {
            if (LastLightApp.Instance == null)
            {
                return;
            }

            if (horizontalBestText != null)
            {
                horizontalBestText.text = LastLightApp.Instance.GetBestScore(GameMode.Horizontal).ToString();
            }

            if (verticalBestText != null)
            {
                verticalBestText.text = LastLightApp.Instance.GetBestScore(GameMode.Vertical).ToString();
            }
        }

        private void RefreshGameOver()
        {
            if (LastLightApp.Instance == null || gameOverTitleText == null)
            {
                return;
            }

            var mode = LastLightApp.Instance.ActiveMode;
            var score = LastLightApp.Instance.LastScore;
            var best = LastLightApp.Instance.GetBestScore(mode);
            gameOverTitleText.text = lastRunNewBest && score > 0 ? "NEW RECORD" : "FAILED";
            finalScoreText.text = score.ToString("00");
            finalBestText.text = lastRunNewBest
                ? $"{ModeLabel(mode)} BEST: {best}"
                : best > 0 ? $"{ModeLabel(mode)} BEST: {best}" : string.Empty;
        }

        private static string ModeLabel(GameMode mode)
        {
            return mode == GameMode.Horizontal ? "HORIZONTAL" : "VERTICAL";
        }

        private void RefreshSettingsControls()
        {
            if (LastLightApp.Instance == null || volumeSlider == null)
            {
                return;
            }

            var settings = LastLightApp.Instance.Progress.settings;
            updatingSettingsControls = true;
            volumeSlider.value = settings.masterVolume;
            glowSlider.value = settings.glowIntensity;
            updatingSettingsControls = false;

            volumeValueText.text = settings.masterVolume <= 0f ? "MUTED" : $"{Mathf.RoundToInt(settings.masterVolume * 100f)}%";
            glowValueText.text = $"{Mathf.RoundToInt(settings.glowIntensity * 100f)}%";
            flashesValueText.text = settings.deathFlashesEnabled ? "ON" : "OFF";

            SetButtonSelected(deathFlashOnButton, settings.deathFlashesEnabled);
            SetButtonSelected(deathFlashOffButton, !settings.deathFlashesEnabled);
        }

        private static void SetButtonSelected(GameObject buttonObject, bool selected)
        {
            if (buttonObject == null)
            {
                return;
            }

            var image = buttonObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.02f);
            }

            var text = buttonObject.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = selected ? Color.black : Color.white;
            }
        }

        private void UpdateVisibleState(bool force = false)
        {
            if (LastLightApp.Instance == null)
            {
                return;
            }

            var state = LastLightApp.Instance.State;
            if (!force && visibleState == state)
            {
                exitModal.SetActive(showExitConfirm);
                restartModal.SetActive(showRestartConfirm);
                playingControls.SetActive(state == AppState.Playing && !showExitConfirm && !showRestartConfirm);
                return;
            }

            visibleState = state;
            introPanel.SetActive(state == AppState.Intro);
            menuPanel.SetActive(state == AppState.Menu);
            settingsPanel.SetActive(state == AppState.Settings);
            gameOverPanel.SetActive(state == AppState.GameOver);
            playingControls.SetActive(state == AppState.Playing && !showExitConfirm && !showRestartConfirm);
            scoreText.gameObject.SetActive(state == AppState.Playing);
            exitModal.SetActive(showExitConfirm);
            restartModal.SetActive(showRestartConfirm);

            if (state == AppState.Menu)
            {
                RefreshHighScores();
            }
            else if (state == AppState.Settings)
            {
                RefreshSettingsControls();
            }
            else if (state == AppState.GameOver)
            {
                RefreshGameOver();
            }
        }

        private Vector2 GetFrameSize()
        {
            if (frame == null)
            {
                return new Vector2(1080f, 1920f);
            }

            var rect = frame.rect;
            if (rect.width <= 1f || rect.height <= 1f)
            {
                return new Vector2(1080f, 1920f);
            }

            return new Vector2(rect.width, rect.height);
        }
    }

    public sealed class LastLightGameplayInput : MonoBehaviour, IPointerDownHandler
    {
        private LastLightGameController controller;

        public void Initialize(LastLightGameController owner)
        {
            controller = owner;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            controller?.HandleGameplayPointerDown();
        }
    }
}
