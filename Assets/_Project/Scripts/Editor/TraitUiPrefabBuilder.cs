using System;
using System.IO;
using System.Linq;
using ScrapRush.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapRush.Editor
{
    public static class TraitUiPrefabBuilder
    {
        private const string PrefabDirectory = "Assets/_Project/Prefabs/UI";
        private const string RowPrefabPath = PrefabDirectory + "/HUD_TraitRow.prefab";
        private const string RailPrefabPath = PrefabDirectory + "/HUD_TraitRail.prefab";
        private const string TraitArtDirectory = "Assets/_Project/Art/UI/HUD/Trait";
        private const string TraitIconDirectory = "Assets/_Project/Art/Icon/Trait";
        private const string FontPath = "Assets/_Project/Font/LiberationSans SDF - HUD.asset";

        private static readonly TraitSpec[] TraitSpecs =
        {
            new(TraitId.Magnet, "MAGNET", "ICO_TRT_001_Magnet_v01.png"),
            new(TraitId.Electric, "ELECTRIC", "ICO_TRT_002_Electric_v01.png"),
            new(TraitId.Industry, "INDUSTRY", "ICO_TRT_003_Industry_v01.png"),
            new(TraitId.Recycle, "RECYCLE", "ICO_TRT_004_Recycle_v01.png"),
            new(TraitId.Explosion, "EXPLOSION", "ICO_TRT_005_Explosion_v01.png"),
            new(TraitId.Luck, "LUCK", "ICO_TRT_006_Luck_v01.png"),
            new(TraitId.Overload, "OVERLOAD", "ICO_TRT_007_Overload_v01.png")
        };

        [MenuItem("Scrap Rush/UI/Build Trait HUD Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(PrefabDirectory);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Require(font, FontPath);

            Sprite rowFrame = LoadSprite(TraitArtDirectory + "/UI_TRT_001_TraitRow_v01.png", "Compact Row Frame");
            Sprite thresholdEmpty = LoadSprite(TraitArtDirectory + "/UI_TRT_001_TraitRow_v01.png", "Threshold Empty");
            Sprite thresholdFilled = LoadSprite(TraitArtDirectory + "/UI_TRT_001_TraitRow_v01.png", "Threshold Filled");
            Sprite thresholdFocus = LoadSprite(TraitArtDirectory + "/UI_TRT_001_TraitRow_v01.png", "Threshold Focus");
            Sprite virtualGlyph = LoadLargestSprite(TraitArtDirectory + "/UI_TRT_002_VirtualCount_Glyph_v01.png");
            Sprite railFrame = LoadLargestSprite(TraitArtDirectory + "/UI_TRT_004_TraitRail_Frame_v01.png");
            Sprite rowGlow = LoadLargestSprite(TraitArtDirectory + "/UI_TRT_005_RowChanged_Glow_v01.png");
            Sprite thresholdRing = LoadLargestSprite(TraitArtDirectory + "/UI_TRT_005_ThresholdReached_Ring_v01.png");

            BuildRowPrefab(font, rowFrame, thresholdEmpty, thresholdFilled, thresholdFocus, virtualGlyph, rowGlow,
                thresholdRing);
            BuildRailPrefab(font, railFrame);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidatePrefabs();
            Debug.Log($"Trait HUD prefabs built: {RowPrefabPath}, {RailPrefabPath}");
        }

        public static void BuildAndCapture()
        {
            Build();
            CapturePreview();
        }

        private static void BuildRowPrefab(TMP_FontAsset font, Sprite rowFrame, Sprite thresholdEmpty,
            Sprite thresholdFilled, Sprite thresholdFocus, Sprite virtualGlyph, Sprite rowGlow, Sprite thresholdRing)
        {
            GameObject root = CreateUiObject("HUD_TraitRow", null, typeof(CanvasGroup), typeof(Image), typeof(TraitHudRow));
            SetRect(root.GetComponent<RectTransform>(), Vector2.zero, new Vector2(284f, 64f));
            Image rootImage = root.GetComponent<Image>();
            rootImage.sprite = rowFrame;
            rootImage.preserveAspect = false;
            rootImage.raycastTarget = false;

            Image glow = CreateImage("RowChangedGlow", root.transform, rowGlow, new Vector2(0f, 0f),
                new Vector2(294f, 72f));
            glow.color = new Color(1f, 0.72f, 0.12f, 0.85f);
            glow.raycastTarget = false;
            glow.gameObject.SetActive(false);
            glow.transform.SetAsFirstSibling();

            Image icon = CreateImage("TraitIcon", root.transform, null, new Vector2(-112f, 3f), new Vector2(44f, 44f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Image ring = CreateImage("ThresholdReachedRing", root.transform, thresholdRing, new Vector2(-112f, 3f),
                new Vector2(54f, 54f));
            ring.color = new Color(1f, 0.78f, 0.16f, 1f);
            ring.raycastTarget = false;
            ring.gameObject.SetActive(false);

            TMP_Text name = CreateText("TraitName", root.transform, font, "MAGNET", new Vector2(-48f, 8f),
                new Vector2(100f, 27f), 14.5f, TextAlignmentOptions.MidlineLeft, new Color32(224, 229, 226, 255));
            TMP_Text count = CreateText("CountText", root.transform, font, "0/2", new Vector2(30f, 9f),
                new Vector2(60f, 27f), 20f, TextAlignmentOptions.MidlineRight, new Color32(250, 210, 86, 255));
            count.fontStyle = FontStyles.Bold;

            GameObject virtualRoot = CreateUiObject("VirtualCount", root.transform);
            SetRect(virtualRoot.GetComponent<RectTransform>(), new Vector2(92f, 10f), new Vector2(48f, 24f));
            Image virtualIcon = CreateImage("Glyph", virtualRoot.transform, virtualGlyph, new Vector2(-13f, 0f),
                new Vector2(18f, 18f));
            virtualIcon.preserveAspect = true;
            virtualIcon.raycastTarget = false;
            TMP_Text virtualText = CreateText("Value", virtualRoot.transform, font, "+1", new Vector2(10f, 0f),
                new Vector2(28f, 22f), 14f, TextAlignmentOptions.MidlineLeft, new Color32(107, 231, 238, 255));
            virtualText.fontStyle = FontStyles.Bold;
            virtualRoot.SetActive(false);

            GameObject thresholdRoot = CreateUiObject("Thresholds", root.transform);
            SetRect(thresholdRoot.GetComponent<RectTransform>(), new Vector2(50f, -19f), new Vector2(132f, 18f));
            Image[] dots = new Image[4];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = CreateImage($"Threshold_{(i + 1) * 2}", thresholdRoot.transform,
                    i == 0 ? thresholdFocus : thresholdEmpty, new Vector2(-45f + i * 30f, 0f), new Vector2(16f, 16f));
                dots[i].preserveAspect = true;
                dots[i].raycastTarget = false;
            }

            TraitHudRow row = root.GetComponent<TraitHudRow>();
            SerializedObject serialized = new(row);
            serialized.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("traitName").objectReferenceValue = name;
            serialized.FindProperty("countText").objectReferenceValue = count;
            serialized.FindProperty("virtualCountRoot").objectReferenceValue = virtualRoot;
            serialized.FindProperty("virtualCountText").objectReferenceValue = virtualText;
            SetObjectArray(serialized.FindProperty("thresholdDots"), dots);
            serialized.FindProperty("thresholdEmpty").objectReferenceValue = thresholdEmpty;
            serialized.FindProperty("thresholdFilled").objectReferenceValue = thresholdFilled;
            serialized.FindProperty("thresholdFocus").objectReferenceValue = thresholdFocus;
            serialized.FindProperty("rowChangedGlow").objectReferenceValue = glow;
            serialized.FindProperty("thresholdReachedRing").objectReferenceValue = ring;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            row.SetIdentity(TraitId.Magnet, "MAGNET", LoadLargestSprite(TraitIconDirectory + "/ICO_TRT_001_Magnet_v01.png"));
            row.SetState(0, 0);

            PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildRailPrefab(TMP_FontAsset font, Sprite railFrame)
        {
            GameObject rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
            Require(rowPrefab, RowPrefabPath);

            GameObject root = CreateUiObject("HUD_TraitRail", null, typeof(Image), typeof(TraitHudRail));
            SetRect(root.GetComponent<RectTransform>(), Vector2.zero, new Vector2(320f, 580f));
            Image frame = root.GetComponent<Image>();
            frame.sprite = railFrame;
            frame.color = Color.white;
            frame.raycastTarget = false;

            TMP_Text header = CreateText("Header", root.transform, font, "TRAIT SYSTEMS", new Vector2(0f, 264f),
                new Vector2(260f, 34f), 17f, TextAlignmentOptions.Center, new Color32(238, 196, 75, 255));
            header.fontStyle = FontStyles.Bold;
            header.characterSpacing = 5f;

            TraitHudRow[] rows = new TraitHudRow[TraitSpecs.Length];
            for (int i = 0; i < TraitSpecs.Length; i++)
            {
                TraitSpec spec = TraitSpecs[i];
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab);
                instance.name = $"Row_{spec.Id}";
                instance.transform.SetParent(root.transform, false);
                SetRect(instance.GetComponent<RectTransform>(), new Vector2(0f, 210f - i * 70f), new Vector2(284f, 64f));

                TraitHudRow row = instance.GetComponent<TraitHudRow>();
                row.SetIdentity(spec.Id, spec.DisplayName, LoadLargestSprite(TraitIconDirectory + "/" + spec.IconFile));
                row.SetState(0, 0);
                rows[i] = row;
            }

            GameObject toastObject = CreateUiObject("ThresholdToast", root.transform, typeof(CanvasGroup), typeof(Image));
            SetRect(toastObject.GetComponent<RectTransform>(), new Vector2(220f, 215f), new Vector2(360f, 54f));
            Image toastBackground = toastObject.GetComponent<Image>();
            toastBackground.color = new Color32(17, 22, 24, 242);
            toastBackground.raycastTarget = false;
            TMP_Text toastText = CreateText("ToastText", toastObject.transform, font, "MAGNET 6  ·  GRAVITY ENGINE",
                Vector2.zero, new Vector2(336f, 44f), 21f, TextAlignmentOptions.Center, new Color32(255, 219, 96, 255));
            toastText.fontStyle = FontStyles.Bold;
            CanvasGroup toastGroup = toastObject.GetComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastObject.SetActive(false);

            TraitHudRail rail = root.GetComponent<TraitHudRail>();
            SerializedObject serialized = new(rail);
            SetObjectArray(serialized.FindProperty("rows"), rows);
            serialized.FindProperty("toastGroup").objectReferenceValue = toastGroup;
            serialized.FindProperty("toastText").objectReferenceValue = toastText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, RailPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ValidatePrefabs()
        {
            GameObject rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
            GameObject railPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RailPrefabPath);
            Require(rowPrefab, RowPrefabPath);
            Require(railPrefab, RailPrefabPath);
            Require(rowPrefab.GetComponent<TraitHudRow>(), "TraitHudRow component");
            Require(railPrefab.GetComponent<TraitHudRail>(), "TraitHudRail component");
            if (railPrefab.GetComponentsInChildren<TraitHudRow>(true).Length != 7)
                throw new InvalidOperationException("HUD_TraitRail must contain exactly seven TraitHudRow components.");
        }

        private static void CapturePreview()
        {
            const string outputPath = "TraitHudPreview.png";
            GameObject cameraObject = new("TraitPreviewCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(7, 10, 12, 255);
            camera.orthographic = true;
            camera.orthographicSize = 540f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasObject = new("TraitPreviewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject railPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RailPrefabPath);
            GameObject railInstance = (GameObject)PrefabUtility.InstantiatePrefab(railPrefab);
            railInstance.transform.SetParent(canvasObject.transform, false);
            RectTransform railRect = railInstance.GetComponent<RectTransform>();
            railRect.anchorMin = new Vector2(0f, 0.5f);
            railRect.anchorMax = new Vector2(0f, 0.5f);
            railRect.pivot = new Vector2(0f, 0.5f);
            railRect.anchoredPosition = new Vector2(28f, 0f);

            TraitHudRail rail = railInstance.GetComponent<TraitHudRail>();
            rail.SetTraitState(TraitId.Magnet, 5, 0);
            rail.SetTraitState(TraitId.Electric, 3, 1);
            rail.SetTraitState(TraitId.Industry, 2, 0);
            rail.SetTraitState(TraitId.Recycle, 2, 0);
            rail.SetTraitState(TraitId.Explosion, 0, 0);
            rail.SetTraitState(TraitId.Luck, 0, 0);
            rail.SetTraitState(TraitId.Overload, 3, 0);

            RenderTexture target = new(1920, 1080, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in canvasObject.GetComponentsInChildren<TMP_Text>(true))
                text.ForceMeshUpdate(true, true);
            camera.Render();
            RenderTexture.active = target;
            Texture2D screenshot = new(1920, 1080, TextureFormat.RGBA32, false);
            screenshot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            screenshot.Apply();
            File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());

            RenderTexture.active = null;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(screenshot);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(railInstance);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log("Trait HUD preview captured: " + Path.GetFullPath(outputPath));
        }

        private static GameObject CreateUiObject(string name, Transform parent, params Type[] extraComponents)
        {
            Type[] components = new Type[extraComponents.Length + 1];
            components[0] = typeof(RectTransform);
            Array.Copy(extraComponents, 0, components, 1, extraComponents.Length);
            GameObject gameObject = new(name, components) { layer = 5 };
            if (parent != null) gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject gameObject = CreateUiObject(name, parent, typeof(CanvasRenderer), typeof(Image));
            SetRect(gameObject.GetComponent<RectTransform>(), position, size);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value,
            Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject gameObject = CreateUiObject(name, parent, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            SetRect(gameObject.GetComponent<RectTransform>(), position, size);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .FirstOrDefault(candidate => candidate.name == spriteName);
            if (sprite == null) throw new InvalidOperationException($"Sprite '{spriteName}' was not found at {path}.");
            return sprite;
        }

        private static Sprite LoadLargestSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderByDescending(candidate => candidate.rect.width * candidate.rect.height).FirstOrDefault();
            if (sprite == null) throw new InvalidOperationException("No sprite was found at " + path + ".");
            return sprite;
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void Require(UnityEngine.Object value, string label)
        {
            if (value == null) throw new InvalidOperationException("Required asset or component is missing: " + label);
        }

        private readonly struct TraitSpec
        {
            public TraitSpec(TraitId id, string displayName, string iconFile)
            {
                Id = id;
                DisplayName = displayName;
                IconFile = iconFile;
            }

            public TraitId Id { get; }
            public string DisplayName { get; }
            public string IconFile { get; }
        }
    }
}
