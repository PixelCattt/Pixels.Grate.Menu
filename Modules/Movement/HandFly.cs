using BepInEx.Configuration;
using GorillaLocomotion;
using Bark.Gestures;
using Bark.GUI;
using UnityEngine;
using UnityEngine.XR;

namespace Bark.Modules.Movement;

public class HandFly : BarkModule
{
    public static string DisplayName = "Hand Fly";

    private static ConfigEntry<int>? Speed;

    private bool leftTrigger;
    private bool rightTrigger;

    private float SpeedScale => Speed!.Value * 2.5f + 10f;

    private void FixedUpdate()
    {
        var rb = GorillaTagger.Instance.rigidbody;

        bool any = leftTrigger || rightTrigger;
        bool both = leftTrigger && rightTrigger;

        if (!any)
            return;

        float multiplier = both ? 2f : 1f;

        Vector3 dir = GetFlyDirection();

        rb.velocity = Vector3.zero;

        rb.MovePosition(rb.position + dir * (SpeedScale * multiplier * Time.fixedDeltaTime));
    }

    private Vector3 GetFlyDirection()
    {
        Vector3 dir = Vector3.zero;

        if (leftTrigger)
        {
            var left = GetTrueHandPosition(true);
            dir += left.forward;
        }

        if (rightTrigger)
        {
            var right = GetTrueHandPosition(false);
            dir += right.forward;
        }

        if (dir == Vector3.zero)
            return GTPlayer.Instance.bodyCollider.transform.forward;

        return dir.normalized;
    }

    public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right)
    GetTrueHandPosition(bool left)
    {
        Transform controllerTransform = left
            ? GorillaTagger.Instance.leftHandTransform
            : GorillaTagger.Instance.rightHandTransform;

        GTPlayer.HandState handState = left
            ? GTPlayer.Instance.LeftHand
            : GTPlayer.Instance.RightHand;

        Quaternion rot = controllerTransform.rotation * handState.handRotOffset;

        Vector3 pos =
            controllerTransform.position +
            controllerTransform.rotation *
            (handState.handOffset * GTPlayer.Instance.scale);

        return (
            pos,
            rot,
            rot * Vector3.up,
            rot * Vector3.forward,
            rot * Vector3.right
        );
    }

    public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right)
        GetTrueRightHand() => GetTrueHandPosition(false);

    public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right)
        GetTrueLeftHand() => GetTrueHandPosition(true);

    protected override void OnEnable()
    {
        base.OnEnable();

        var left = GestureTracker.Instance.GetInputTracker("trigger", XRNode.LeftHand);
        var right = GestureTracker.Instance.GetInputTracker("trigger", XRNode.RightHand);

        left.OnPressed += _ => leftTrigger = true;
        left.OnReleased += _ => leftTrigger = false;

        right.OnPressed += _ => rightTrigger = true;
        right.OnReleased += _ => rightTrigger = false;
    }

    protected override void Cleanup()
    {
        leftTrigger = false;
        rightTrigger = false;
    }

    public override string GetDisplayName() => DisplayName;

    public override string Tutorial()
    {
        return "[TRIGGER] to Fly in the direction your Hand is Pointing at.\n" +
               "Press both [TRIGGERS] for more Speed.";
    }

    public static void BindConfigEntries()
    {
        Speed = Plugin.ConfigFile.Bind(
            DisplayName,
            "speed",
            5,
            "Flight Speed"
        );
    }
}