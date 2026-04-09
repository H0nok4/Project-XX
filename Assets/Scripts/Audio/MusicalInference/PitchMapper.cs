using UnityEngine;

namespace GuitarTest.Audio.MusicalInference
{
    public static class PitchMapper
    {
        private static readonly string[] NoteNames =
        {
            "C",
            "C#",
            "D",
            "D#",
            "E",
            "F",
            "F#",
            "G",
            "G#",
            "A",
            "A#",
            "B",
        };

        public static void DescribeFrequency(float frequencyHz, out int midiNote, out string noteName, out int octave, out float centsOffset)
        {
            midiNote = FrequencyToMidiNote(frequencyHz);
            noteName = MidiToNoteName(midiNote);
            octave = MidiToOctave(midiNote);
            centsOffset = FrequencyToCentsOffset(frequencyHz, midiNote);
        }

        public static int FrequencyToMidiNote(float frequencyHz)
        {
            float midi = 69f + (12f * Mathf.Log(frequencyHz / 440f, 2f));
            return Mathf.RoundToInt(midi);
        }

        public static float FrequencyToCentsOffset(float frequencyHz, int midiNote)
        {
            float targetFrequency = MidiToFrequency(midiNote);
            return GetCentsDifference(targetFrequency, frequencyHz);
        }

        public static float MidiToFrequency(int midiNote)
        {
            return 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
        }

        public static string MidiToNoteName(int midiNote)
        {
            int noteIndex = ((midiNote % 12) + 12) % 12;
            return NoteNames[noteIndex];
        }

        public static int MidiToOctave(int midiNote)
        {
            return (midiNote / 12) - 1;
        }

        public static float GetCentsDifference(float referenceFrequencyHz, float comparedFrequencyHz)
        {
            if (referenceFrequencyHz <= 0f || comparedFrequencyHz <= 0f)
            {
                return 0f;
            }

            return 1200f * Mathf.Log(comparedFrequencyHz / referenceFrequencyHz, 2f);
        }
    }
}