using Bark.GUI;
using Bark.Patches;

namespace Bark.Modules.Physics;

public class NoSlip : BarkModule
{
    public static readonly string DisplayName = "No Slip";
    public static NoSlip Instance;

    private void Awake()
    {
        Instance = this;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        if (SlipperyHands.Instance)
            SlipperyHands.Instance.enabled = false;
    }

    protected override void Cleanup()
    {
        var s = $"The functionality for this module is in {nameof(SlidePatch)}";
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "You no longer Slide on slippery Surfaces.";
    }
}