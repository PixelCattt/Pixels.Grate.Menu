using System;
using Bark.Extensions;
using Bark.Gestures;
using Bark.GUI;
using Bark.Networking;
using Bark.Patches;
using Bark.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Bark.Modules.Misc;

public class DeveloperPhone : BarkModule
{
    private static readonly string DisplayName = "Dev Phone";
    private static GameObject? _phone;

    protected override void Start()
    {
        base.Start();
        if (_phone == null)
        {
            _phone = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("DEVPHONE"));
            _phone.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
            _phone.transform.localPosition = new Vector3(-1.5f, 0.2f, 0.1f);
            _phone.transform.localRotation = Quaternion.Euler(2, 10, 0);
            _phone.transform.localScale /= 2;
        }

        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
        _phone?.SetActive(false);
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            GestureTracker.Instance.rightGrip.OnPressed += OnGripPressed;
            GestureTracker.Instance.rightGrip.OnReleased += OnGripReleased;

            _phone?.SetActive(false);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnGripPressed(InputTracker tracker)
    {
        _phone?.SetActive(true);
    }

    private void OnGripReleased(InputTracker tracker)
    {
        _phone?.SetActive(false);
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool modEnabled)
    {
        if (mod != DisplayName || !player.IsDev()) return;
        if (modEnabled)
            player.Rig()?.gameObject.GetOrAddComponent<NetPhone>();
        else
            Destroy(player.Rig()?.gameObject.GetComponent<NetPhone>());
    }

    protected override void Cleanup()
    {
        _phone?.SetActive(false);

        if (GestureTracker.Instance != null)
        {
            GestureTracker.Instance.rightGrip.OnPressed -= OnGripPressed;
            GestureTracker.Instance.rightGrip.OnReleased -= OnGripReleased;
        }
    }

    private static void OnRigCached(NetworkPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<NetPhone>()?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "[RIGHT GRIP] to equip your Phone, Dev Moke.";
    }

    private class NetPhone : MonoBehaviour
    {
        private NetworkedPlayer? networkedPlayer;
        private GameObject? phone;

        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            phone = Instantiate(_phone, rightHand, false);

            if (phone == null)
                return;

            phone.transform.localPosition = new Vector3(0.0992f, 0.06f, 0.02f);
            phone.transform.localRotation = Quaternion.Euler(270, 163.12f, 0);
            Vector3 localScale = phone.transform.localScale / 20;
            localScale.y = 54f;
            phone.transform.localScale = localScale;

            phone.SetActive(false);

            networkedPlayer.OnGripPressed += OnGripPressed;
            networkedPlayer.OnGripReleased += OnGripReleased;
        }

        private void OnDisable()
        {
            phone?.Obliterate();

            if (networkedPlayer != null)
            {
                networkedPlayer.OnGripPressed -= OnGripPressed;
                networkedPlayer.OnGripReleased -= OnGripReleased;
            }
        }

        private void OnDestroy()
        {
            phone?.Obliterate();

            if (networkedPlayer != null)
            {
                networkedPlayer.OnGripPressed -= OnGripPressed;
                networkedPlayer.OnGripReleased -= OnGripReleased;
            }
        }

        private void OnGripPressed(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) phone?.SetActive(true);
        }

        private void OnGripReleased(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) phone?.SetActive(false);
        }
    }
}