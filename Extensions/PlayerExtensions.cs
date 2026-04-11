using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using Bark.Modules;
using Bark.Tools;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Bark.Extensions;

public static class PlayerExtensions
{
    private static readonly HashSet<string> PixelIds = new()
    {
        "1F677B8C11A839B6" // PIXELCATT
    };

    private static readonly HashSet<string> DeveloperIds = new()
    {
        "NO-ONE-HERE-YET-HEHEHEHEHE" // NO-ONE-HERE-YET-HEHEHEHEHE
    };

    private static readonly HashSet<string> TrustedIds = new()
    {
        "NO-ONE-HERE-YET-HEHEHEHEHE" // NO-ONE-HERE-YET-HEHEHEHEHE
    };

    // Use Plugin.localPlayerPixel for checking if the local player is PixelCatt
    public static bool IsPixel(this NetPlayer player)
    {
        return PixelIds.Contains(player.UserId);
    }

    // Use Plugin.localPlayerDev for checking if the local player is a Dev
    public static bool IsDev(this NetPlayer player)
    {
        return (player.IsPixel() || DeveloperIds.Contains(player.UserId));
    }

    // Use Plugin.localPlayerTrusted for checking if the local player is Trusted
    public static bool IsTrusted(this NetPlayer player)
    {
        return (player.IsDev() || TrustedIds.Contains(player.UserId));
    }

    public static void AddForce(this GTPlayer self, Vector3 v)
    {
        self.GetComponent<Rigidbody>().velocity += v;
    }

    public static void SetVelocity(this GTPlayer self, Vector3 v)
    {
        self.GetComponent<Rigidbody>().velocity = v;
    }

    public static PhotonView PhotonView(this VRRig rig)
    {
        return Traverse.Create(rig).Field("photonView").GetValue<PhotonView>();
    }

    public static bool HasProperty(this VRRig rig, string key)
    {
        return rig?.OwningNetPlayer?.HasProperty(key) ?? false;
    }

    public static bool ModuleEnabled(this VRRig rig, string mod)
    {
        return rig?.OwningNetPlayer?.ModuleEnabled(mod) ?? false;
    }

    public static T GetProperty<T>(this NetPlayer? player, string key)
    {
        return (T)player?.GetPlayerRef().CustomProperties[key];
    }

    public static bool HasProperty(this NetPlayer player, string key)
    {
        return player?.GetPlayerRef().CustomProperties.ContainsKey(key) ?? false;
    }

    public static bool ModuleEnabled(this NetPlayer player, string mod)
    {
        if (!player.HasProperty(BarkModule.enabledModulesKey)) return false;

        var enabledMods = player.GetProperty<Dictionary<string, bool>>(BarkModule.enabledModulesKey);
        return enabledMods.TryGetValue(mod, out var enabled) && enabled;
    }

    public static VRRig? Rig(this NetPlayer? player)
    {
        return VRRigCache.ActiveRigs.FirstOrDefault(rig => rig.OwningNetPlayer == player);
    }
}