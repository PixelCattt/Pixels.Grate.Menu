namespace Bark.Modules.Misc;

internal class DisableWind : BarkModule
{
    public static bool Enabled;
    public static string DisplayName = "Disable Wind";

    protected override void Start()
    {
        Enabled = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Enabled = true;
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Disables the Wind Barriers.";
    }

    protected override void Cleanup()
    {
        Enabled = false;
    }
}