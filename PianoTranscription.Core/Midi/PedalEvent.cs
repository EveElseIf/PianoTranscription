namespace PianoTranscription.Core.Midi;

public struct PedalEvent
{
    public float OnsetTime { get; }
    public float OffsetTime { get; }

    public PedalEvent(float onsetTime, float offsetTime)
    {
        OnsetTime = onsetTime;
        OffsetTime = offsetTime;
    }
}