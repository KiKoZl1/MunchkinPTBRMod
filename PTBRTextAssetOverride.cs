using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(MunchkinPTBRMod.PTBRTextAssetOverride), "Munchkin PT-BR", "1.3.2", "KiKoZl")]
[assembly: MelonGame(null, "Munchkin Digital")]

namespace MunchkinPTBRMod
{
    public class PTBRTextAssetOverride : MelonMod
    {
        private const string TwitterUrl = "https://x.com/K1K000ZL";
        private const string EmbeddedResourceName = "MunchkinPTBRMod.ptbr_es_override.txt";
        private const string EmbeddedLogoFileName = "ptbr_logo.png";
        private const string ExternalLogoFileName = "ptbr_logo.png";
        private const float Margin = 24f;
        private const float LogoMaxWidth = 128f;
        private const float LogoMaxHeight = 128f;
        private const float FirstShowDelaySeconds = 7f;
        private const float HoverScale = 1.06f;
        private const float PressScale = 0.94f;
        private const float InteractionLerpSpeed = 16f;
        private static readonly Color IdleLogoColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        private static readonly Color HoverLogoColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color PressLogoColor = new Color(0.78f, 0.78f, 0.78f, 1f);

        private static string? _overrideText;
        private static bool _sawTargetTextAsset;
        private static bool _firstOverlayShown;
        private static float _firstOverlayGateStart = -1f;
        private static bool _overlayEnabled = true;
        private static bool _targetHookLogged;
        private static string _lastSceneName = string.Empty;

        private static Texture2D? _logoTexture;
        private static Sprite? _logoSprite;
        private static bool _logoLoadAttempted;
        private static bool _logoMissingLogged;

        private static GameObject? _overlayRoot;
        private static Image? _logoImage;
        private static RectTransform? _logoRect;
        private static bool _isHoveringLogo;
        private static bool _isPressingLogo;

        public override void OnInitializeMelon()
        {
            LoadEmbeddedTranslation();
            HarmonyInstance.PatchAll();

            try
            {
                _lastSceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            }
            catch
            {
                _lastSceneName = string.Empty;
            }

            PrintDiag("init");
            MelonLogger.Msg("[PTBR] Mod initialized (Canvas overlay).");
        }

        public override void OnUpdate()
        {
            try
            {
                var sceneName = SceneManager.GetActiveScene().name;
                if (!string.Equals(sceneName, _lastSceneName, StringComparison.Ordinal))
                {
                    _lastSceneName = sceneName;
                    PrintDiag("scene_changed_poll");
                }

                if (Input.GetKeyDown(KeyCode.F9))
                {
                    _overlayEnabled = !_overlayEnabled;
                    ApplyOverlayVisibility();
                    MelonLogger.Msg($"[PTBR] Overlay: {(_overlayEnabled ? "ON" : "OFF")}");
                    PrintDiag("overlay_toggle");
                }

                if (Input.GetKeyDown(KeyCode.F10))
                {
                    PrintDiag("manual_debug");
                }

                if (_sawTargetTextAsset)
                {
                    TryCreateOrUpdateOverlay();
                    UpdateLogoInteractionAndEffects();
                }
            }
            catch
            {
                // nunca derrubar o jogo por erro de overlay
            }
        }

        private static void UpdateLogoInteractionAndEffects()
        {
            if (!_overlayEnabled || !ShouldShowOverlay() || !IsUnityAlive(_overlayRoot) || !_overlayRoot!.activeSelf || !IsUnityAlive(_logoRect) || !IsUnityAlive(_logoImage))
            {
                EaseBackToIdle();
                return;
            }

            bool isOverLogo = RectTransformUtility.RectangleContainsScreenPoint(_logoRect, Input.mousePosition, null);
            _isHoveringLogo = isOverLogo;

            if (Input.GetMouseButtonDown(0) && isOverLogo)
                _isPressingLogo = true;

            if (_isPressingLogo && Input.GetMouseButtonUp(0))
            {
                if (isOverLogo)
                {
                    Application.OpenURL(TwitterUrl);
                    MelonLogger.Msg($"[PTBR] Opening link: {TwitterUrl}");
                }

                _isPressingLogo = false;
            }

            if (_isPressingLogo && !Input.GetMouseButton(0))
                _isPressingLogo = false;

            float targetScale = _isPressingLogo ? PressScale : (_isHoveringLogo ? HoverScale : 1f);
            Color targetColor = _isPressingLogo ? PressLogoColor : (_isHoveringLogo ? HoverLogoColor : IdleLogoColor);
            ApplyLogoVisualState(targetScale, targetColor);
        }

        private static void EaseBackToIdle()
        {
            _isHoveringLogo = false;
            _isPressingLogo = false;
            ApplyLogoVisualState(1f, IdleLogoColor);
        }

        private static void ApplyLogoVisualState(float targetScale, Color targetColor)
        {
            if (!IsUnityAlive(_logoRect) || !IsUnityAlive(_logoImage))
                return;

            float t = 1f - Mathf.Exp(-InteractionLerpSpeed * Time.unscaledDeltaTime);
            var targetVec = new Vector3(targetScale, targetScale, 1f);
            _logoRect!.localScale = Vector3.Lerp(_logoRect.localScale, targetVec, t);
            _logoImage!.color = Color.Lerp(_logoImage.color, targetColor, t);
        }

        private static void TryCreateOrUpdateOverlay()
        {
            EnsureLogoTexture();
            if (!IsUnityAlive(_logoTexture))
            {
                ApplyOverlayVisibility();
                return;
            }

            if (!IsUnityAlive(_overlayRoot))
            {
                CreateOverlayUI();
            }

            if (!IsUnityAlive(_overlayRoot) || !IsUnityAlive(_logoImage) || !IsUnityAlive(_logoRect))
            {
                ApplyOverlayVisibility();
                return;
            }

            EnsureLogoSprite();
            if (IsUnityAlive(_logoSprite))
            {
                if (_logoImage!.sprite != _logoSprite)
                    _logoImage.sprite = _logoSprite;
            }

            UpdateLogoLayout();
            ApplyOverlayVisibility();
        }

        private static void CreateOverlayUI()
        {
            try
            {
                _overlayRoot = new GameObject("PTBR_OverlayRoot");
                UnityEngine.Object.DontDestroyOnLoad(_overlayRoot);

                var canvas = _overlayRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;

                var scaler = _overlayRoot.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                _overlayRoot.AddComponent<GraphicRaycaster>();

                var logoGo = new GameObject("PTBR_Logo");
                logoGo.transform.SetParent(_overlayRoot.transform, false);
                _logoImage = logoGo.AddComponent<Image>();
                _logoImage.raycastTarget = false;
                _logoRect = _logoImage.rectTransform;

                if (!IsUnityAlive(_logoRect))
                {
                    MelonLogger.Error("[PTBR] Failed to get RectTransform for PTBR_Logo.");
                    return;
                }

                _logoRect!.anchorMin = new Vector2(0f, 0f);
                _logoRect.anchorMax = new Vector2(0f, 0f);
                _logoRect.pivot = new Vector2(0f, 0f);
                _logoRect.anchoredPosition = new Vector2(Margin, Margin);
                _logoRect.sizeDelta = new Vector2(LogoMaxWidth, LogoMaxHeight);
                _logoRect.localScale = Vector3.one;
                _logoImage.color = IdleLogoColor;

                MelonLogger.Msg("[PTBR] Canvas overlay created.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[PTBR] CreateOverlayUI failed: " + ex);
            }
        }

        private static void UpdateLogoLayout()
        {
            if (!IsUnityAlive(_logoRect) || !IsUnityAlive(_logoTexture))
                return;

            float srcW = Math.Max(1f, _logoTexture!.width);
            float srcH = Math.Max(1f, _logoTexture.height);
            float scale = Math.Min(LogoMaxWidth / srcW, LogoMaxHeight / srcH);
            if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
                scale = 1f;

            float drawW = srcW * scale;
            float drawH = srcH * scale;
            _logoRect!.sizeDelta = new Vector2(drawW, drawH);
            _logoRect.anchoredPosition = new Vector2(Margin, Margin);
        }

        private static void EnsureLogoTexture()
        {
            if (IsUnityAlive(_logoTexture))
                return;

            if (_logoLoadAttempted)
                return;

            _logoLoadAttempted = true;

            if (TryLoadEmbeddedLogo(out var embeddedTex, out var embeddedName))
            {
                _logoTexture = embeddedTex;
                MelonLogger.Msg($"[PTBR] Logo loaded from embedded resource: {embeddedName} ({_logoTexture!.width}x{_logoTexture.height})");
                return;
            }

            if (TryLoadExternalLogo(out var externalTex, out var loadedPath))
            {
                _logoTexture = externalTex;
                MelonLogger.Msg($"[PTBR] Logo loaded from file: {loadedPath} ({_logoTexture!.width}x{_logoTexture.height})");
                return;
            }

            if (!_logoMissingLogged)
            {
                _logoMissingLogged = true;
                MelonLogger.Warning($"[PTBR] Logo PNG not found. Add '{ExternalLogoFileName}' beside the mod DLL or embed 'assets/{ExternalLogoFileName}'.");
            }
        }

        private static void EnsureLogoSprite()
        {
            if (!IsUnityAlive(_logoTexture))
                return;

            if (IsUnityAlive(_logoSprite))
                return;

            try
            {
                _logoSprite = Sprite.Create(
                    _logoTexture!,
                    new Rect(0f, 0f, _logoTexture!.width, _logoTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                _logoSprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[PTBR] Failed creating logo sprite: " + ex.Message);
            }
        }

        private static bool TryLoadEmbeddedLogo(out Texture2D? texture, out string resourceName)
        {
            texture = null;
            resourceName = string.Empty;

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var resource = asm
                    .GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith("." + EmbeddedLogoFileName, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(resource))
                    return false;

                using var stream = asm.GetManifestResourceStream(resource);
                if (stream == null)
                    return false;

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                if (!TryDecodePng(ms.ToArray(), out texture))
                    return false;

                resourceName = resource;
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[PTBR] Embedded logo load failed: " + ex.Message);
                return false;
            }
        }

        private static bool TryLoadExternalLogo(out Texture2D? texture, out string loadedPath)
        {
            texture = null;
            loadedPath = string.Empty;

            try
            {
                var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                string[] candidates =
                {
                    Path.Combine(asmDir, ExternalLogoFileName),
                    Path.Combine(Environment.CurrentDirectory, ExternalLogoFileName),
                    Path.Combine(Environment.CurrentDirectory, "Mods", ExternalLogoFileName)
                };

                foreach (var path in candidates)
                {
                    if (!File.Exists(path))
                        continue;

                    var bytes = File.ReadAllBytes(path);
                    if (!TryDecodePng(bytes, out texture))
                        continue;

                    loadedPath = path;
                    return true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[PTBR] External logo load failed: " + ex.Message);
            }

            return false;
        }

        private static bool TryDecodePng(byte[] pngBytes, out Texture2D? texture)
        {
            texture = null;

            try
            {
                if (pngBytes == null || pngBytes.Length == 0)
                    return false;

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, pngBytes, false))
                {
                    UnityEngine.Object.Destroy(tex);
                    return false;
                }

                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
                texture = tex;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void LoadEmbeddedTranslation()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream(EmbeddedResourceName);

                if (stream == null)
                {
                    _overrideText = null;
                    MelonLogger.Error($"[PTBR] Embedded resource NOT FOUND: {EmbeddedResourceName}");
                    MelonLogger.Error("[PTBR] Check if ptbr_es_override.txt is included as <EmbeddedResource> in the csproj.");
                    return;
                }

                using var reader = new StreamReader(stream, new UTF8Encoding(false));
                _overrideText = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(_overrideText))
                    MelonLogger.Warning("[PTBR] Embedded translation loaded but is EMPTY.");
                else
                    MelonLogger.Msg($"[PTBR] Embedded translation loaded: {_overrideText.Length} chars");
            }
            catch (Exception ex)
            {
                _overrideText = null;
                MelonLogger.Error("[PTBR] Failed loading embedded translation: " + ex);
            }
        }

        private static bool ShouldOverride(TextAsset ta)
        {
            if (ta == null) return false;

            var name = (ta.name ?? string.Empty).Trim();
            if (name.Equals("es_ES", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("es_es", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("es-ES", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("es", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.IndexOf("es_es", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        [HarmonyPatch(typeof(TextAsset), "get_text")]
        private static class Patch_TextAsset_get_text
        {
            private static bool Prefix(TextAsset __instance, ref string __result)
            {
                try
                {
                    if (_overrideText == null) return true;
                    if (__instance == null) return true;
                    if (!ShouldOverride(__instance)) return true;

                    __result = _overrideText;
                    _sawTargetTextAsset = true;
                    if (_firstOverlayGateStart < 0f)
                        _firstOverlayGateStart = Time.realtimeSinceStartup;

                    if (!_targetHookLogged)
                    {
                        _targetHookLogged = true;
                        PrintDiag("target_hooked");
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("[PTBR] get_text Prefix exception: " + ex);
                    return true;
                }
            }
        }

        private static bool ShouldShowOverlay()
        {
            if (!_firstOverlayShown && _firstOverlayGateStart >= 0f)
            {
                float elapsed = Time.realtimeSinceStartup - _firstOverlayGateStart;
                if (elapsed < FirstShowDelaySeconds)
                    return false;
            }

            return _overlayEnabled && _sawTargetTextAsset && IsMenuScene(_lastSceneName);
        }

        private static void ApplyOverlayVisibility()
        {
            if (!IsUnityAlive(_overlayRoot))
                return;

            bool visible = ShouldShowOverlay() && IsUnityAlive(_logoTexture);
            if (_overlayRoot!.activeSelf != visible)
                _overlayRoot.SetActive(visible);

            if (visible && !_firstOverlayShown)
            {
                _firstOverlayShown = true;
                MelonLogger.Msg("[PTBR] First logo display unlocked.");
            }

            if (!visible)
                EaseBackToIdle();
        }

        private static bool IsMenuScene(string? sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return true;
            var s = sceneName.Trim().ToLowerInvariant();
            string[] menuTokens = { "menu", "main", "title", "lobby", "startup", "splash", "front", "home" };
            return menuTokens.Any(t => s.Contains(t));
        }

        private static bool IsUnityAlive(UnityEngine.Object? obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            try
            {
                return obj != null;
            }
            catch
            {
                return false;
            }
        }

        private static string GetModVersion()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var info = asm.GetCustomAttributes(typeof(MelonInfoAttribute), false)
                    .Cast<MelonInfoAttribute>()
                    .FirstOrDefault();
                return info?.Version ?? "?.?.?";
            }
            catch
            {
                return "?.?.?";
            }
        }

        private static void PrintDiag(string phase)
        {
            try
            {
                var unity = Application.unityVersion;
                var melon = typeof(MelonUtils).Assembly.GetName().Version?.ToString() ?? "unknown";
                var target = _sawTargetTextAsset ? "es_ES(found)" : "es_ES(not_yet)";
                var overrideState = _overrideText != null ? "embedded_ok" : "embedded_missing";
                var overlay = !IsUnityAlive(_overlayRoot)
                    ? "canvas_not_created"
                    : (_overlayRoot!.activeSelf ? "canvas_visible" : "canvas_hidden");
                var logo = IsUnityAlive(_logoTexture) ? $"png_loaded_{_logoTexture!.width}x{_logoTexture.height}" : "png_missing";
                var gate = _firstOverlayShown
                    ? "first_show_done"
                    : (_firstOverlayGateStart < 0f
                        ? "first_show_wait_hook"
                        : $"first_show_wait_{Math.Max(0f, FirstShowDelaySeconds - (Time.realtimeSinceStartup - _firstOverlayGateStart)):0.0}s");

                MelonLogger.Msg(
                    $"PTBRMOD_DIAG v{GetModVersion()} | phase={phase} | unity={unity} | melon={melon} | target={target} | override={overrideState} | overlay={overlay} | logo={logo} | gate={gate} | scene={_lastSceneName} | twitter={TwitterUrl}"
                );
            }
            catch
            {
            }
        }
    }
}
