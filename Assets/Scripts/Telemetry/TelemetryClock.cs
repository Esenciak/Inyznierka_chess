using System;
using UnityEngine;

public class TelemetryClock
{
    private DateTime matchStartUtc;
    private float matchStartRealtime;

    public void Reset()
    {
        matchStartUtc = DateTime.UtcNow;
        matchStartRealtime = Time.realtimeSinceStartup;
    }

    public long GetElapsedMs()
    {
        float elapsed = Time.realtimeSinceStartup - matchStartRealtime;
        return (long)(elapsed * 1000f);
    }

    public string GetTimestampUtc()
    {
        return DateTime.UtcNow.ToString("o");
    }
}
