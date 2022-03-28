using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PianoTranscription.Core.Midi;

public class MidiWriter
{
    public static byte[] WriteEventsToMidi((List<NoteEvent>, List<PedalEvent>) dictTuple) =>
        WriteEventsToMidi(dictTuple.Item1, dictTuple.Item2);

    public static byte[] WriteEventsToMidi(List<NoteEvent> noteEvents, List<PedalEvent> pedalEvents)
    {
        var startTime = 0;
        var ticksPerBeat = 384;
        var beatsPerSecond = 2;
        var ticksPerSecond = ticksPerBeat * beatsPerSecond;
        var microsecondsPerBeat = (int)(1e6 / beatsPerSecond);

        var midi = new MidiFile();
        midi.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)ticksPerBeat);

        var t0 = new TrackChunk();
        var e0 = new SetTempoEvent(microsecondsPerBeat);
        var e1 = new TimeSignatureEvent(4, 4);
        t0.Events.Add(e0);
        t0.Events.Add(e1);
        midi.Chunks.Add(t0);

        var t1 = new TrackChunk();
        var roll = new List<MidiMessage>();
        foreach (var note in noteEvents)
        {
            roll.Add(new MidiMessage(note.OnsetTime, note.MidiNote, note.Velocity, 0));
            roll.Add(new MidiMessage(note.OffsetTime, note.MidiNote, 0, 0));
        }

        if (pedalEvents.Count != 0)
        {
            var controlChange = 64;
            foreach (var pedal in pedalEvents)
            {
                roll.Add(new MidiMessage(pedal.OnsetTime, controlChange, 127, 1));
                roll.Add(new MidiMessage(pedal.OffsetTime, controlChange, 0, 1));
            }
        }

        roll = roll.OrderBy(x => x.A).ToList();

        var previousTicks = 0;
        foreach (var m in roll)
        {
            var thisTicks = (int)((m.A - startTime) * ticksPerSecond);
            if (thisTicks >= 0)
            {
                var diffTicks = thisTicks - previousTicks;
                previousTicks = thisTicks;
                if (m.Type == 0)
                {
                    var me = new NoteOnEvent((SevenBitNumber)m.B, (SevenBitNumber)m.C)
                    {
                        DeltaTime = diffTicks
                    };
                    t1.Events.Add(me);
                }
                else
                {
                    var me = new ControlChangeEvent((SevenBitNumber)m.B, (SevenBitNumber)m.C)
                    {
                        DeltaTime = diffTicks
                    };
                    t1.Events.Add(me);
                }
            }
        }

        midi.Chunks.Add(t1);
        using var outstream = new MemoryStream();
        midi.Write(outstream);

        return outstream.ToArray();
    }

    private class MidiMessage
    {
        public float A { get; }
        public int B { get; }
        public int C { get; }
        public int Type { get; }

        public MidiMessage(float a, int b, int c, int type)
        {
            A = a;
            B = b;
            C = c;
            Type = type;
        }
    }
}