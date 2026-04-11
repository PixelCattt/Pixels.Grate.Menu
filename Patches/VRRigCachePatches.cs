using System;
using HarmonyLib;

namespace Bark.Patches;

[HarmonyPatch(typeof(VRRigCache), nameof(VRRigCache.OnPlayerLeftRoom))]
public class VRRigCachePatches
{
    public static Action<NetPlayer, VRRig> OnRigCached;

    private static void Postfix(NetPlayer leavingPlayer)
    {
        if (VRRigCache.rigsInUse.TryGetValue(leavingPlayer, out RigContainer container))
        {
            OnRigCached?.Invoke(leavingPlayer, container.Rig);
        }
    }
}