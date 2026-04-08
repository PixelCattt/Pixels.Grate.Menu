using System;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Misc;

public class WaterGun : GrateModule
{
    public static string DisplayName = "Water Gun";
    private static GameObject WaterGunObj;

    protected override void Start()
    {
        base.Start();
        if (WaterGunObj == null)
        {
            WaterGunObj = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("WaterGun"));
            WaterGunObj.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
            WaterGunObj.transform.localPosition = new Vector3(-0.35f, 0.25f, 0.5f);
            WaterGunObj.transform.localRotation = Quaternion.Euler(-90, 270, 180);
            WaterGunObj.transform.localScale = WaterGunObj.transform.localScale * 0.125f;
        }

        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
        WaterGunObj.SetActive(false);
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            GestureTracker.Instance.rightGrip.OnPressed += OnGripPressed;
            GestureTracker.Instance.rightGrip.OnReleased += OnGripReleased;

            WaterGunObj.SetActive(false);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnGripPressed(InputTracker tracker)
    {
        WaterGunObj?.SetActive(true);
    }

    private void OnGripReleased(InputTracker tracker)
    {
        WaterGunObj?.SetActive(false);
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
    {
        if (mod == DisplayName && player != NetworkSystem.Instance.LocalPlayer && player.IsDev())
        {
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<NetWaterGun>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetWaterGun>());
        }
    }

    protected override void Cleanup()
    {
        WaterGunObj?.SetActive(false);

        if (GestureTracker.Instance != null)
        {
            GestureTracker.Instance.rightGrip.OnPressed -= OnGripPressed;
            GestureTracker.Instance.rightGrip.OnReleased -= OnGripReleased;
        }
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<NetWaterGun>()?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Water Gun equipped.";
    }

    private class NetWaterGun : MonoBehaviour
    {
        private GameObject waterGun;
        private NetworkedPlayer networkedPlayer;

        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            waterGun = Instantiate(WaterGunObj);

            waterGun.transform.SetParent(rightHand);
            waterGun.transform.localPosition = new Vector3(0.0992f, 0.06f, 0.02f);
            waterGun.transform.localRotation = Quaternion.Euler(2, 100, 180);
            waterGun.transform.localScale = waterGun.transform.localScale * 0.5f;

            waterGun.SetActive(false);

            networkedPlayer.OnGripPressed += OnGripPressed;
            networkedPlayer.OnGripReleased += OnGripReleased;
        }

        private void OnDisable()
        {
            waterGun.Obliterate();

            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
        }

        private void OnDestroy()
        {
            waterGun.Obliterate();

            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
        }

        private void OnGripPressed(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) waterGun.SetActive(true);
        }

        private void OnGripReleased(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) waterGun.SetActive(false);
        }
    }
}