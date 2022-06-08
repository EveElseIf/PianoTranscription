using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PianoTranscription.Core.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PianoTranscription.Core;

public class Transcriptor
{
    private readonly InferenceSession _ortSession;
    private const int segmentSamples = 16000 * 10;
    private const int framesPerSecond = 100;
    private const int classesNum = 88;
    private const float onsetThreshold = 0.3f;
    private const float offsetThreshold = 0.3f;
    private const float frameThreshold = 0.1f;
    private const float pedalOffsetThreshold = 0.2f;

    public Transcriptor(byte[] model)
    {
        var option = new SessionOptions();
        option.AppendExecutionProvider_CPU();

        try
        {
            _ortSession = new InferenceSession(model, option);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public (List<NoteEvent>, List<PedalEvent>) Transcript(float[] normalizedData, Action<int, int>? progressReport = null, CancellationToken? token = null)
    {
        var padLen = (int)MathF.Ceiling(normalizedData.Length / segmentSamples) * segmentSamples -
                     normalizedData.Length;
        if (padLen != 0)
        {
            padLen = ((int)MathF.Ceiling(normalizedData.Length / segmentSamples) + 1) * segmentSamples -
                     normalizedData.Length;
            var buf = new float[normalizedData.Length + padLen];
            Array.Copy(normalizedData, buf, normalizedData.Length);
            normalizedData = buf;
        }

        var segments = Enframe(normalizedData, segmentSamples);
        var outputDict = Forward(segments, progressReport, token);
        var newOutputDict = new Dictionary<string, float[,]>();
        if (token?.IsCancellationRequested ?? false)
            throw new OperationCanceledException();
        foreach (var item in outputDict)
        {
            newOutputDict.Add(item.Key, Deframe(item.Value));
        }

        var postProcessor = new RegressionPostProcessor(framesPerSecond, classesNum, onsetThreshold, offsetThreshold,
            frameThreshold, pedalOffsetThreshold);
        var output = postProcessor.OutputDictToMidiEvents(newOutputDict);
        var estNoteEvents = output.Item1;
        var estPedalEvents = output.Item2;

        return (estNoteEvents, estPedalEvents);
    }

    private float[,] Deframe(List<Tensor<float>> x)
    {
        if (x.Count == 1)
            return ReadTensor(x[0]);
        else
        {
            var segmentSamples = x[0].Dimensions[1] - 1;
            var length = x[0].Dimensions[2];
            var y = new float[(int)(segmentSamples * 0.75 * 2 + segmentSamples * 0.5 * (x.Count - 2)), length];
            y.Write(0..(int)(segmentSamples * 0.75), 0..length,
                ReadTensor(x[0]).Read(0..(int)(segmentSamples * 0.75), 0..length));
            for (int i = 1; i < x.Count - 1; i++)
            {
                y.Write(
                    ((int)(segmentSamples * 0.75) + (i - 1) * (int)(segmentSamples * 0.5))..((int)(segmentSamples *
                        0.75) + i * (int)(segmentSamples * 0.5)),
                    0..length,
                    ReadTensor(x[i]).Read((int)(segmentSamples * 0.25)..(int)(segmentSamples * 0.75), 0..length));
            }

            y.Write((int)(segmentSamples * 0.75 + (x.Count - 2) * (segmentSamples * 0.5))..y.GetLength(0), 0..length,
                ReadTensor(x.Last()).Read((int)(segmentSamples * 0.25)..segmentSamples, 0..length));
            return y;
        }
    }

    private float[,] ReadTensor(Tensor<float> t)
    {
        int x = t.Dimensions[1], y = t.Dimensions[2];
        var output = new float[x, y];
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                output[i, j] = t[0, i, j];
            }
        }

        return output;
    }

    private Dictionary<string, List<Tensor<float>>> Forward(List<float[]> data, Action<int, int>? progressReporter, CancellationToken? token)
    {
        int batchSize = 1;
        var outputDic = new Dictionary<string, List<Tensor<float>>>()
        {
            {"reg_onset_output", new List<Tensor<float>>()},
            {"reg_offset_output", new List<Tensor<float>>()},
            {"frame_output", new List<Tensor<float>>()},
            {"velocity_output", new List<Tensor<float>>()},
            {"reg_pedal_onset_output", new List<Tensor<float>>()},
            {"reg_pedal_offset_output", new List<Tensor<float>>()},
            {"pedal_frame_output", new List<Tensor<float>>()}
        };
        int p = 0;
        var totalSegments = data.Count / batchSize;
        while (true)
        {
            if (token?.IsCancellationRequested ?? false)
                throw new OperationCanceledException();

            Console.WriteLine($"Segment {p} / {totalSegments}");
            if (p >= data.Count) break;

            var tensor = new DenseTensor<float>(data[p], new int[] { 1, segmentSamples });
            var inputs = NamedOnnxValue.CreateFromTensor("input", tensor);
            var output = _ortSession.Run(new[] { inputs });
            foreach (var i in output)
            {
                outputDic[i.Name].Add(i.AsTensor<float>());
            }

            p += batchSize;

            progressReporter?.Invoke(p, totalSegments);
        }

        return outputDic;
    }

    private List<float[]> Enframe(float[] data, int i)
    {
        int p = 0;
        var batch = new List<float[]>();
        while (p + i <= data.Length)
        {
            batch.Add(data[p..(p + i)]);
            p += i / 2;
        }

        return batch;
    }
}

internal class RegressionPostProcessor
{
    private readonly int _framesPerSecond;
    private readonly int _classesNum;
    private readonly float _onsetThreshold;
    private readonly float _offsetThreshold;
    private readonly float _frameThreshold;
    private readonly float _pedalOffsetThreshold;
    private readonly int _beginNote = 21;
    private readonly int velocityScale = 128;

    public RegressionPostProcessor(int framesPerSecond, int classesNum, float onsetThreshold, float offsetThreshold,
        float frameThreshold, float pedalOffsetThreshold)
    {
        _framesPerSecond = framesPerSecond;
        _classesNum = classesNum;
        _onsetThreshold = onsetThreshold;
        _offsetThreshold = offsetThreshold;
        _frameThreshold = frameThreshold;
        _pedalOffsetThreshold = pedalOffsetThreshold;
    }

    public (List<NoteEvent>, List<PedalEvent>) OutputDictToMidiEvents(Dictionary<string, float[,]> dict)
    {
        var out1 = OutputDictToNotePedalArrays(dict);
        var estOnOffNoteVels = out1.Item1;
        var estPedalOnOffs = out1.Item2;

        (List<NoteEvent>, List<PedalEvent>) output;

        var estNoteEvents = DetectedNotesToEvents(estOnOffNoteVels);
        output.Item1 = estNoteEvents;

        if (estPedalOnOffs.Count != 0)
        {
            var estPedalEvents = DetectedPedalsToEvents(estPedalOnOffs);
            output.Item2 = estPedalEvents;
        }
        else
            output.Item2 = new List<PedalEvent>();

        return output;
    }

    private List<PedalEvent> DetectedPedalsToEvents(List<float[]> estPedalOnOffs)
    {
        var output = new List<PedalEvent>();
        foreach (var i in estPedalOnOffs)
        {
            output.Add(new PedalEvent(i[0], i[1]));
        }

        return output;
    }

    private List<NoteEvent> DetectedNotesToEvents(List<float[]> estOnOffNoteVels)
    {
        var output = new List<NoteEvent>();
        foreach (var i in estOnOffNoteVels)
        {
            output.Add(new NoteEvent(i[0], i[1], (int)i[2], (int)(i[3] * velocityScale)));
        }

        return output;
    }

    private (List<float[]>, List<float[]>) OutputDictToNotePedalArrays(Dictionary<string, float[,]> dict)
    {
        var out1 = GetBinarizedOutputFromRegression(dict["reg_onset_output"], _onsetThreshold, 2);
        var onsetOutput = out1[0];
        var onsetShiftOutput = out1[1];
        dict.Add("onset_output", onsetOutput);
        dict.Add("onset_shift_output", onsetShiftOutput);

        var out2 = GetBinarizedOutputFromRegression(dict["reg_offset_output"], _offsetThreshold, 4);
        var offsetOutput = out2[0];
        var offsetShiftOutput = out2[1];
        dict.Add("offset_output", offsetOutput);
        dict.Add("offset_shift_output", offsetShiftOutput);

        if (dict.ContainsKey("reg_pedal_offset_output"))
        {
            var out3 = GetBinarizedOutputFromRegression(dict["reg_pedal_offset_output"], _pedalOffsetThreshold, 4);
            var pedalOffsetOutput = out3[0];
            var pedalOffsetShiftOutput = out3[1];
            dict.Add("pedal_offset_output", pedalOffsetOutput);
            dict.Add("pedal_offset_shift_output", pedalOffsetShiftOutput);
        }

        (List<float[]>, List<float[]>) output;

        var estOnOffNoteVels = OutputDictToDetectedNotes(dict);
        output.Item1 = estOnOffNoteVels;

        if (dict.ContainsKey("reg_pedal_onset_output"))
        {
            var estPedalOnOffs = OutputDictToDetectedPedals(dict);
            output.Item2 = estPedalOnOffs;
        }
        else
            output.Item2 = new List<float[]>();

        return output;
    }


    private List<float[]> OutputDictToDetectedNotes(Dictionary<string, float[,]> dict)
    {
        var estTunples = new List<float[]>();
        var estMidiNotes = new List<float>();
        var classesNum = dict["frame_output"].GetLength(1);
        for (int pianoNote = 0; pianoNote < classesNum; pianoNote++)
        {
            var estTunplesPerNote = NoteDetectionWithOnsetOffsetRegress(
                new ExtractedArray(dict["frame_output"], pianoNote),
                new ExtractedArray(dict["onset_output"], pianoNote),
                new ExtractedArray(dict["onset_shift_output"], pianoNote),
                new ExtractedArray(dict["offset_output"], pianoNote),
                new ExtractedArray(dict["offset_shift_output"], pianoNote),
                new ExtractedArray(dict["velocity_output"], pianoNote),
                _frameThreshold
            );
            estTunples.AddRange(estTunplesPerNote);
            for (int i = 0; i < estTunplesPerNote.Count; i++)
            {
                estMidiNotes.Add(pianoNote + _beginNote);
            }
        }

        if (estTunples.Count == 0)
            return new List<float[]>();
        else
        {
            var onsetTimes = Add2ArraysAndDivide(
                new ExtractedArray2(estTunples.ToArray(), 0),
                new ExtractedArray2(estTunples.ToArray(), 2),
                _framesPerSecond
            );
            var offsetTimes = Add2ArraysAndDivide(
                new ExtractedArray2(estTunples.ToArray(), 1),
                new ExtractedArray2(estTunples.ToArray(), 3),
                _framesPerSecond
            );
            var velocities = new ExtractedArray2(estTunples.ToArray(), 4);
            var estOnOffNoteVels = Combine4ArraysToList(new ExtractedArray3(onsetTimes),
                new ExtractedArray3(offsetTimes), new ExtractedArray3(estMidiNotes.ToArray()), velocities);
            return estOnOffNoteVels;
        }
    }

    private List<float[]> Combine4ArraysToList(IExtractedArray a, IExtractedArray b, IExtractedArray c,
        IExtractedArray d)
    {
        var output = new List<float[]>();
        for (var i = 0; i < a.Length; i++)
        {
            output.Add(new[] { a[i], b[i], c[i], d[i] });
        }

        return output;
    }

    private float[] Add2ArraysAndDivide(IExtractedArray a, IExtractedArray b, float devide)
    {
        var output = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            output[i] = (a[i] + b[i]) / devide;
        }

        return output;
    }

    private List<float[]> NoteDetectionWithOnsetOffsetRegress(IExtractedArray frameOutputs,
        IExtractedArray onsetOutputs,
        IExtractedArray onsetShiftOutputs, IExtractedArray offsetOutputs, IExtractedArray offsetShiftOutputs,
        IExtractedArray velocityOutputs, float frameThreshold)
    {
        var bgn = 0;
        var frameDisappear = 0;
        var offsetOccur = 0;
        var fin = 0;
        var outputTunples = new List<float[]>();

        for (int i = 0; i < onsetOutputs.Length; i++)
        {
            if (onsetOutputs[i] == 1)
            {
                if (bgn != 0)
                {
                    fin = Math.Max(i - 1, 0);
                    outputTunples.Add(new float[] { bgn, fin, onsetShiftOutputs[bgn], 0, velocityOutputs[bgn] });
                    frameDisappear = offsetOccur = 0;
                }

                bgn = i;
            }

            if (bgn != 0 && i > bgn)
            {
                if (frameOutputs[i] <= frameThreshold && frameDisappear == 0)
                {
                    frameDisappear = i;
                }

                if (offsetOutputs[i] == 1 && offsetOccur == 0)
                {
                    offsetOccur = 1;
                }

                if (frameDisappear != 0)
                {
                    if (offsetOccur != 0 && offsetOccur - bgn > frameDisappear - offsetOccur)
                    {
                        fin = offsetOccur;
                    }
                    else
                    {
                        fin = frameDisappear;
                    }

                    outputTunples.Add(new float[]
                        {bgn, fin, onsetShiftOutputs[bgn], offsetShiftOutputs[fin], velocityOutputs[bgn]});
                    bgn = frameDisappear = offsetOccur = 0;
                }

                if (bgn != 0 && (i - bgn >= 600 || i == onsetOutputs.Length - 1))
                {
                    fin = i;
                    outputTunples.Add(new float[]
                        {bgn, fin, onsetShiftOutputs[bgn], offsetShiftOutputs[fin], velocityOutputs[bgn]});
                    bgn = frameDisappear = offsetOccur = 0;
                }
            }
        }

        return outputTunples.OrderBy(x => x[0]).ToList();
    }

    private List<float[]> OutputDictToDetectedPedals(Dictionary<string, float[,]> dict)
    {
        var framesNum = dict["pedal_frame_output"].GetLength(0);
        var estTuples = PedalDetectionWithOnsetOffsetRegress(
            new ExtractedArray(dict["pedal_frame_output"], 0),
            new ExtractedArray(dict["pedal_offset_output"], 0),
            new ExtractedArray(dict["pedal_offset_shift_output"], 0),
            0.5
        );
        if (estTuples.Count == 0)
            return new List<float[]>();
        else
        {
            var onsetTimes = Add2ArraysAndDivide(
                new ExtractedArray2(estTuples.ToArray(), 0),
                new ExtractedArray2(estTuples.ToArray(), 2),
                _framesPerSecond
            );
            var offsetTimes = Add2ArraysAndDivide(
                new ExtractedArray2(estTuples.ToArray(), 1),
                new ExtractedArray2(estTuples.ToArray(), 3),
                _framesPerSecond
            );
            var estOnOff = Combine2ArraysToList(new ExtractedArray3(onsetTimes), new ExtractedArray3(offsetTimes));
            return estOnOff;
        }
    }

    private List<float[]> Combine2ArraysToList(IExtractedArray a, IExtractedArray b)
    {
        var output = new List<float[]>();
        for (var i = 0; i < a.Length; i++)
        {
            output.Add(new[] { a[i], b[i] });
        }

        return output;
    }

    private List<float[]> PedalDetectionWithOnsetOffsetRegress(IExtractedArray frameOutputs,
        IExtractedArray offsetOutputs, IExtractedArray offsetShiftOutputs, double frameThreshold)
    {
        var bgn = 0;
        var frameDisappear = 0;
        var offsetOccur = 0;
        var fin = 0;
        var output = new List<float[]>();

        for (int i = 1; i < frameOutputs.Length; i++)
        {
            if (frameOutputs[i] >= frameThreshold && frameOutputs[i] > frameOutputs[i - 1])
            {
                if (bgn == 0)
                {
                    bgn = i;
                }
            }

            if (bgn != 0 && i > bgn)
            {
                if (frameOutputs[i] <= frameThreshold && frameDisappear == 0)
                {
                    frameDisappear = i;
                }

                if (offsetOutputs[i] == 1 && offsetOccur == 0)
                {
                    offsetOccur = i;
                }

                if (offsetOccur != 0)
                {
                    fin = offsetOccur;
                    output.Add(new float[] { bgn, fin, 0, offsetShiftOutputs[fin] });
                    bgn = frameDisappear = offsetOccur = 0;
                }

                if (frameDisappear != 0 && i - frameDisappear >= 10)
                {
                    fin = frameDisappear;
                    output.Add(new float[] { bgn, fin, 0, offsetShiftOutputs[fin] });
                    bgn = frameDisappear = offsetOccur = 0;
                }
            }
        }

        return output.OrderBy(x => x[0]).ToList();
    }

    private List<float[,]> GetBinarizedOutputFromRegression(float[,] regOutput, float threshold, int neighbour)
    {
        var binaryOutput = new float[regOutput.GetLength(0), regOutput.GetLength(1)];
        var shiftOutput = new float[regOutput.GetLength(0), regOutput.GetLength(1)];
        var framesNum = regOutput.GetLength(0);
        var classesNum = regOutput.GetLength(1);
        for (int k = 0; k < classesNum; k++)
        {
            var x = new ExtractedArray(regOutput, k);
            for (int n = neighbour; n < framesNum - neighbour; n++)
            {
                if (x[n] > threshold && IsMonotonicNeighbour(x, n, neighbour))
                {
                    binaryOutput[n, k] = 1;
                    float shift;
                    if (x[n - 1] > x[n + 1])
                        shift = (x[n + 1] - x[n - 1]) / (x[n] - x[n + 1]) / 2;
                    else
                        shift = (x[n + 1] - x[n - 1]) / (x[n] - x[n - 1]) / 2;
                    shiftOutput[n, k] = shift;
                }
            }
        }

        return new List<float[,]>
        {
            binaryOutput,
            shiftOutput
        };
    }

    private bool IsMonotonicNeighbour(ExtractedArray x, int n, int neighbour)
    {
        var monotonic = true;
        for (int i = 0; i < neighbour; i++)
        {
            if (x[n - i] < x[n - i - 1])
                monotonic = false;
            if (x[n + i] < x[n + i + 1])
                monotonic = false;
        }

        return monotonic;
    }

    private interface IExtractedArray
    {
        public int Length { get; }
        public float this[int index] { get; }
    }

    private class ExtractedArray : IExtractedArray
    {
        private readonly float[,] _array;
        private readonly int _column;

        public int Length
        {
            get => _array.GetLength(0);
        }

        public ExtractedArray(float[,] array, int column)
        {
            _array = array;
            _column = column;
        }

        public float this[int index] => _array[index, _column];
    }

    private class ExtractedArray2 : IExtractedArray
    {
        private readonly float[][] _array;
        private readonly int _column;

        public int Length
        {
            get => _array.Length;
        }

        public ExtractedArray2(float[][] array, int column)
        {
            _array = array;
            _column = column;
        }

        public float this[int index] => _array[index][_column];
    }

    private class ExtractedArray3 : IExtractedArray
    {
        private readonly float[] _array;

        public ExtractedArray3(float[] array)
        {
            _array = array;
        }

        public int Length
        {
            get => _array.Length;
        }

        public float this[int index] => _array[index];
    }
}