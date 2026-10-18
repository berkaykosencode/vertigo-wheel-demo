using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using VertigoWheelDemo.Data;
using VertigoWheelDemo.Gameplay;
using VertigoWheelDemo.UI;
using Object = UnityEngine.Object;

namespace VertigoWheelDemo.Editor
{
    // Editor-only construction: the shipped UI is an ordinary, editable prefab.
    public static class WheelPresentationBuilder
    {
        private const string Art = "Assets/Art/UI/";
        private const string PrefabPath = "Assets/Prefabs/game_wheel.prefab";
        private static TMP_FontAsset font;
        private static readonly Color White = new Color32(234, 240, 246, 255);
        private static readonly Color Muted = new Color32(142, 161, 181, 255);
        private static readonly Color Gold = new Color32(255, 207, 121, 255);

        [MenuItem("Tools/Vertigo/Apply Presentation")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before updating the prefab.");

            PrepareArt();
            var rewards = PrepareRewards();
            PrepareWheels(rewards);
            font = PrepareFont();
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);
                var controller = root.GetComponentInChildren<GameSessionController>(true);
                // Keep the existing prefab root, canvas and controller identities.
                foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
                    Object.DestroyImmediate(child.gameObject);

                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

                RectTransform background = Rect("ui_background", canvas.transform, Vector2.zero, Vector2.zero);
                Stretch(background);
                var backdrop = background.gameObject.AddComponent<WheelBackdropGraphic>();
                backdrop.raycastTarget = false;

                var glow = Image("ui_image_wheel_glow_value", canvas.transform, Vector2.zero, new Vector2(1100, 1100), "star_glow_alpha", new Color(1, 0.6f, 0.2f, 0.16f));
                Label("ui_text_brand", canvas.transform, new Vector2(-715, 465), new Vector2(300, 40), "VERTIGO", 29, White, TextAlignmentOptions.Left, true).characterSpacing = 7;
                Label("ui_text_brand_caption", canvas.transform, new Vector2(-715, 430), new Vector2(300, 30), "WHEEL DEMO", 15, Muted, TextAlignmentOptions.Left).characterSpacing = 5;
                Label("ui_text_title", canvas.transform, new Vector2(0, 455), new Vector2(650, 70), "ŞANS ÇARKI", 49, White, TextAlignmentOptions.Center, true).characterSpacing = 5;
                var zone = Label("ui_text_zone_value", canvas.transform, new Vector2(0, 385), new Vector2(400, 40), "BÖLGE 01", 25, White, TextAlignmentOptions.Center, true);
                var kind = Label("ui_text_zone_kind_value", canvas.transform, new Vector2(0, 346), new Vector2(400, 30), "NORMAL ÇARK", 17, Gold, TextAlignmentOptions.Center, true);
                kind.characterSpacing = 4;

                var previous = Preview(canvas.transform, "previous", -625, 0.42f);
                var next = Preview(canvas.transform, "next", 625, 0.72f);

                var currentRoot = Rect("ui_wheel_current", canvas.transform, new Vector2(0, -25), new Vector2(650, 650));
                var currentVisual = Rect("ui_wheel_current_animated", currentRoot, Vector2.zero, new Vector2(650, 650));
                var currentGroup = currentVisual.gameObject.AddComponent<CanvasGroup>();
                Image wheel = Image("ui_image_spin_bronze", currentVisual, Vector2.zero, new Vector2(650, 650), "ui_spin_bronze_base", Color.white);
                var spinView = wheel.gameObject.AddComponent<WheelSpinView>();
                var contentView = wheel.gameObject.AddComponent<WheelContentView>();
                var rewardImages = new Image[8];
                var rewardTexts = new TMP_Text[8];
                var bronze = Wheel("bronze");
                var initialSlots = new WheelRewardSelector(new System.Random(2026)).Select(bronze);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4;
                    Vector2 position = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 195;
                    rewardImages[i] = Image($"ui_image_reward_{i:00}_value", wheel.transform, position + new Vector2(0, 8), new Vector2(100, 74), null, Color.white);
                    rewardImages[i].sprite = initialSlots[i].Reward.Icon;
                    rewardTexts[i] = Label($"ui_text_reward_amount_{i:00}_value", rewardImages[i].transform, new Vector2(0, -42), new Vector2(120, 30), initialSlots[i].Amount.ToString(), 23, White, TextAlignmentOptions.Center, true);
                    rewardTexts[i].gameObject.SetActive(initialSlots[i].Reward.Type != RewardType.Bomb);
                }
                Image indicator = Image("ui_image_spin_indicator_value", currentVisual, new Vector2(0, 295), new Vector2(58, 80), "ui_spin_bronze_indicator", Color.white);
                Set(contentView, "wheelDefinition", bronze, "indicatorImage", indicator, "rewardImages", rewardImages, "rewardAmountTexts", rewardTexts);
                Set(controller, "wheelSpinView", spinView, "wheelContentView", contentView);

                // The button is a sibling of the rotating wheel, so it stays fixed.
                var spin = Button("ui_button_spin", canvas.transform, new Vector2(0, -25), new Vector2(176, 176), "ui_spin_generic_button", null);
                spin.targetGraphic.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
                var spinGroup = spin.gameObject.AddComponent<CanvasGroup>();
                Set(spin.gameObject.AddComponent<SpinButtonView>(), "sessionController", controller);

                var transition = canvas.gameObject.GetComponent<WheelTransitionView>() ?? canvas.gameObject.AddComponent<WheelTransitionView>();
                Set(transition, "sessionController", controller, "currentVisual", currentVisual, "currentGroup", currentGroup,
                    "spinButtonGroup", spinGroup, "amountTexts", rewardTexts, "previousWheel", previous, "nextWheel", next);

                var cashOut = Button("ui_button_cashout", canvas.transform, new Vector2(0, -417), new Vector2(360, 84), "UI_button_grey_standard", "ÖDÜLLERİ AL");
                Set(cashOut.gameObject.AddComponent<CashOutButtonView>(), "sessionController", controller);
                var restart = Button("ui_button_restart", canvas.transform, new Vector2(0, -417), new Vector2(360, 84), "UI_button_orange_standard", "YENİDEN BAŞLAT");
                Set(restart.gameObject.AddComponent<RestartButtonView>(), "sessionController", controller);
                restart.gameObject.SetActive(false);

                var bank = Button("ui_button_bank", canvas.transform, new Vector2(710, 445), new Vector2(280, 92), "UI_button_grey_standard", null);
                Image("ui_image_bank_icon", bank.targetGraphic.transform, new Vector2(-87, 0), new Vector2(90, 52), "UI_icon_chest_silver_nolight", Color.white);
                Label("ui_text_bank", bank.targetGraphic.transform, new Vector2(40, 15), new Vector2(150, 35), "BANKA", 25, White, TextAlignmentOptions.Center, true);
                var count = Label("ui_text_bank_count_value", bank.targetGraphic.transform, new Vector2(40, -20), new Vector2(160, 25), "0 ÇEŞİT ÖDÜL", 13, White);
                var hint = Label("ui_text_hint_value", canvas.transform, new Vector2(0, -493), new Vector2(1500, 40), "Bomba tüm kazançlarını siler. Her 5. bölgede güvenle ayrılabilirsin.", 21, Muted);

                BuildBank(canvas.transform, controller, bank);
                BuildReveal(canvas.transform, controller);

                var hud = canvas.gameObject.GetComponent<WheelHudView>() ?? canvas.gameObject.AddComponent<WheelHudView>();
                Set(hud, "sessionController", controller, "previousWheel", previous, "nextWheel", next,
                    "zoneText", zone, "zoneKindText", kind, "hintText", hint, "bankCountText", count,
                    "cashOutImage", cashOut.targetGraphic, "cashOutText", cashOut.GetComponentInChildren<TMP_Text>(),
                    "availableButton", Sprite("UI_button_orange_standard"), "unavailableButton", Sprite("UI_button_grey_standard"),
                    "restartButton", restart.gameObject, "wheelGlow", glow);

                // Editor preview: use the same selector and layout as the runtime.
                previous.Show(null, null, 0);
                next.Show(bronze, new WheelRewardSelector(new System.Random(2027)).Select(bronze), 2);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            UpdateAtlas();
            AssetDatabase.SaveAssets();
            Debug.Log("VERTIGO: Presentation updated. All supplied reward sprites are configured in the tier pools.");
        }

        private static TMP_FontAsset PrepareFont()
        {
            const string folder = "Assets/Art/Fonts";
            const string path = folder + "/Wheel UI SDF.asset";
            Directory.CreateDirectory(folder);
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null) return asset;
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = "Wheel UI SDF";
            string characters = "";
            for (int i = 32; i < 127; i++) characters += (char)i;
            characters += "ÇĞİÖŞÜçğıöşü×·‹›…";
            asset.TryAddCharacters(characters, out string missing);
            if (!string.IsNullOrEmpty(missing)) throw new Exception("Font is missing: " + missing);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void PrepareArt()
        {
            foreach (string path in Directory.GetFiles(Art, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                string name = Path.GetFileNameWithoutExtension(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = name == "star_flash_alpha" ? 1024 : 2048;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);
                bool scalable = name.StartsWith("ui_card_") && !name.Contains("icon") || name.StartsWith("UI_button_");
                importer.spriteBorder = scalable ? new Vector4(12, 12, 12, 12) : Vector4.zero;
                if (name == "ui_card_panel_zone_bg" || name.Contains("panel_zone_"))
                    importer.spriteBorder = new Vector4(4, 4, 4, 4);
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, RewardDefinition> PrepareRewards()
        {
            Directory.CreateDirectory("Assets/GameData/Rewards");
            // filename | stable id | visible name | type | tier (0 common, 1 silver, 2 golden)
            string[] definitions =
            {
                "UI_icon_gold|gold|Altın|0|0", "UI_icon_cash|cash|Para|1|0",
                "ui_card_icon_death|bomb|Bomba|3|0",
                "UI_Icons_Armor_Points|armor_points|Zırh puanı|2|0",
                "UI_Icons_Knife_Points|knife_points|Bıçak puanı|2|0",
                "UI_Icons_Pistol_Points|pistol_points|Tabanca puanı|2|0",
                "UI_Icons_Pistol_Points_|pistol_points_elite|Elit tabanca puanı|2|1",
                "UI_Icons_Rifle_Points|rifle_points|Tüfek puanı|2|0",
                "UI_Icons_SMG_Points|smg_points|SMG puanı|2|0",
                "UI_Icons_Shotgun_Points|shotgun_points|Pompalı puanı|2|0",
                "UI_Icons_Sniper_Points|sniper_points|Keskin nişancı puanı|2|1",
                "UI_Icons_Submachine_Points|submachine_points|Makineli tüfek puanı|2|0",
                "UI_Icons_Vest_Points|vest_points|Yelek puanı|2|0",
                "UI_icon_chest_Bronze_nolight|bronze_chest|Bronz sandık|2|0",
                "UI_icon_chest_small_noligt|small_chest|Küçük sandık|2|0",
                "UI_icon_chest_standart_nolight|standard_chest|Standart sandık|2|0",
                "UI_icon_chest_big_nolight|big_chest|Büyük sandık|2|1",
                "UI_icon_chest_silver_nolight|silver_chest|Gümüş sandık|2|1",
                "UI_icon_chest_gold_nolight|gold_chest|Altın sandık|2|2",
                "UI_icon_chest_super_nolight|super_chest|Süper sandık|2|2",
                "UI_Icon_Renders_tier1_shotgun|shotgun|Taktik pompalı|2|1",
                "UI_Icon_Renders_tier2_mle|knife|Özel bıçak|2|1",
                "UI_Icon_Renders_tier2_rifle|rifle|Özel tüfek|2|1",
                "UI_Icon_Renders_tier3_shotgun|elite_shotgun|Elit pompalı|2|2",
                "UI_Icon_Renders_tier3_smg|elite_smg|Elit SMG|2|2",
                "UI_Icon_Renders_tier3_sniper|elite_sniper|Elit keskin nişancı|2|2",
                "ui_icon_aviator_glasses_easter|glasses|Pilot gözlüğü|2|1",
                "ui_icon_baseball_cap_easter|cap|Özel şapka|2|1",
                "ui_icon_helmet_pumpkin|helmet|Balkabağı miğferi|2|2",
                "ui_icon_mle_bayonet_easter_time|easter_bayonet|Bahar süngüsü|2|2",
                "ui_icon_mle_bayonet_summer_vice|summer_bayonet|Yaz süngüsü|2|2",
                "ui_icon_render_cons_grenade_m26|grenade_m26|M26 el bombası|2|0",
                "ui_icon_render_cons_grenade_m67|grenade_m67|M67 el bombası|2|0",
                "ui_icon_render_cons_healthshot_2_neurostim|neurostim|Neurostim|2|0",
                "ui_icon_render_cons_healthshot_2_regenerator|regenerator|Yenileyici|2|0",
                "ui_icon_render_t_cons_molotov|molotov|Molotof|2|0"
            };

            var result = new Dictionary<string, RewardDefinition>();
            foreach (string line in definitions)
            {
                string[] d = line.Split('|');
                string path = $"Assets/GameData/Rewards/reward_{d[1]}.asset";
                var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
                if (reward == null)
                {
                    reward = ScriptableObject.CreateInstance<RewardDefinition>();
                    AssetDatabase.CreateAsset(reward, path);
                }
                var so = new SerializedObject(reward);
                so.FindProperty("rewardId").stringValue = d[1];
                so.FindProperty("displayName").stringValue = d[2];
                so.FindProperty("rewardType").enumValueIndex = int.Parse(d[3]);
                so.FindProperty("icon").objectReferenceValue = Sprite(d[0]);
                so.ApplyModifiedPropertiesWithoutUndo();
                result.Add(d[1], reward);
                tiers[d[1]] = int.Parse(d[4]);
            }
            return result;
        }

        private static readonly Dictionary<string, int> tiers = new Dictionary<string, int>();

        private static void PrepareWheels(Dictionary<string, RewardDefinition> rewards)
        {
            string[] names = { "bronze", "silver", "golden" };
            for (int tier = 0; tier < names.Length; tier++)
            {
                var wheel = Wheel(names[tier]);
                var so = new SerializedObject(wheel);
                so.FindProperty("baseSprite").objectReferenceValue = Sprite($"ui_spin_{names[tier]}_base");
                so.FindProperty("indicatorSprite").objectReferenceValue = Sprite($"ui_spin_{names[tier]}_indicator");
                var pool = rewards.Values.Where(reward => reward.Type != RewardType.Bomb &&
                    (reward.Type != RewardType.Item || tiers[reward.RewardId] <= tier) &&
                    // Super zones concentrate on special rewards rather than common consumables.
                    (tier < 2 || reward.Type != RewardType.Item || tiers[reward.RewardId] >= 2)).ToList();
                SerializedProperty slots = so.FindProperty("slots");
                slots.arraySize = 8;
                for (int i = 0; i < 8; i++)
                {
                    RewardDefinition reward = tier == 0 && i == 4 ? rewards["bomb"] : pool[i % pool.Count];
                    SetSlot(slots.GetArrayElementAtIndex(i), reward, BaseAmount(reward, tier));
                }
                SerializedProperty entries = so.FindProperty("rewardPool");
                entries.arraySize = pool.Count;
                for (int i = 0; i < pool.Count; i++)
                    SetSlot(entries.GetArrayElementAtIndex(i), pool[i], BaseAmount(pool[i], tier));
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static int BaseAmount(RewardDefinition reward, int tier)
        {
            if (reward.Type == RewardType.Bomb) return 0;
            if (reward.Type == RewardType.Gold) return new[] { 75, 300, 1000 }[tier];
            if (reward.Type == RewardType.Cash) return new[] { 200, 800, 2500 }[tier];
            if (reward.RewardId.Contains("points")) return new[] { 5, 20, 60 }[tier];
            return new[] { 1, 2, 4 }[tier];
        }

        private static void SetSlot(SerializedProperty slot, RewardDefinition reward, int amount)
        {
            slot.FindPropertyRelative("reward").objectReferenceValue = reward;
            slot.FindPropertyRelative("amount").intValue = amount;
        }

        private static WheelPreviewView Preview(Transform parent, string suffix, float x, float alpha)
        {
            RectTransform root = Rect($"ui_preview_{suffix}_value", parent, new Vector2(x, -105), new Vector2(350, 400));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            group.blocksRaycasts = false;
            var animated = Rect($"ui_preview_{suffix}_animated", root, Vector2.zero, new Vector2(310, 310));
            const float previewRatio = 310f / 650f;
            Image image = Image($"ui_image_{suffix}_wheel_value", animated, Vector2.zero, new Vector2(310, 310), "ui_spin_bronze_base", Color.white);
            Image pointer = Image($"ui_image_{suffix}_indicator_value", animated, new Vector2(0, 295 * previewRatio), new Vector2(58, 80) * previewRatio, "ui_spin_bronze_indicator", Color.white);
            var images = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                images[i] = Image($"ui_image_{suffix}_reward_{i:00}_value", image.transform,
                    new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 93 + new Vector2(0, 8 * previewRatio), new Vector2(100, 74) * previewRatio, "UI_icon_gold", Color.white);
            }
            Label($"ui_text_{suffix}_marker", root, new Vector2(0, -196), new Vector2(160, 35), suffix == "previous" ? "‹" : "›", 36, Muted);
            var label = Label($"ui_text_{suffix}_zone_value", root, new Vector2(0, 216), new Vector2(300, 45), "BÖLGE 02", 22, White, TextAlignmentOptions.Center, true);
            var view = root.gameObject.AddComponent<WheelPreviewView>();
            Set(view, "wheelImage", image, "indicatorImage", pointer, "rewardImages", images, "zoneText", label, "animatedVisual", animated);
            return view;
        }

        private static void BuildBank(Transform parent, GameSessionController controller, Button open)
        {
            var overlay = Image("ui_panel_bank_overlay", parent, Vector2.zero, Vector2.zero, null, new Color(0.01f, 0.025f, 0.045f, 0.93f));
            Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
            var panel = Image("ui_panel_bank_animated", overlay.transform, Vector2.zero, new Vector2(1180, 820), "ui_card_panel_zone_bg", new Color32(48, 66, 85, 255));
            Frame(panel.transform, new Vector2(1180, 820));
            Image("ui_image_bank_chest", panel.transform, new Vector2(-470, 300), new Vector2(150, 80), "UI_icon_chest_gold_nolight", Color.white);
            Label("ui_text_bank_title", panel.transform, new Vector2(-155, 314), new Vector2(450, 60), "OTURUM BANKASI", 36, Gold, TextAlignmentOptions.Left, true);
            var summary = Label("ui_text_bank_summary_value", panel.transform, new Vector2(-35, 263), new Vector2(700, 50), "Güvenli bölgede ödüllerini alabilirsin.", 19, Muted, TextAlignmentOptions.Left);
            var close = Button("ui_button_bank_close", panel.transform, new Vector2(480, 310), new Vector2(100, 70), "UI_button_grey_standard", "KAPAT");
            close.GetComponentInChildren<TMP_Text>().fontSize = 15;

            var viewport = Image("ui_bank_viewport", panel.transform, new Vector2(0, -32), new Vector2(1040, 530), null, new Color(0, 0, 0, 0.01f));
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("ui_bank_content_value", viewport.transform, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10; layout.padding = new RectOffset(0, 0, 4, 4);
            layout.childControlHeight = true; layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
            var template = Image("ui_bank_row_template", content, Vector2.zero, new Vector2(1040, 104), "ui_card_panel_zone_coming", new Color32(28, 44, 61, 255));
            template.maskable = true;
            var element = template.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = 104;
            var icon = Image("ui_image_bank_reward_value", template.transform, new Vector2(-424, 0), new Vector2(150, 82), null, Color.white);
            var name = Label("ui_text_bank_reward_name_value", template.transform, new Vector2(-45, 0), new Vector2(560, 70), "", 26, White, TextAlignmentOptions.Left, true);
            var amount = Label("ui_text_bank_reward_amount_value", template.transform, new Vector2(397, 0), new Vector2(190, 70), "", 29, Gold, TextAlignmentOptions.Right, true);
            icon.maskable = true; name.maskable = true; amount.maskable = true;
            var row = template.gameObject.AddComponent<BankRewardRowView>();
            Set(row, "iconImage", icon, "nameText", name, "amountText", amount);
            template.gameObject.SetActive(false);
            var empty = Label("ui_text_bank_empty_value", panel.transform, new Vector2(0, -20), new Vector2(850, 140), "Bankan henüz boş.\nÇarkı çevirerek ilk ödülünü kazan.", 29, Muted);
            Label("ui_text_bank_footer", panel.transform, new Vector2(0, -353), new Vector2(1000, 40), "Banka bu oturumu gösterir. Bomba çıkarsa biriken ödüller kaybolur.", 18, Muted);
            var view = overlay.gameObject.AddComponent<BankView>();
            Set(view, "sessionController", controller, "openButton", open, "closeButton", close,
                "overlay", group, "animatedPanel", panel.rectTransform, "content", content, "rowTemplate", row,
                "emptyText", empty, "summaryText", summary, "scrollRect", scroll);
        }

        private static void BuildReveal(Transform parent, GameSessionController controller)
        {
            var overlay = Image("ui_panel_reward_reveal", parent, Vector2.zero, Vector2.zero, null, new Color(0.015f, 0.025f, 0.045f, 0.94f));
            Stretch(overlay.rectTransform); overlay.raycastTarget = true;
            var button = overlay.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
            button.targetGraphic = overlay;
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
            var animated = Rect("ui_reward_reveal_animated", overlay.transform, Vector2.zero, new Vector2(1100, 900));
            var flash = Image("ui_image_reward_star_flash", animated, Vector2.zero, new Vector2(850, 850), "star_flash_alpha", new Color(1, 0.78f, 0.35f, 0.38f));
            var glow = Image("ui_image_reward_star_glow", animated, Vector2.zero, new Vector2(590, 590), "star_glow_alpha", new Color(1, 0.7f, 0.3f, 0.8f));
            var icon = Image("ui_image_reward_reveal_value", animated, new Vector2(0, 15), new Vector2(450, 300), "UI_icon_chest_super_nolight", Color.white);
            var title = Label("ui_text_reward_reveal_title_value", animated, new Vector2(0, 333), new Vector2(1000, 70), "ÖDÜLÜNÜ KAZANDIN", 41, Gold, TextAlignmentOptions.Center, true);
            title.characterSpacing = 4;
            var amount = Label("ui_text_reward_reveal_amount_value", animated, new Vector2(0, -190), new Vector2(800, 100), "× 1", 66, White, TextAlignmentOptions.Center, true);
            var name = Label("ui_text_reward_reveal_name_value", animated, new Vector2(0, -280), new Vector2(1100, 70), "Süper sandık", 32, White, TextAlignmentOptions.Center, true);
            Label("ui_text_reward_reveal_dismiss", animated, new Vector2(0, -395), new Vector2(1000, 50), "DEVAM ETMEK İÇİN EKRANA DOKUN", 19, Muted).characterSpacing = 3;
            var view = overlay.gameObject.AddComponent<RewardRevealView>();
            Set(view, "sessionController", controller, "dismissButton", button, "overlay", group,
                "animatedContent", animated, "starFlash", flash.rectTransform, "glowImage", glow,
                "rewardImage", icon, "titleText", title, "rewardNameText", name, "amountText", amount);
        }

        private static void Frame(Transform parent, Vector2 size)
        {
            Image frame = Image("ui_image_panel_frame", parent, Vector2.zero, size, "ui_card_frame_4px_zone", new Color32(82, 107, 129, 255));
            frame.type = UnityEngine.UI.Image.Type.Sliced;
        }

        private static Button Button(string name, Transform parent, Vector2 position, Vector2 size, string sprite, string text)
        {
            RectTransform root = Rect(name, parent, position, size);
            Image visual = Image(name + "_visual", root, Vector2.zero, size, sprite, Color.white);
            visual.type = UnityEngine.UI.Image.Type.Sliced;
            visual.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = visual;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1, 0.96f, 0.84f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f); colors.disabledColor = new Color(0.55f, 0.58f, 0.64f, 0.6f);
            colors.fadeDuration = 0.12f; button.colors = colors;
            Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            if (text != null)
                Label(name.Replace("button", "text"), visual.transform, Vector2.zero, size - new Vector2(20, 6), text, 23,
                    sprite.Contains("orange") ? new Color32(39, 28, 13, 255) : White, TextAlignmentOptions.Center, true);
            Set(root.gameObject.AddComponent<ButtonPressFeedback>(), "visual", visual.rectTransform);
            return button;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, string sprite, Color color)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite == null ? null : Sprite(sprite);
            image.color = color; image.preserveAspect = true;
            image.raycastTarget = false; image.maskable = false;
            if (sprite != null && (sprite.StartsWith("ui_card_") && !sprite.Contains("icon")))
            { image.type = UnityEngine.UI.Image.Type.Sliced; image.preserveAspect = false; }
            return image;
        }

        private static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text,
            float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool bold = false)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = fontSize;
            label.color = color; label.alignment = alignment;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            label.raycastTarget = false; label.maskable = false;
            return label;
        }

        private static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
            if (sprite == null) throw new InvalidOperationException("Missing supplied sprite: " + name);
            return sprite;
        }

        private static WheelDefinition Wheel(string name) =>
            AssetDatabase.LoadAssetAtPath<WheelDefinition>($"Assets/GameData/Wheels/wheel_{name}.asset");

        private static void Set(Object target, params object[] pairs)
        {
            var so = new SerializedObject(target);
            for (int i = 0; i < pairs.Length; i += 2)
            {
                var property = so.FindProperty((string)pairs[i]);
                if (pairs[i + 1] is Object[] array)
                {
                    property.arraySize = array.Length;
                    for (int j = 0; j < array.Length; j++) property.GetArrayElementAtIndex(j).objectReferenceValue = array[j];
                }
                else property.objectReferenceValue = (Object)pairs[i + 1];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateAtlas()
        {
            string path = "Assets/Art/Atlases/atlas_ui_wheel.spriteatlasv2";
            SpriteAtlasAsset asset = SpriteAtlasAsset.Load(path);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas != null)
                asset.Remove(atlas.GetPackables());
            Object[] textures = Directory.GetFiles(Art, "*.png").OrderBy(file => file)
                .Select(file => (Object)AssetDatabase.LoadAssetAtPath<Texture2D>(file)).ToArray();
            asset.Add(textures);
            SpriteAtlasAsset.Save(asset, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
