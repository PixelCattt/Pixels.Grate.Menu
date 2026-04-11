using System;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Bark.Extensions;
using Bark.Gestures;
using Bark.GUI;
using Bark.Modules.Physics;
using Bark.Tools;
using UnityEngine;

namespace Bark.Modules.Movement;

public class Climb : BarkModule
{
    public static readonly string DisplayName = "Climb";
    public GameObject climbableLeft, climbableRight;
    private InputTracker<float>? leftGrip;
    private Transform leftHand, rightHand;
    private InputTracker<float>? rightGrip;

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            leftGrip = GestureTracker.Instance.leftGrip;
            rightGrip = GestureTracker.Instance.rightGrip;

            leftHand = GestureTracker.Instance.leftHand.transform;
            rightHand = GestureTracker.Instance.rightHand.transform;
            climbableLeft = CreateClimbable(leftGrip);
            climbableRight = CreateClimbable(rightGrip);
            ReloadConfiguration();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }

        Plugin.MenuController.GetComponent<Potions>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    public GameObject CreateClimbable(InputTracker<float>? grip)
    {
        var climbable = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        climbable.name = "Bark Climb Obj";
        climbable.AddComponent<GorillaClimbable>();
        climbable.layer = LayerMask.NameToLayer("GorillaInteractable");
        climbable.GetComponent<Renderer>().enabled = false;
        climbable.transform.localScale = Vector3.one * .15f * GTPlayer.Instance.scale;
        climbable.SetActive(false);
        grip.OnPressed += OnGrip;
        grip.OnReleased += OnRelease;
        return climbable;
    }

    public void OnGrip(InputTracker tracker)
    {
        if (enabled)
        {
            GameObject climbable;
            Transform hand;
            if (tracker == leftGrip)
            {
                climbable = climbableLeft;
                hand = leftHand;
            }
            else
            {
                climbable = climbableRight;
                hand = rightHand;
            }

            Collider[] colliders = UnityEngine.Physics.OverlapSphere(
                hand.position,
                0.15f,
                GTPlayer.Instance.locomotionEnabledLayers
            );

            if (colliders.Length > 0)
            {
                climbable.transform.position = hand.position;
                climbable.SetActive(true);
            }
        }
    }

    public void OnRelease(InputTracker tracker)
    {
        if (tracker == GestureTracker.Instance.leftGrip)
            climbableLeft.SetActive(false);
        else
            climbableRight.SetActive(false);
    }

    protected override void Cleanup()
    {
        climbableLeft?.Obliterate();
        climbableRight?.Obliterate();
        if (leftGrip != null)
        {
            leftGrip.OnPressed -= OnGrip;
            leftGrip.OnReleased -= OnRelease;
        }

        if (rightGrip != null)
        {
            rightGrip.OnPressed -= OnGrip;
            rightGrip.OnReleased -= OnRelease;
        }

        Plugin.MenuController.GetComponent<Potions>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    public override string GetDisplayName()
    {
        return "Climb";
    }

    public override string Tutorial()
    {
        return "[GRIP] to hold on to a Surface.";
    }
}