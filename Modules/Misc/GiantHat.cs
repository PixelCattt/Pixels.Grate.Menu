using GorillaLocomotion;
using Bark.Extensions;
using Bark.GUI;
using Bark.Networking;
using Bark.Patches;
using Bark.Tools;
using UnityEngine;

namespace Bark.Modules.Misc
{
    public class GiantHat : BarkModule
    {
        private static GameObject Hat;
        
        protected override void Start()
        {
            base.Start();
            
            if (Hat == null)
            {
                Hat = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("goudabuda"), GTPlayer.Instance.headCollider.transform);
                Hat.name = "Giant Hat";
                
                Hat.transform.localPosition = new Vector3(0f, 1f, 0f);
                Hat.transform.localRotation = Quaternion.Euler(300f, 180f, 180f);
            }
            
            Hat.SetActive(false);
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            VRRigCachePatches.OnRigCached += OnRigCached;
        }
        
        private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
        {
            if (mod == GetDisplayName() && player != NetworkSystem.Instance.LocalPlayer && player.IsTrusted())
            {
                if (enabled)
                    player.Rig().gameObject.GetOrAddComponent<GiantHat>();
                else
                    Destroy(player.Rig().gameObject.GetComponent<GiantHat>());
            }
        }
        
        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            Hat.SetActive(true);
        }

        protected override void Cleanup()
        {
            if (Hat)
                Hat.SetActive(false);
        }

        private void OnRigCached(NetPlayer player, VRRig rig) => rig?.gameObject?.GetComponent<GiantHat>()?.Obliterate();
        public override string GetDisplayName() => "Giant Hat";
        public override string Tutorial() => "Gives you a Cool Big Hat.";

        private class NetGiantHat : MonoBehaviour
        {
            private GameObject netHat;
            private NetworkedPlayer networkedPlayer;

            private void OnEnable()
            {
                networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
                Transform head = networkedPlayer.rig.headMesh.transform;

                netHat = Instantiate(Hat, head);
                netHat.name = "Giant Networked Hat";
                
                netHat.transform.localPosition = new Vector3(0f, 1f, 0f);
                netHat.transform.localRotation = Quaternion.Euler(300f, 180f, 180f);

                netHat.SetActive(true);
            }

            private void OnDisable() => netHat.Obliterate();
            private void OnDestroy() => netHat.Obliterate();
        }
    }
}