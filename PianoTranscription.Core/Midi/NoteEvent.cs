namespace PianoTranscription.Core.Midi;

public struct NoteEvent
{
    public float OnsetTime { get; }
    public float OffsetTime { get; }
    public int MidiNote { get; }
    public int Velocity { get; }

    public NoteEvent(float onsetTime, float offsetTime, int midiNote, int velocity)
    {
        OnsetTime = onsetTime;
        OffsetTime = offsetTime;
        MidiNote = midiNote;
        Velocity = velocity;
    }
}