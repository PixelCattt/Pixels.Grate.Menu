using Bark.Extensions;
using Bark.GUI;
using Bark.Networking;
using Bark.Patches;
using Bark.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using NetworkPlayer = NetPlayer;

namespace Bark.Modules.Movement;

public class ShadowWings : BarkModule
{
    private static GameObject? localWings;
    public static string DisplayName = "Shadow Wings";

    protected override void Start()
    {
        base.Start();

        if (localWings == null)
        {
            localWings = Instantiate(Plugin.AssetBundle?.LoadAsset<GameObject>("ShadowWings"), VRRig.LocalRig.transform);
            localWings.transform.localScale = Vector3.one;
        }
        
        localWings.SetActive(false);
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        localWings.SetActive(true);
    }

    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
    {
        if (mod == GetDisplayName() && player != NetworkSystem.Instance.LocalPlayer && player.IsTrusted())
        {
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<NetShadWing>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetShadWing>());
        }
    }

    protected override void Cleanup()
    {
        if (localWings)
            localWings.SetActive(false);
    }

    private void OnRigCached(NetPlayer player, VRRig rig) => rig?.gameObject?.GetComponent<NetShadWing>()?.Obliterate();
    public override string Tutorial() => "Gives you Cool Red Wings.";
    public override string GetDisplayName() => DisplayName;

    private class NetShadWing : MonoBehaviour
    {
        private GameObject netWings;
        private NetworkedPlayer networkedPlayer;
        
        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            netWings = Instantiate(localWings, networkedPlayer.rig.transform);
            netWings.SetActive(true);
        }
        
        private void OnDisable() => netWings.Obliterate();
        private void OnDestroy() => netWings.Obliterate();
    }
}