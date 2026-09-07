using System;
using System.IO;

/// <summary>Bounded PCM16 stereo 44100-Hz RIFF reader; cue wrappers enforce their exact durations.</summary>
public static class SkillPcmWave
{
    public const int SampleRate = 44100;
    public const int MaxWaveBytes = 512 * 1024;

    public static float[] ReadStereo44100(string path, int expectedFrames)
    {
        if (expectedFrames <= 0 || expectedFrames > (MaxWaveBytes - 44) / 4) throw new ArgumentOutOfRangeException("expectedFrames");
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new BinaryReader(stream))
        {
            if (stream.Length < 44 || stream.Length > MaxWaveBytes) throw new InvalidDataException("WAV file size outside fixed task bounds");
            if (reader.ReadUInt32() != 0x46464952) throw new InvalidDataException("Expected RIFF");
            if ((long)reader.ReadUInt32() + 8 != stream.Length) throw new InvalidDataException("RIFF length mismatch");
            if (reader.ReadUInt32() != 0x45564157) throw new InvalidDataException("Expected WAVE");
            bool haveFormat = false;
            long dataOffset = -1;
            uint dataLength = 0;
            while (stream.Position < stream.Length)
            {
                if (stream.Length - stream.Position < 8) throw new InvalidDataException("Truncated WAV chunk header");
                uint id = reader.ReadUInt32(), size = reader.ReadUInt32();
                long start = stream.Position, next = start + size + (size & 1u);
                if (next > stream.Length) throw new InvalidDataException("WAV chunk exceeds file");
                if (id == 0x20746D66) // fmt
                {
                    if (haveFormat || (size != 16 && size != 18)) throw new InvalidDataException("Duplicate or unsupported PCM format chunk");
                    ushort format = reader.ReadUInt16(), channels = reader.ReadUInt16();
                    uint rate = reader.ReadUInt32(), byteRate = reader.ReadUInt32();
                    ushort blockAlign = reader.ReadUInt16(), bits = reader.ReadUInt16();
                    if (format != 1 || channels != 2 || rate != 44100 || byteRate != 176400 || blockAlign != 4 || bits != 16)
                        throw new InvalidDataException("Expected PCM16 stereo 44100 Hz with consistent byte rate/alignment");
                    if (size == 18 && reader.ReadUInt16() != 0) throw new InvalidDataException("Unexpected PCM extension");
                    haveFormat = true;
                }
                else if (id == 0x61746164) // data
                {
                    if (dataOffset >= 0) throw new InvalidDataException("Duplicate WAV data chunk");
                    dataOffset = start;
                    dataLength = size;
                }
                stream.Position = next;
            }
            if (!haveFormat || dataOffset < 0 || dataLength != expectedFrames * 4) throw new InvalidDataException("Missing PCM data or incorrect frozen duration");
            stream.Position = dataOffset;
            var result = new float[expectedFrames * 2];
            for (int i = 0; i < result.Length; i++) result[i] = reader.ReadInt16() / 32768f;
            return result;
        }
    }
}
