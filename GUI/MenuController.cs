using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Bark.Extensions;
using Bark.Gestures;
using Bark.Interaction;
using Bark.Modules;
using Bark.Modules.Misc;
using Bark.Modules.Movement;
using Bark.Modules.Multiplayer;
using Bark.Modules.Physics;
using Bark.Modules.Teleportation;
using Bark.Tools;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR;
using Player = GorillaLocomotion.GTPlayer;

namespace Bark.GUI;

public class MenuController : BarkGrabbable
{
    public static MenuController? Instance;
    private static InputTracker? _summonTracker;
    private static ConfigEntry<string>? _summonInput;
    private static ConfigEntry<string>? _summonInputHand;
    private static ConfigEntry<string>? _theme;
    public static Material[]? ShinyRocks;

    public static bool Debugger = true;

    public static bool hasCheckedUpdates = false;
    Version remoteVersion = null;

    public Vector3
        initialMenuOffset = new(0, .035f, .65f),
        btnDimensions = new(.3f, .05f, .05f);

    public Rigidbody? rigidbody;
    public List<Transform>? modPages;
    public List<ButtonController>? buttons;
    public List<BarkModule> modules = [];
    public GameObject? modPage, settingsPage;
    public Text? helpText;

    public Renderer? renderer;
    public Material[]? bark, gray, metal, monke;

    private int debugButtons;

    private bool docked;

    private int pageIndex;
    public bool Built { get; private set; }

    protected override void Awake()
    {
        Instance = this;
        try
        {
            base.Awake();
            throwOnDetach = true;
            gameObject.AddComponent<PositionValidator>();
            if (Plugin.ConfigFile != null) Plugin.ConfigFile.SettingChanged += SettingsChanged;
            var tooAddmodules = new List<BarkModule>
            {
                // Locomotion
                gameObject.AddComponent<Airplane>(),
                gameObject.AddComponent<Helicopter>(),
                gameObject.AddComponent<Bubble>(),
                gameObject.AddComponent<Fly>(),
                gameObject.AddComponent<HandFly>(),
                gameObject.AddComponent<GrapplingHooks>(),
                gameObject.AddComponent<Climb>(),
                gameObject.AddComponent<DoubleJump>(),
                gameObject.AddComponent<Platforms>(),
                gameObject.AddComponent<Frozone>(),
                gameObject.AddComponent<NailGun>(),
                gameObject.AddComponent<Rockets>(),
                gameObject.AddComponent<SpeedBoost>(),
                gameObject.AddComponent<Swim>(),
                gameObject.AddComponent<Wallrun>(),
                gameObject.AddComponent<Zipline>(),

                //// Physics
                gameObject.AddComponent<LowGravity>(),
                gameObject.AddComponent<NoClip>(),
                gameObject.AddComponent<NoSlip>(),
                gameObject.AddComponent<Potions>(),
                gameObject.AddComponent<SlipperyHands>(),
                gameObject.AddComponent<DisableWind>(),
                gameObject.AddComponent<UpsideDown>(),

                //// Teleportation
                gameObject.AddComponent<Checkpoint>(),
                gameObject.AddComponent<Portal>(),
                gameObject.AddComponent<Pearl>(),
                gameObject.AddComponent<Teleport>(),

                //// Multiplayer
                gameObject.AddComponent<Boxing>(),
                gameObject.AddComponent<Piggyback>(),
                gameObject.AddComponent<Telekinesis>(),
                gameObject.AddComponent<Grab>(),
                gameObject.AddComponent<Fireflies>(),
                gameObject.AddComponent<ESP>(),
                gameObject.AddComponent<RatSword>()
            };

            // TRUSTED
            var trustedModules = new List<BarkModule>
            {
                gameObject.AddComponent<ShadowWings>(),
                gameObject.AddComponent<GiantHat>(),
                gameObject.AddComponent<TrustedPhone>()
            };

            if (Plugin.LocalPlayerTrusted)
            {
                tooAddmodules.AddRange(trustedModules);
            }

            // DEV
            var devModules = new List<BarkModule>
            {
                gameObject.AddComponent<DeveloperPhone>(),
                gameObject.AddComponent<WaterGun>()
            };

            if (Plugin.LocalPlayerDev)
            {
                tooAddmodules.AddRange(devModules);
            }

            // PIXEL
            var pixelModules = new List<BarkModule>
            {
                gameObject.AddComponent<Cheese>()
            };

            if (Plugin.LocalPlayerPixel)
            {
                tooAddmodules.AddRange(pixelModules);
            }


            modules.AddRange(tooAddmodules);

            ReloadConfiguration();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void Start()
    {
        Summon();
        transform.SetParent(null);
        transform.position = Vector3.zero;
        if (rigidbody != null)
        {
            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;
        }

        transform.SetParent(null);
        AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
        docked = false;
    }

    private void FixedUpdate()
    {
        if (BarkModule.LastEnabled && BarkModule.LastEnabled == Potions.Instance)
            helpText.text = Potions.Instance.Tutorial();
    }

    private void Update()
    {
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (!docked)
            {
                Summon();
            }
            else
            {
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = false;
                    rigidbody.useGravity = true;
                }

                transform.SetParent(null);
                AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
                docked = false;
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Plugin.ConfigFile!.SettingChanged -= SettingsChanged;
    }

    private void ThemeChanged()
    {
        if (Plugin.AssetBundle != null)
        {
            if (!renderer)
                renderer = GetComponent<MeshRenderer>();
            if (Plugin.AssetBundle != null)
            {
                if (bark == null)
                {
                    var outer = Plugin.AssetBundle.LoadAsset<Material>("m_Menu Outer");
                    var inner = Plugin.AssetBundle.LoadAsset<Material>("m_Menu Inner");
                    if (outer && inner) bark = [outer, inner];
                }

                if (metal == null)
                {
                    var zipline = Plugin.AssetBundle.LoadAsset<Material>("Zipline Rope Material");
                    var metalMaterial = Plugin.AssetBundle.LoadAsset<Material>("Metal Material");
                    if (zipline && metalMaterial) metal = [zipline, metalMaterial];
                }

                if (monke == null || gray == null)
                {
                    var baseMat = Plugin.AssetBundle.LoadAsset<Material>("Gorilla Material");
                    if (baseMat)
                    {
                        monke ??= [baseMat, baseMat];

                        gray ??=
                        [
                            new Material(baseMat)
                            {
                                mainTexture = null,
                                color = new Color(0.17f, 0.17f, 0.17f)
                            },
                            new Material(baseMat)
                            {
                                mainTexture = null,
                                color = new Color(0.2f, 0.2f, 0.2f)
                            }
                        ];
                    }
                }
            }

            var themeName = _theme.Value.ToLower();

            switch (themeName)
            {
                case "bark":
                    renderer.materials = bark;
                    break;

                case "gray":
                    renderer.materials = gray;
                    break;

                case "metal":
                    if (metal != null) renderer.materials = metal;
                    break;

                case "player" when VRRig.LocalRig.CurrentCosmeticSkin != null:
                    var skinMat = VRRig.LocalRig.CurrentCosmeticSkin.scoreboardMaterial;
                    renderer.materials = [skinMat, skinMat];
                    break;

                case "player":
                    if (monke != null)
                    {
                        renderer.materials = monke;
                        var playerColor = VRRig.LocalRig.playerColor;
                        monke[0].color = playerColor;
                        monke[1].color = playerColor;
                    }

                    break;
            }
        }
    }

    private void ReloadConfiguration()
    {
        if (_summonTracker != null)
            _summonTracker.OnPressed -= Summon;
        GestureTracker.Instance.OnMeatBeat -= Summon;

        var hand = _summonInputHand.Value == "left"
            ? XRNode.LeftHand
            : XRNode.RightHand;

        if (_summonInput.Value == "gesture")
        {
            GestureTracker.Instance.OnMeatBeat += Summon;
        }
        else
        {
            _summonTracker = GestureTracker.Instance.GetInputTracker(
                _summonInput.Value, hand
            );
            if (_summonTracker != null)
                _summonTracker.OnPressed += Summon;
        }
    }

    private void SettingsChanged(object sender, SettingChangedEventArgs e)
    {
        if (e.ChangedSetting == _summonInput || e.ChangedSetting == _summonInputHand) ReloadConfiguration();
        if (e.ChangedSetting == _theme) ThemeChanged();
    }

    private void Summon(InputTracker _)
    {
        Summon();
    }

    public void Summon()
    {
        if (!Built)
        {
            BuildMenu();
            StartCoroutine(VerCheck());
        }
        else
        {
            ResetPosition();
            StartCoroutine(VerCheck());
        }
    }

    private void ResetPosition()
    {
        rigidbody.isKinematic = true;
        rigidbody.velocity = Vector3.zero;
        transform.SetParent(Player.Instance.bodyCollider.transform);
        transform.localPosition = initialMenuOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        foreach (var button in buttons) button.RemoveBlocker(ButtonController.Blocker.MENU_FALLING);
        docked = true;
    }

    private IEnumerator VerCheck()
    {
        Transform versionCanvas = null;
        float timeout = 5f;
        float timer = 0f;

        while (versionCanvas == null && timer < timeout)
        {
            versionCanvas = transform.Find("Version Canvas");
            timer += Time.deltaTime;
            yield return null;
        }

        if (versionCanvas == null)
        {
            Logging.Warning("Cannot Display Version Number: Version Canvas not Found.");
            yield break;
        }

        var text = versionCanvas.GetComponentInChildren<Text>();

        if (text == null)
        {
            Logging.Warning("Cannot Display Version Number: Version Canvas has no Text Component.");
            yield break;
        }

        text.text = $"{PluginInfo.Name} {PluginInfo.Version}";

        Version localVersion;
        if (!Version.TryParse(PluginInfo.Version, out localVersion))
        {
            Logging.Warning("[UPDATE CHECKER] Invalid Local Version Format.");
            yield break;
        }

        if (hasCheckedUpdates)
        {
            if (remoteVersion != null)
            {
                if (remoteVersion > localVersion)
                {
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    text.text = "!!! Update Needed !!!";
                }
            }

            yield break;
        }

        using (var request = UnityWebRequest.Get("https://api.github.com/repos/PixelCattt/Pixel-Bark/releases/latest"))
        {
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Logging.Warning("[UPDATE CHECKER] Error: " + request.error);
                yield break;
            }

            var json = request.downloadHandler.text;
            const string key = "\"tag_name\": \"";
            var start = json.IndexOf(key, StringComparison.Ordinal);

            if (start == -1)
            {
                Logging.Warning("[UPDATE CHECKER] tag_name not found in Response.");
                yield break;
            }

            start += key.Length;
            var end = json.IndexOf("\"", start, StringComparison.Ordinal);

            if (end == -1)
            {
                Logging.Warning("[UPDATE CHECKER] Invalid JSON Formatting.");
                yield break;
            }

            var tag = json.Substring(start, end - start);

            if (tag.StartsWith("v"))
                tag = tag.Substring(1);

            if (!Version.TryParse(tag, out remoteVersion))
            {
                Logging.Warning("[UPDATE CHECKER] Invalid Remote Version Format.");
                yield break;
            }

            if (remoteVersion > localVersion)
            {
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.text = "!!! Update Needed !!!";

                Process.Start("https://github.com/PixelCattt/Pixel-Bark/releases");
            }

            hasCheckedUpdates = true;
        }
    }

    private void BuildMenu()
    {
        Logging.Debug("Building menu...");
        try
        {
            helpText = gameObject.transform.Find("Help Canvas").GetComponentInChildren<Text>();
            helpText.transform.parent.localPosition = new Vector3(0, -0.0731f, -0.1f);
            helpText.transform.parent.localRotation = Quaternion.Euler(0, 180f, 0);
            helpText.text = "Click a Module to see its Tutorial.";
            var collider = gameObject.GetOrAddComponent<BoxCollider>();
            collider.isTrigger = true;
            rigidbody = gameObject.GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;

            SetupInteraction();
            SetupModPages();
            SetupSettingsPage();

            transform.SetParent(Player.Instance.bodyCollider.transform);
            ResetPosition();
            Logging.Debug("Build successful.");
            ReloadConfiguration();
            ThemeChanged();
        }
        catch (Exception ex)
        {
            Logging.Warning(ex.Message);
            Logging.Warning(ex.StackTrace);
            return;
        }

        Built = true;
    }

    private void SetupSettingsPage()
    {
        var button = gameObject.transform.Find("Settings Button").gameObject;
        var btnController = button.AddComponent<ButtonController>();
        buttons.Add(btnController);
        btnController.OnPressed += (obj, pressed) =>
        {
            settingsPage.SetActive(pressed);
            if (pressed)
                settingsPage.GetComponent<SettingsPage>().UpdateText();
            modPage.SetActive(!pressed);
        };

        settingsPage = transform.Find("Settings Page").gameObject;
        settingsPage.AddComponent<SettingsPage>();
        settingsPage.SetActive(false);
    }

    public void SetupModPages()
    {
        var modPageTemplate = gameObject.transform.Find("Mod Page");
        var buttonsPerPage = modPageTemplate.childCount - 2; // Excludes the Page Switching Buttons
        var numPages = (modules.Count - 1) / buttonsPerPage + 1;
        if (Plugin.DebugMode)
            numPages++;

        modPages = new List<Transform> { modPageTemplate };
        for (var i = 0; i < numPages - 1; i++)
            modPages.Add(Instantiate(modPageTemplate, gameObject.transform));

        buttons = new List<ButtonController>();
        for (var i = 0; i < modules.Count; i++)
        {
            var module = modules[i];

            var page = modPages[i / buttonsPerPage];
            var button = page.Find($"Button {i % buttonsPerPage}").gameObject;

            var btnController = button.AddComponent<ButtonController>();
            buttons.Add(btnController);
            btnController.OnPressed += (obj, pressed) =>
            {
                module.enabled = pressed;
                if (pressed)
                    helpText.text = module.GetDisplayName().ToUpper() +
                                    "\n\n" +
                                    module.Tutorial().ToUpper();
            };
            module.button = btnController;
            btnController.SetText(module.GetDisplayName().ToUpper());
        }

        AddDebugButtons();

        foreach (var modPage in modPages)
        {
            foreach (Transform button in modPage)
                if (button.name == "Button Left" && modPage != modPages[0])
                {
                    var btnController = button.gameObject.AddComponent<ButtonController>();
                    btnController.OnPressed += PreviousPage;
                    btnController.SetText("Prev Page");
                    buttons.Add(btnController);
                }
                else if (button.name == "Button Right" && modPage != modPages[modPages.Count - 1])
                {
                    var btnController = button.gameObject.AddComponent<ButtonController>();
                    btnController.OnPressed += NextPage;
                    btnController.SetText("Next Page");
                    buttons.Add(btnController);
                }
                else if (!button.GetComponent<ButtonController>())
                {
                    button.gameObject.SetActive(false);
                }

            modPage.gameObject.SetActive(false);
        }

        modPageTemplate.gameObject.SetActive(true);
        modPage = modPageTemplate.gameObject;
    }

    private void AddDebugButtons()
    {
        AddDebugButton("Debug Log", (btn, isPressed) =>
        {
            Debugger = isPressed;
            Logging.Debug("Debugger", Debugger ? "active" : "inactive");
            Plugin.DebugText.text = "";
        });

        AddDebugButton("Close game", (btn, isPressed) =>
        {
            Debugger = isPressed;
            if (btn.text.text == "You sure?")
                Application.Quit();
            else
                btn.text.text = "You sure?";
        });

        AddDebugButton("Show Colliders", (btn, isPressed) =>
        {
            if (isPressed)
                foreach (var c in FindObjectsOfType<Collider>())
                    c.gameObject.AddComponent<ColliderRenderer>();
            else
                foreach (var c in FindObjectsOfType<ColliderRenderer>())
                    c.Obliterate();
        });
    }

    private void AddDebugButton(string title, Action<ButtonController, bool> onPress)
    {
        if (!Plugin.DebugMode) return;
        var page = modPages.Last();
        var button = page.Find($"Button {debugButtons}").gameObject;
        var btnController = button.gameObject.AddComponent<ButtonController>();
        btnController.OnPressed += onPress;
        btnController.SetText(title);
        buttons.Add(btnController);
        debugButtons++;
    }

    public void PreviousPage(ButtonController button, bool isPressed)
    {
        button.IsPressed = false;
        pageIndex--;
        for (var i = 0; i < modPages.Count; i++) modPages[i].gameObject.SetActive(i == pageIndex);
        modPage = modPages[pageIndex].gameObject;
    }

    public void NextPage(ButtonController button, bool isPressed)
    {
        button.IsPressed = false;
        pageIndex++;
        for (var i = 0; i < modPages.Count; i++) modPages[i].gameObject.SetActive(i == pageIndex);
        modPage = modPages[pageIndex].gameObject;
    }

    public void SetupInteraction()
    {
        throwOnDetach = true;
        priority = 100;
        OnSelectExit += (_, __) =>
        {
            AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
            docked = false;
        };
        OnSelectEnter += (_, __) => { RemoveBlockerFromAllButtons(ButtonController.Blocker.MENU_FALLING); };
    }

    public Material GetMaterial(string name)
    {
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            var _name = renderer.material.name.ToLower();
            if (_name.Contains(name)) return renderer.material;
        }

        return null;
    }

    public void AddBlockerToAllButtons(ButtonController.Blocker blocker)
    {
        foreach (var button in buttons) button.AddBlocker(blocker);
    }

    public void RemoveBlockerFromAllButtons(ButtonController.Blocker blocker)
    {
        foreach (var button in buttons) button.RemoveBlocker(blocker);
    }

    public static void BindConfigEntries()
    {
        try
        {
            var inputDesc = new ConfigDescription(
                "Which button you press to open the menu",
                new AcceptableValueList<string>("gesture", "stick", "a/x", "b/y")
            );
            _summonInput = Plugin.ConfigFile.Bind("General",
                "open menu",
                "gesture",
                inputDesc
            );

            var handDesc = new ConfigDescription(
                "Which hand can open the menu",
                new AcceptableValueList<string>("left", "right")
            );
            _summonInputHand = Plugin.ConfigFile.Bind("General",
                "open hand",
                "right",
                handDesc
            );

            var ThemeDesc = new ConfigDescription(
                "Which Theme Should Bark Use?",
                new AcceptableValueList<string>("bark", "gray", "metal", "player")
            );
            _theme = Plugin.ConfigFile.Bind("General",
                "theme",
                "bark",
                ThemeDesc
            );
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}