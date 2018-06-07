﻿using System;
using System.Linq;
using BeatSaberSongGenerator.AudioProcessing;
using BeatSaberSongGenerator.IO;
using BeatSaberSongGenerator.Objects;

namespace BeatSaberSongGenerator.Generators
{
    public class SongGenerator
    {
        private readonly BeatDetector beatDetector;
        private readonly LevelInstructionGenerator levelInstructionGenerator;

        public SongGenerator(SongGeneratorSettings settings)
        {
            beatDetector = new BeatDetector();
            levelInstructionGenerator = new LevelInstructionGenerator(settings);
        }

        public Song Generate(string songName, string author, string audioFilePath, string coverFilePath)
        {
            var audioMetadata = GetAudioMetadata(audioFilePath);
            audioMetadata.SongName = songName;
            audioMetadata.Author = author;
            var environmentType = EnvironmentType.DefaultEnvironment;
            var difficulties = new []{ Difficulty.Easy, Difficulty.Normal, Difficulty.Hard, Difficulty.Expert};
            var songInfo = new SongInfo
            {
                SongName = songName,
                SongSubName = "",
                AuthorName = author,
                BeatsPerMinute = (float) audioMetadata.BeatsPerMinute,
                PreviewStartTime = 0,
                PreviewDuration = 0,
                CoverImagePath = SongStorer.CoverImagePath,
                EnvironmentName = environmentType,
                DifficultyLevels = difficulties.Select(GenerateDifficultyLevel).ToList()
            };
            var levelInstructions = difficulties.ToDictionary(
                difficulty => difficulty, 
                difficulty => levelInstructionGenerator.Generate(difficulty, audioMetadata));
            return new Song(songInfo, levelInstructions, audioFilePath, coverFilePath);
        }

        private DifficultyLevel GenerateDifficultyLevel(Difficulty difficulty)
        {
            return new DifficultyLevel
            {
                AudioPath = SongStorer.SongPath,
                Difficulty = difficulty,
                InstructionPath = SongStorer.GenerateLevelFilePath(difficulty),
                Offset = 0,
                OldOffset = 0
            };
        }

        private AudioMetadata GetAudioMetadata(string audioFilePath)
        {
            var audioData = AudioSampleReader.ReadMonoSamples(audioFilePath, out var sampleRate);
            var beatDetectorResult = beatDetector.DetectBeats(audioData, sampleRate);
            return new AudioMetadata
            {
                SampleRate = sampleRate,
                Length = TimeSpan.FromSeconds(audioData.Count / (double)sampleRate),
                BeatsPerMinute = beatDetectorResult.BeatsPerMinute,
                BeatsPerBar = 4,
                Beats = beatDetectorResult.Beats,
                SongIntensities = beatDetectorResult.SongIntensities
            };
        }
    }
}
