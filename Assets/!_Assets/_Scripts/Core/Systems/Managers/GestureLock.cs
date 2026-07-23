/// <summary>
/// Reference-counted lock that any gesture-waiting system (toss, stir, scrub, ...) can hold
/// while it needs the player's full attention. Systems like screen navigation check IsLocked
/// so the player can't leave mid-gesture.
/// </summary>
public static class GestureLock
{
    private static int lockCount = 0;

    public static bool IsLocked => lockCount > 0;

    /// <summary>Fired whenever the lock is taken or released, so UI (e.g. nav buttons) can refresh.</summary>
    public static event System.Action OnLockChanged;

    public static void Lock()
    {
        lockCount++;
        if (lockCount == 1)
            OnLockChanged?.Invoke();
    }

    public static void Unlock()
    {
        if (lockCount == 0)
            return;

        lockCount--;
        if (lockCount == 0)
            OnLockChanged?.Invoke();
    }
}
