using System;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using Bark.Modules.Movement;
using Bark.Tools;
using HarmonyLib;
using UnityEngine;

namespace Bark.Patches;

[HarmonyPatch(typeof(GorillaZipline))]
[HarmonyPatch(nameof(GorillaZipline.Update), MethodType.Normal)]
public class ZiplineUpdatePatch
{
    private static void Postfix(GorillaZipline __instance, BezierSpline ___spline, float ___currentT,
        GorillaHandClimber ___currentClimber)
    {
        try
        {
            var rockets = Rockets.Instance;
            if (!rockets || !rockets.enabled || !___currentClimber) return;
            var curDir = __instance.GetCurrentDirection();
            var rocketDir = rockets.AddedVelocity();
            var currentSpeed = Traverse.Create(__instance).Property("currentSpeed");
            var speedDelta = Vector3.Dot(curDir, rocketDir) * Time.deltaTime * rocketDir.magnitude * 1000f;
            currentSpeed.SetValue(currentSpeed.GetValue<float>() + speedDelta);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}