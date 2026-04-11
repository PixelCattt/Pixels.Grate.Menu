using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GorillaLocomotion;
using Bark.Extensions;
using Bark.Gestures;
using Bark.GUI;
using Bark.Modules.Misc;
using Bark.Patches;
using Bark.Tools;
using Photon.Voice;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;
using UnityEngine.XR;

namespace Bark.Modules.Teleportation;

public class Portal : BarkModule
{
    public static readonly string DisplayName = "Portal Gun";
    public static GameObject launcherPrefab, bluePortal, orangePortal;

    public static ConfigEntry<string> LauncherHand;
    public static ConfigEntry<string> PortalSize;
    public GameObject launcher;
    private readonly Dictionary<int, GameObject> portals = new();
    private AudioSource blueAudio;
    private XRNode hand;
    private AudioSource orangeAudio;
    private ParticleSystem[] smokeSystems;

    private void Awake()
    {
    }


    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            if (!launcherPrefab)
            {
                launcherPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("PortalGun");
                orangePortal = Plugin.AssetBundle.LoadAsset<GameObject>("OrangePortal");
                bluePortal = Plugin.AssetBundle.LoadAsset<GameObject>("BluePortal");
            }

            launcher = Instantiate(launcherPrefab);
            launcher.transform.SetParent(GestureTracker.Instance.rightHand.transform, false);
            launcher.transform.localScale = Vector3.one * 125f;
            launcher.transform.localPosition = new Vector3(-1.5f, 0.2f, 0.1f);
            launcher.transform.localRotation = Quaternion.Euler(2, 10, 0);
            launcher.SetActive(false);
            orangeAudio = launcher.transform.Find("OrangeAudio").GetComponent<AudioSource>();
            blueAudio = launcher.transform.Find("BlueAudio").GetComponent<AudioSource>();
            launcher.transform.Find("PortalBeam").Obliterate();

            ReloadConfiguration();

            smokeSystems = launcher.GetComponentsInChildren<ParticleSystem>();
            foreach (var system in smokeSystems)
                system.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void ShowLauncher(InputTracker _)
    {
        launcher.SetActive(true);
    }

    private void HideLauncher(InputTracker _)
    {
        launcher.SetActive(false);
    }

    private void Fire(int portal)
    {
        if (!launcher.activeSelf) return;
        MakePortal(portal);
    }

    private void MakePortal(int portal)
    {
        var hit = Raycast(launcher.transform.GetChild(0).position, launcher.transform.GetChild(0).transform.forward);
        if (!hit.collider) return;
        MakePortal(hit.point, hit.normal, portal);
    }

    private GameObject MakePortal(Vector3 position, Vector3 normal, int index)
    {
        GameObject portal = null;
        try
        {
            portals[index]?.Obliterate();
            portals.Remove(index);
        }
        catch (Exception e)
        {
        }

        if (index == 0)
        {
            portal = Instantiate(orangePortal);
            orangeAudio.PlayOneShot(orangeAudio.clip, 1f);
            smokeSystems[0].startColor = new Color(255, 160, 0);
        }
        else
        {
            portal = Instantiate(bluePortal);
            blueAudio.PlayOneShot(blueAudio.clip, 1f);
            smokeSystems[0].startColor = new Color(0, 160, 255);
        }

        GestureTracker.Instance.HapticPulse(hand == XRNode.LeftHand, 1, .25f);
        foreach (var system in smokeSystems)
        {
            system.gameObject.SetActive(true);
            system.Clear();
            system.Play();
        }

        portal.transform.position = position;
        Logging.Info("Creating portal with index: " + index);
        portal.transform.LookAt(position + normal);
        portal.transform.position += portal.transform.forward / 100;
        portal.transform.localScale = portal.transform.localScale * GetPortalSize(PortalSize.Value);
        portals.Add(index, portal);
        portal.AddComponent<CollisionObserver>().OnTriggerEntered += (self, collider) =>
        {
            if (collider.gameObject.GetComponentInParent<GTPlayer>() ||
                collider == GestureTracker.Instance.leftPalmInteractor ||
                collider == GestureTracker.Instance.rightPalmInteractor)
                OnPlayerEntered(self, index);
        };
        return portal;
    }

    private float GetPortalSize(string value)
    {
        switch (value)
        {
            default:
                return 1;
            case "small":
                return 0.5f;
            case "normal":
                return 1;
            case "big":
                return 1.5f;
        }
    }

    private void OnPlayerEntered(GameObject inPortal, int portalIndex)
    {
        GameObject outPortal = null;
        if (portalIndex == 1)
            outPortal = portals[0];
        else
            outPortal = portals[1];
        if (!outPortal) return;
        var p = GTPlayer.Instance.RigidbodyVelocity.magnitude;
        TeleportPatch.TeleportPlayer(outPortal.transform.position + outPortal.transform.forward * 1.5f,
            Quaternion.Euler(outPortal.transform.forward).y, false);
        GTPlayer.Instance.SetVelocity(p * outPortal.transform.forward);
    }

    private RaycastHit Raycast(Vector3 origin, Vector3 forward)
    {
        var ray = new Ray(origin, forward);
        RaycastHit hit;

        // Shoot a ray forward
        UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, Teleport.layerMask);
        return hit;
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        UnsubscribeFromEvents();
        launcher?.Obliterate();
        foreach (var portal in portals.Values) portal?.Obliterate();
        portals.Clear();
    }

    protected override void ReloadConfiguration()
    {
        UnsubscribeFromEvents();

        hand = LauncherHand.Value == "left"
            ? XRNode.LeftHand
            : XRNode.RightHand;

        Parent();

        var grip = GestureTracker.Instance.GetInputTracker("grip", hand);
        var primary = GestureTracker.Instance.GetInputTracker("primary", hand);
        var secondary = GestureTracker.Instance.GetInputTracker("secondary", hand);

        grip.OnPressed += ShowLauncher;
        grip.OnReleased += HideLauncher;
        primary.OnPressed += FireA;
        secondary.OnPressed += FireB;
        foreach (var portal in portals.Values)
            portal.transform.localScale =
                new Vector3(0.01384843f, 0.01717813f, 0.01384843f) * GetPortalSize(PortalSize.Value);
    }

    private void FireA(InputTracker _)
    {
        Fire(0);
    }

    private void FireB(InputTracker _)
    {
        Fire(1);
    }

    private void Parent()
    {
        var parent = GestureTracker.Instance.rightHand.transform;
        var position = new Vector3(0.637f, -0.1155f, 3.8735f);
        var rotation = new Vector3(89.7736f, 302.1569f, 208.3616f);
        if (hand == XRNode.LeftHand)
        {
            parent = GestureTracker.Instance.leftHand.transform;
            position = new Vector3(0.637f, -0.1155f, 3.8735f);
            rotation = new Vector3(89.7736f, 302.1569f, 208.3616f);
        }
        //-0.00002

        launcher.transform.SetParent(parent, true);
        launcher.transform.localPosition = position;
        launcher.transform.localRotation = Quaternion.Euler(rotation);
    }

    private void UnsubscribeFromEvents()
    {
        var grip = GestureTracker.Instance.GetInputTracker("grip", hand);
        var primary = GestureTracker.Instance.GetInputTracker("primary", hand);
        var secondary = GestureTracker.Instance.GetInputTracker("secondary", hand);
        grip.OnPressed -= ShowLauncher;
        grip.OnReleased -= HideLauncher;
        primary.OnPressed -= FireA;
        secondary.OnPressed -= FireB;
    }

    public static void BindConfigEntries()
    {
        LauncherHand = Plugin.ConfigFile.Bind(
            DisplayName,
            "launcher hand",
            "right",
            new ConfigDescription(
                "Which hand holds the launcher",
                new AcceptableValueList<string>("left", "right")
            )
        );
        PortalSize = Plugin.ConfigFile.Bind(
            DisplayName,
            "Portal Size",
            "normal",
            new ConfigDescription(
                "The size of the portals",
                new AcceptableValueList<string>("small", "normal", "big")
            )
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        string buttons = LauncherHand.Value == "right" ? "A / B" : "X / Y";

        var h = LauncherHand.Value.ToUpper();
        return $"[{h} GRIP] to grab the Portal-Cannon.\n" +
               $"Use [{buttons}] to Fire the Portals.";
    }
}