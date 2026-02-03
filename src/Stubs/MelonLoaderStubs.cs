#if BONELAB_STUBS
namespace MelonLoader;

public abstract class MelonMod
{
    public virtual void OnInitializeMelon() { }
}

public static class MelonEnvironment
{
    public static string ModsDirectory => string.Empty;
}
#endif
