using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Wobble.Audio.Tracks;
using Wobble.Graphics;
using ManagedBass;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Scheduling;
using Wobble.Logging;

namespace Quaver.Shared.Screens.Edit.UI.Playfield.Waveform
{
    public class EditorPlayfieldWaveform : Container
    {
        private List<EditorPlayfieldWaveformSlice> Slices { get; set; }

        private List<EditorPlayfieldWaveformSlice> VisibleSlices { get; }

        private EditorPlayfield Playfield { get; }

        private float[] TrackData { get; set; }

        private long TrackByteLength { get; set; }

        private double TrackLengthMilliSeconds { get; set; }

        private int SliceSize { get; set; }

        private int Stream { get; set; }

        private CancellationToken Token { get; }

        public EditorPlayfieldWaveform(EditorPlayfield playfield, CancellationToken token)
        {
            Playfield = playfield;
            Token = token;

            Slices = new List<EditorPlayfieldWaveformSlice>();
            VisibleSlices = new List<EditorPlayfieldWaveformSlice>();

            GenerateWaveform();
            CheckCancellationToken();
        }

        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (Slices.Count > 0)
            {
                foreach (var slice in Slices)
                    slice.Update(gameTime);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            var index = (int)(Audio.AudioEngine.Track.Time / TrackLengthMilliSeconds * Slices.Count);

            var amount = Math.Max(3, (int)(2.5f / Playfield.TrackSpeed + 0.5f));

            for (var i = 0; i < amount; i++)
                TryDrawSlice(index + (i - amount / 2), gameTime);
        }

        /// <summary>
        /// </summary>
        public void GenerateWaveform()
        {
            SliceSize = (int)Playfield.Height;
            GenerateTrackData();

            var tempSlices = new List<EditorPlayfieldWaveformSlice>();
            int t;

            for (t = 0; t < TrackLengthMilliSeconds; t += SliceSize)
            {
                var trackSliceData = new float[SliceSize, 2];

                for (var y = 0; y < SliceSize; y++)
                {
                    var timePoint = t + y;

                    var index = Bass.ChannelSeconds2Bytes(Stream, timePoint / 1000.0) / 4;

                    if (index >= TrackByteLength / sizeof(float))
                        continue;

                    trackSliceData[y, 0] = TrackData[index];
                    trackSliceData[y, 1] = TrackData[index + 1];
                }

                var slice = new EditorPlayfieldWaveformSlice(Playfield, SliceSize, trackSliceData, t);
                tempSlices.Add(slice);
            }

            Logger.Debug("Waveform: t = " + t, LogType.Runtime);
            Logger.Debug("Waveform: TrackLengthMilliSeconds = " + TrackLengthMilliSeconds, LogType.Runtime);
            Logger.Debug("Waveform: Audio.AudioEngine.Track.Time = " + Audio.AudioEngine.Track.Time, LogType.Runtime);

            Slices = tempSlices;
            Bass.StreamFree(Stream);
        }

        /// <summary>
        /// </summary>
        private void GenerateTrackData()
        {
            const BassFlags flags = BassFlags.Decode | BassFlags.Float;

            Stream = Bass.CreateStream(((AudioTrack)Audio.AudioEngine.Track).OriginalFilePath, 0, 0, flags);

            TrackByteLength = Bass.ChannelGetLength(Stream);
            Logger.Debug("Waveform: TrackByteLength = " + TrackByteLength, LogType.Runtime);

            TrackData = new float[TrackByteLength / sizeof(float)];

            TrackByteLength = Bass.ChannelGetData(Stream, TrackData, (int)TrackByteLength);
            Logger.Debug("Waveform: TrackByteLength = " + TrackByteLength, LogType.Runtime);

            TrackLengthMilliSeconds = Bass.ChannelBytes2Seconds(Stream, TrackByteLength) * 1000.0;
            Logger.Debug("Waveform: ChannelBytes2Seconds = " + Bass.ChannelBytes2Seconds(Stream, TrackByteLength), LogType.Runtime);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="gameTime"></param>
        private void TryDrawSlice(int index, GameTime gameTime)
        {
            if (index >= 0 && index < Slices.Count)
                Slices[index]?.Draw(gameTime);
        }

        private void CheckCancellationToken()
        {
            if (!Token.IsCancellationRequested)
                return;

            Destroy();
        }

        /// <summary>
        /// </summary>
        public override void Destroy() => DisposeWaveform();

        /// <summary>
        /// </summary>
        public void DisposeWaveform()
        {
            DisposeSlices();
            base.Destroy();
        }

        private void DisposeSlices()
        {
            foreach (var slice in Slices)
                slice.Destroy();

            Slices.Clear();
        }
    }
}