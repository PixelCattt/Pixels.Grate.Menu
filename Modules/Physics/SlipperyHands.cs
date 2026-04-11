using Bark.GUI;
using Bark.Patches;

namespace Bark.Modules.Physics;

public class SlipperyHands : BarkModule
{
    public static readonly string DisplayName = "Slippery Hands";
    public static SlipperyHands Instance;

    private void Awake()
    {
        Instance = this;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        if (NoSlip.Instance)
            NoSlip.Instance.enabled = false;
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
        return "All surfaces become Slippery.";
    }
}