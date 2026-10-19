using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VertigoWheelDemo.Data;
using VertigoWheelDemo.Gameplay;
using VertigoWheelDemo.UI;
using Object = UnityEngine.Object;

namespace VertigoWheelDemo.Editor
{
    [InitializeOnLoad]
    public static class WheelPresentationChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string RunningKey = "Vertigo.PresentationChecks";
        private static IEnumerator checks;
        private static double resumeAt;
        private static readonly List<string> passed = new List<string>();
        private static readonly List<string> errors = new List<string>();
        private static GameSessionController controller;
        private static Button spin;
        private static float originalDuration;
        private const string Output = "Documentation/Presentation";

        static WheelPresentationChecks()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Tools/Vertigo/Validate Presentation")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Start validation outside Play mode.");
            SessionState.SetBool(RunningKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false))
                return;
            passed.Clear(); errors.Clear();
            Application.logMessageReceived += Log;
            checks = Run();
            resumeAt = 0;
            EditorApplication.update += Tick;
        }

        private static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(message);
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup < resumeAt) return;
                if (checks.MoveNext())
                {
                    resumeAt = EditorApplication.timeSinceStartup + (checks.Current is float seconds ? seconds : 0);
                    return;
                }
                Finish(null);
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void Finish(Exception failure)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            SessionState.SetBool(RunningKey, false);
            Directory.CreateDirectory(Output);
            string report = "# Presentation validation\n\nUnity " + Application.unityVersion +
                ". Executed in Play mode against the scene prefab and actual UI components. " +
                "Captures use a temporary camera render target; no APK was built. " +
                "Editor checks set up specific zones and outcomes without changing the shipped game rules.\n\n";
            foreach (string line in passed) report += "- PASS: " + line + "\n";
            foreach (string error in errors) report += "- ERROR: " + error + "\n";
            if (failure != null) report += "\nFAILED: " + failure + "\n";
            File.WriteAllText(Output + "/Validation.md", report);
            if (controller != null)
                Field(Object.FindFirstObjectByType<WheelSpinView>(), "spinDuration", originalDuration);
            EditorApplication.isPlaying = false;
            if (failure != null) Debug.LogException(failure);
            else if (errors.Count > 0) Debug.LogError("VERTIGO QA: runtime errors were reported. See Validation.md.");
            else Debug.Log("VERTIGO QA: All " + passed.Count + " checks passed; captures saved to " + Output);
        }

        private static IEnumerator Run()
        {
            Directory.CreateDirectory(Output);
            yield return 0.5f;
            controller = Object.FindFirstObjectByType<GameSessionController>();
            Assert(controller != null && controller.CanSpin, "Scene starts ready to spin");
            var spinView = Object.FindFirstObjectByType<WheelSpinView>();
            originalDuration = (float)Get(spinView, "spinDuration");
            Field(spinView, "spinDuration", 0.12f);
            spin = Find("ui_button_spin").GetComponent<Button>();
            var bank = Object.FindFirstObjectByType<BankView>();
            var reveal = Object.FindFirstObjectByType<RewardRevealView>();
            var openBank = Find("ui_button_bank").GetComponent<Button>();
            var closeBank = Find("ui_button_bank_close").GetComponent<Button>();
            var revealButton = reveal.GetComponent<Button>();

            var spinRect = spin.GetComponent<RectTransform>();
            var wheelRect = spinView.GetComponent<RectTransform>();
            Assert(!spinRect.IsChildOf(wheelRect) && Vector3.Distance(spinRect.position, wheelRect.position) < 0.1f,
                "Spin button is centered on the wheel and has a separate non-rotating transform");

            var hud = Object.FindFirstObjectByType<WheelHudView>();
            var previousPreview = (WheelPreviewView)Get(hud, "previousWheel");
            var nextPreview = (WheelPreviewView)Get(hud, "nextWheel");
            float idleStart = wheelRect.localEulerAngles.z;
            float nextIdleStart = nextPreview.WheelTransform.localEulerAngles.z;
            yield return 0.3f;
            Assert(Mathf.Abs(Mathf.DeltaAngle(idleStart, wheelRect.localEulerAngles.z)) > 0.25f &&
                Mathf.Abs(Mathf.DeltaAngle(nextIdleStart, nextPreview.WheelTransform.localEulerAngles.z)) > 0.25f,
                "Ready current and next wheels rotate gently in place");
            Assert(spinRect.localRotation == Quaternion.identity && wheelRect.anchoredPosition == Vector2.zero &&
                nextPreview.AnimatedVisual.anchoredPosition == Vector2.zero,
                "Idle rotation leaves wheel positions and the center button fixed");
            float beforeRefresh = nextPreview.WheelTransform.localEulerAngles.z;
            Call(controller, "HandleSpinningChanged", false);
            Assert(Mathf.Abs(Mathf.DeltaAngle(beforeRefresh, nextPreview.WheelTransform.localEulerAngles.z)) < 0.01f,
                "Refreshing the HUD preserves an unchanged preview's idle angle");

            var allRewards = new HashSet<RewardDefinition>();
            foreach (string tier in new[] { "bronze", "silver", "golden" })
            {
                var definition = AssetDatabase.LoadAssetAtPath<WheelDefinition>($"Assets/GameData/Wheels/wheel_{tier}.asset");
                var selector = new WheelRewardSelector(new System.Random(31));
                var layouts = new HashSet<string>();
                var sampled = new HashSet<RewardDefinition>();
                for (int n = 0; n < 200; n++)
                {
                    WheelSlotData[] slots = selector.Select(definition);
                    if (slots.Length != 8 || slots.Select(slot => slot.Reward).Distinct().Count() != 8)
                        throw new Exception("Repeated reward or wrong slot count in " + tier);
                    int bombCount = slots.Count(slot => slot.Reward.Type == RewardType.Bomb);
                    if (bombCount != (tier == "bronze" ? 1 : 0)) throw new Exception("Wrong bomb count in " + tier);
                    foreach (var slot in slots) { sampled.Add(slot.Reward); allRewards.Add(slot.Reward); }
                    layouts.Add(string.Join(",", slots.Select(slot => slot.Reward.RewardId)));
                }
                Assert(layouts.Count > 150, tier + " produces varied eight-slot layouts with distinct rewards and correct bomb count");
                Assert(definition.RewardPool.All(slot => sampled.Contains(slot.Reward)), tier + " pool entries are all reachable");
            }
            Assert(allRewards.Count == 36, "All 36 supplied reward sprites, including the bomb, participate in the configured pools");
            Assert(Amount("silver", RewardType.Gold) > Amount("bronze", RewardType.Gold) &&
                Amount("golden", RewardType.Gold) > Amount("silver", RewardType.Gold) &&
                Amount("silver", RewardType.Cash) > Amount("bronze", RewardType.Cash) &&
                Amount("golden", RewardType.Cash) > Amount("silver", RewardType.Cash), "Silver and golden currency bases improve strictly by tier");

            Capture("gameplay-16x9", 1920, 1080);
            Capture("gameplay-20x9", 2400, 1080);
            Capture("gameplay-4x3", 1440, 1080);
            Assert(Find("ui_text_totals_value") == null, "Inline reward totals have been replaced by the bank");
            float bankIdleAngle = wheelRect.localEulerAngles.z;
            float bankPreviewAngle = nextPreview.WheelTransform.localEulerAngles.z;
            openBank.onClick.Invoke();
            yield return 0.5f;
            Assert(controller.IsBankOpen && !controller.CanSpin && !controller.CanCashOut, "Bank blocks gameplay while open");
            Assert(((TMP_Text)Get(bank, "emptyText")).gameObject.activeSelf, "Empty bank shows its empty state");
            Assert(Mathf.Abs(Mathf.DeltaAngle(bankIdleAngle, wheelRect.localEulerAngles.z)) < 0.01f &&
                Mathf.Abs(Mathf.DeltaAngle(bankPreviewAngle, nextPreview.WheelTransform.localEulerAngles.z)) < 0.01f,
                "Opening the bank pauses idle rotation without resetting the wheels");
            closeBank.onClick.Invoke();
            Assert(controller.CanSpin, "Closing bank restores gameplay");

            var expectedNext = controller.NextSlots;
            int wonIndex = Array.FindIndex(controller.CurrentSlots, slot => slot.Reward.Type != RewardType.Bomb);
            var won = controller.CurrentSlots[wonIndex];
            int expectedAmount = GameSession.CalculateRewardAmount(won, 1);
            float heldPreviewAngle = nextPreview.WheelTransform.localEulerAngles.z;
            ForceSpin(wonIndex);
            Assert(controller.IsSpinning && !controller.CanSpin && !controller.CanCashOut, "Spin locks repeat spins and cash-out");
            controller.RequestCashOut();
            spin.onClick.Invoke();
            for (int i = 0; i < 400 && !controller.IsRewardPending; i++) yield return 0.025f;
            Assert(controller.IsRewardPending && controller.CurrentZone == 1 && !controller.CanSpin, "Reward reveal holds the winning zone and locks input");
            Assert(controller.Rewards[won.Reward] == expectedAmount, "The displayed slot amount equals the awarded bank amount");
            Assert(((Image)Get(reveal, "rewardImage")).sprite == won.Reward.Icon, "Reveal displays the actual won reward icon");
            float winningAngle = wheelRect.localEulerAngles.z;
            yield return 0.5f;
            Assert(Mathf.Abs(Mathf.DeltaAngle(winningAngle, wheelRect.localEulerAngles.z)) < 0.01f &&
                Mathf.Abs(Mathf.DeltaAngle(heldPreviewAngle, nextPreview.WheelTransform.localEulerAngles.z)) < 0.01f,
                "Real spins and reward reveals pause idle motion and preserve the winning slot");
            Capture("reward-reveal", 1920, 1080);
            Capture("reward-reveal-4x3", 1440, 1080);
            Capture("reward-reveal-20x9", 2400, 1080);
            revealButton.onClick.Invoke();
            Assert(controller.IsTransitioning && !controller.CanSpin && !controller.CanCashOut,
                "Dismissing a reward starts a locked carousel transition");
            controller.SetBankOpen(true);
            controller.ResetSession();
            spin.onClick.Invoke();
            Assert(!controller.IsBankOpen && !controller.IsSpinning && controller.IsTransitioning && controller.CurrentZone == 1,
                "Bank, reset and repeat-spin requests cannot interrupt a transition");
            yield return 0.27f;
            var transitionView = Object.FindFirstObjectByType<WheelTransitionView>();
            var movingHero = (RectTransform)Get(transitionView, "currentVisual");
            var movingNext = ((WheelPreviewView)Get(transitionView, "nextWheel")).AnimatedVisual;
            Assert(movingHero.anchoredPosition.x < -10 && movingNext.anchoredPosition.x < -10 &&
                movingHero.localScale.x < 0.99f && movingNext.localScale.x > 1.01f,
                "Current wheel slides left and shrinks while the next wheel grows toward the center");
            Assert(spinRect.localScale == Vector3.one && Vector3.Distance(spinRect.anchoredPosition, new Vector2(0, -25)) < 0.1f,
                "Carousel animation leaves the center button layout fixed");
            Capture("transition-midpoint", 1920, 1080);
            yield return 0.7f;
            Assert(!controller.IsRewardPending && controller.CurrentZone == 2 && controller.CanSpin, "A single dismiss advances exactly one zone");
            Assert(!controller.IsTransitioning && movingHero.anchoredPosition == Vector2.zero && movingHero.localScale == Vector3.one,
                "Carousel completes with restored poses and unlocked gameplay");
            Assert(ReferenceEquals(controller.CurrentSlots, expectedNext), "Next-wheel preview becomes the exact playable wheel");
            revealButton.onClick.Invoke();
            Assert(controller.CurrentZone == 2, "Repeated dismissal cannot skip zones");
            Assert(Mathf.Abs(Mathf.DeltaAngle(wheelRect.localEulerAngles.z, nextPreview.WheelTransform.localEulerAngles.z)) < 1.5f,
                "Incoming preview aligns smoothly before the new current wheel resumes idle rotation");
            float previousIdleStart = previousPreview.WheelTransform.localEulerAngles.z;
            yield return 0.2f;
            Assert(Mathf.Abs(Mathf.DeltaAngle(previousIdleStart, previousPreview.WheelTransform.localEulerAngles.z)) > 0.1f,
                "The previous wheel also rotates gently while idle");
            Assert(((Image[])Get(previousPreview, "rewardImages")).All(image =>
                Mathf.Abs(Mathf.DeltaAngle(image.transform.eulerAngles.z, 0f)) < 0.1f) &&
                ((Image[])Get(nextPreview, "rewardImages")).All(image =>
                Mathf.Abs(Mathf.DeltaAngle(image.transform.eulerAngles.z, 0f)) < 0.1f) &&
                ((Image[])Get(Object.FindFirstObjectByType<WheelContentView>(), "rewardImages")).All(image =>
                Mathf.Abs(Mathf.DeltaAngle(image.transform.eulerAngles.z, 0f)) < 0.1f),
                "Current and preview reward icons and their child amounts remain upright during idle rotation");
            Capture("adjacent-wheels", 1920, 1080);
            Capture("adjacent-wheels-4x3", 1440, 1080);
            Capture("adjacent-wheels-20x9", 2400, 1080);

            openBank.onClick.Invoke();
            yield return 0.5f;
            Assert(((RectTransform)Get(bank, "content")).GetComponentsInChildren<BankRewardRowView>().Length == 1,
                "Bank renders the won reward as an icon/name/quantity row");
            Capture("bank", 1920, 1080);
            closeBank.onClick.Invoke();

            SetZone(5);
            Assert(controller.CanCashOut && controller.CurrentDefinition.name == "wheel_silver", "Zone 5 uses a bomb-free silver wheel and permits cash-out before spinning");
            yield return 0.2f;
            Capture("silver", 1920, 1080);
            Find("ui_button_cashout").GetComponent<Button>().onClick.Invoke();
            Assert(controller.HasCashedOut && controller.IsGameOver && controller.Rewards.Count == 1, "Cash-out ends the session and retains its bank");
            Find("ui_button_restart").GetComponent<Button>().onClick.Invoke();
            Assert(controller.CurrentZone == 1 && controller.Rewards.Count == 0 && controller.CanSpin, "Restart clears the bank and restores zone 1");

            SetZone(30);
            Assert(controller.CurrentDefinition.name == "wheel_golden" && controller.CanCashOut, "Super zone 30 prioritizes golden over silver");
            Assert(controller.CurrentSlots.All(slot => slot.Reward.Type != RewardType.Bomb), "Golden wheel has no bomb");
            Assert(controller.CurrentSlots.Count(slot => slot.Reward.Type == RewardType.Item) == 6,
                "Golden wheel offers six premium items alongside its two improved currency rewards");
            yield return 0.2f;
            Capture("golden", 1920, 1080);

            // Fill the bank to validate scrolling and the complete reward catalog presentation.
            var session = (GameSession)Get(controller, "session");
            foreach (var reward in allRewards.Where(reward => reward.Type != RewardType.Bomb))
                session.ApplySpin(new WheelSlotData(reward, reward.Type == RewardType.Item ? 2 : 100));
            openBank.onClick.Invoke();
            yield return 0.5f;
            var content = (RectTransform)Get(bank, "content");
            var scroll = (ScrollRect)Get(bank, "scrollRect");
            Assert(content.GetComponentsInChildren<BankRewardRowView>().Length == 35 && content.rect.height > scroll.viewport.rect.height,
                "Bank accommodates all 35 non-bomb reward types with scrolling");
            Capture("bank-full", 1920, 1080);
            Capture("bank-full-4x3", 1440, 1080);
            Capture("bank-full-20x9", 2400, 1080);
            scroll.verticalNormalizedPosition = 0;
            yield return 0.5f;
            Capture("bank-scrolled", 1920, 1080);
            closeBank.onClick.Invoke();

            SetZone(1);
            int bombIndex = Array.FindIndex(controller.CurrentSlots, slot => slot.Reward.Type == RewardType.Bomb);
            ForceSpin(bombIndex);
            for (int i = 0; i < 400 && !controller.IsRewardPending; i++) yield return 0.025f;
            Assert(controller.IsGameOver && controller.Rewards.Count == 0 && controller.IsRewardPending,
                "Bomb clears every reward type and displays a blocking result");
            yield return 0.5f;
            Capture("bomb", 1920, 1080);
            revealButton.onClick.Invoke();
            Assert(Find("ui_button_restart").activeInHierarchy, "Restart appears after dismissing bomb result");
            Find("ui_button_restart").GetComponent<Button>().onClick.Invoke();
            Assert(controller.CanSpin && controller.Rewards.Count == 0 && !controller.IsBankOpen && !controller.IsRewardPending,
                "Restart after bomb restores an empty, playable run");

            wonIndex = Array.FindIndex(controller.CurrentSlots, slot => slot.Reward.Type != RewardType.Bomb);
            ForceSpin(wonIndex);
            for (int i = 0; i < 400 && !controller.IsRewardPending; i++) yield return 0.025f;
            revealButton.onClick.Invoke();
            Assert(controller.IsTransitioning, "A subsequent successful spin starts another transition");
            transitionView.enabled = false;
            Assert(!controller.IsTransitioning && controller.CurrentZone == 2 && controller.CanSpin &&
                movingHero.anchoredPosition == Vector2.zero && movingHero.localScale == Vector3.one,
                "Disabling the animation completes its pending advance and restores input and poses");
            transitionView.enabled = true;
            Assert(errors.Count == 0, "No runtime errors or exceptions during the validation run");
        }

        private static void ForceSpin(int index)
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                UnityEngine.Random.InitState(seed);
                var state = UnityEngine.Random.state;
                if (UnityEngine.Random.Range(0, 8) != index) continue;
                UnityEngine.Random.state = state;
                spin.onClick.Invoke();
                return;
            }
            throw new Exception("Unable to seed target outcome.");
        }

        private static void SetZone(int zone)
        {
            var session = (GameSession)Get(controller, "session");
            Field(session, "<CurrentZone>k__BackingField", zone);
            Call(controller, "PrepareLayouts"); Call(controller, "RefreshWheel");
            Call(controller, "HandleSpinningChanged", false);
        }

        private static int Amount(string tier, RewardType type) =>
            AssetDatabase.LoadAssetAtPath<WheelDefinition>($"Assets/GameData/Wheels/wheel_{tier}.asset")
                .RewardPool.First(slot => slot.Reward.Type == type).Amount;

        private static void Assert(bool condition, string description)
        {
            if (!condition) throw new Exception(description);
            passed.Add(description);
        }

        private static GameObject Find(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == name)?.gameObject;

        private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
        private static void Field(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);

        private static void Capture(string name, int width, int height)
        {
            Canvas canvas = controller.transform.parent.GetComponentInChildren<Canvas>();
            Camera camera = Camera.main;
            var mode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldTarget = camera.targetTexture;
            float oldPlane = canvas.planeDistance;
            var scaler = canvas.GetComponent<CanvasScaler>();
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var oldActive = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                scaler.enabled = false; scaler.enabled = true;
                Canvas.ForceUpdateCanvases();
                foreach (var text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget; canvas.renderMode = mode; canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldPlane; RenderTexture.active = oldActive;
                scaler.enabled = false; scaler.enabled = true;
                Object.DestroyImmediate(texture); RenderTexture.ReleaseTemporary(target);
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
