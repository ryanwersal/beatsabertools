﻿using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberSongGenerator.AudioProcessing;
using BeatSaberSongGenerator.Objects;
using Commons;
using Commons.Extensions;
using Commons.Mathematics;

namespace BeatSaberSongGenerator.Generators
{
    public class LevelInstructionGenerator
    {
        private readonly SongGeneratorSettings settings;

        public LevelInstructionGenerator(SongGeneratorSettings settings)
        {
            this.settings = settings;
        }

        public LevelInstructions Generate(Difficulty difficulty, AudioMetadata audioMetadata)
        {
            var events = new List<Event>();
            //var notes = new List<Note>();
            //var notes = GenerateRandomNotes(difficulty, audioMetadata);
            var notes = GenerateNotesFromSongAnalysis(difficulty, audioMetadata);
            var obstacles = new List<Obstacle>();
            //var obstacles = GenerateRandomObstacle(difficulty, audioMetadata);
            return new LevelInstructions
            {
                Version = "1.5.0",
                BeatsPerMinute = (float) audioMetadata.BeatsPerMinute,
                BeatsPerBar = audioMetadata.BeatsPerBar,
                NoteJumpSpeed = 10,
                Shuffle = 0,
                ShufflePeriod = 0.5f,
                Events = events,
                Notes = notes,
                Obstacles = obstacles
            };
        }

        private IList<Note> GenerateNotesFromSongAnalysis(Difficulty difficulty, AudioMetadata audioMetadata)
        {
            var sampleRate = audioMetadata.SampleRate;
            var bpm = audioMetadata.BeatsPerMinute;
            var songIntensities = audioMetadata.SongIntensities;
            var continuousSongIntensity = new ContinuousLine2D(songIntensities.Select(x => new Point2D(x.SampleIndex, x.Intensity)));
            var beats = audioMetadata.Beats;
            var notes = new List<Note>();
            var minimumTimeBetweenNotes = DetermineTimeBetweenNotes(difficulty);
            Beat lastBeat = null;
            foreach (var beat in beats)
            {
                if (lastBeat != null)
                {
                    var timeSinceLastBeat = SampleIndexToTime(beat.SampleIndex, sampleRate) 
                                            - SampleIndexToTime(lastBeat.SampleIndex, sampleRate);
                    var currentIntensity = continuousSongIntensity.ValueAtX(beat.SampleIndex);
                    if (currentIntensity.IsNaN())
                        currentIntensity = 0;
                    var intensityAdjustment = TimeSpan.FromSeconds(1 - currentIntensity);
                    if(timeSinceLastBeat < minimumTimeBetweenNotes + intensityAdjustment)
                        continue;
                }
                var cutDirection = (CutDirection) StaticRandom.Rng.Next(0, 9);
                var hand = StaticRandom.Rng.Next(2) == 1 ? Hand.Right : Hand.Left;
                var note = new Note
                {
                    Time = SampleIndexToBeatIndex(beat.SampleIndex, sampleRate, bpm),
                    CutDirection = cutDirection,
                    Hand = hand,
                    HorizontalPosition = (HorizontalPosition)StaticRandom.Rng.Next(4),
                    VerticalPosition = VerticalPosition.Bottom,
                };
                notes.Add(note);
                lastBeat = beat;
            }
            return notes;
        }

        private static float SampleIndexToBeatIndex(int sampleIndex, int sampleRate, double bpm)
        {
            var beatsPerSecond = bpm / 60;
            return (float) (sampleIndex * beatsPerSecond / sampleRate);
        }

        private static TimeSpan SampleIndexToTime(int sampleIndex, int sampleRate)
        {
            return TimeSpan.FromSeconds(sampleIndex / (double)sampleRate);
        }

        private TimeSpan DetermineTimeBetweenNotes(Difficulty difficulty)
        {
            double multiplier;
            switch (difficulty)
            {
                case Difficulty.Easy:
                    multiplier = 2;
                    break;
                case Difficulty.Normal:
                    multiplier = 1;
                    break;
                case Difficulty.Hard:
                    multiplier = 0.8;
                    break;
                case Difficulty.Expert:
                    multiplier = 0.5;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
            }
            return TimeSpan.FromSeconds(multiplier * (1-settings.SkillLevel));
        }

        private IList<Note> GenerateRandomNotes(Difficulty difficulty, AudioMetadata audioMetadata)
        {
            var notes = new List<Note>();
            var nextHand = Hand.Right;
            var stepSize = difficulty == Difficulty.Easy ? 4.0
                : difficulty == Difficulty.Normal ? 2.0
                : difficulty == Difficulty.Hard ? 1.0
                : difficulty == Difficulty.Expert ? 0.5
                : throw new ArgumentOutOfRangeException(nameof(difficulty));
            var beatsPerSecond = audioMetadata.BeatsPerMinute / 60;
            var totalBeats = audioMetadata.Length.TotalSeconds * beatsPerSecond;
            foreach (var beatIdx in SequenceGeneration.FixedStep(5, totalBeats, stepSize))
            {
                var cutDirection = (CutDirection) StaticRandom.Rng.Next(0, 9);
                var note = new Note
                {
                    Time = (float) beatIdx,
                    CutDirection = cutDirection,
                    Hand = nextHand,
                    HorizontalPosition = nextHand == Hand.Right ? HorizontalPosition.Right : HorizontalPosition.Left,
                    VerticalPosition = VerticalPosition.Bottom
                };
                notes.Add(note);

                nextHand = nextHand == Hand.Right ? Hand.Left : Hand.Right;
            }
            return notes;
        }

        private IList<Obstacle> GenerateRandomObstacle(Difficulty difficulty, AudioMetadata audioMetadata)
        {
            var obstacles = new List<Obstacle>();
            var beatsPerSecond = audioMetadata.BeatsPerMinute / 60;
            var totalBeats = audioMetadata.Length.TotalSeconds * beatsPerSecond;
            foreach (var beatIdx in SequenceGeneration.FixedStep(10, totalBeats, 10))
            {
                var obstacle = new Obstacle
                {
                    Time = (float)beatIdx,
                    Duration = 5,
                    Type = ObstableType.Wall,
                    Width = 1,
                    HorizontalPosition = (HorizontalPosition)StaticRandom.Rng.Next(0, 4)
                };
                obstacles.Add(obstacle);
            }
            return obstacles;
        }
    }
}