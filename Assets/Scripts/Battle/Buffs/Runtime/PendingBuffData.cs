// A pending Buff mutation. It is not a future active Buff instance.
public class PendingBuffData
{
    public string buffID;
    public int stackDelta;
    public int intensityDelta;
    public bool hasIntensityDelta;
    public int delayTurns;
    public int applyTimes;
    public int intervalTurns;

    public PendingBuffData(
        string id,
        int stacks,
        int delay,
        int times,
        int interval,
        int intensity = 0,
        bool hasIntensity = false
    )
    {
        buffID = id;
        stackDelta = stacks;
        intensityDelta = intensity;
        hasIntensityDelta = hasIntensity;
        delayTurns = delay;
        applyTimes = times;
        intervalTurns = interval;
    }
}
