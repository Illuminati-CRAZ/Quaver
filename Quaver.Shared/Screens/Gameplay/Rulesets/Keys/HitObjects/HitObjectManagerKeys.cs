/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MoreLinq;
using Quaver.API.Enums;
using Quaver.API.Maps;
using Quaver.API.Maps.Processors.Scoring.Data;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Audio;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Menu.Border;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Screens.Gameplay.Rulesets.HitObjects;
using Quaver.Shared.Screens.Gameplay.Rulesets.Input;
using Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield;
using Quaver.Shared.Screens.Selection;
using Wobble;
using Wobble.Audio.Tracks;
using Wobble.Bindables;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Logging;
using Wobble.Window;

namespace Quaver.Shared.Screens.Gameplay.Rulesets.Keys.HitObjects
{
    public class HitObjectManagerKeys : HitObjectManager
    {
        /// <summary>
        ///     Used to Round TrackPosition from Long to Float
        /// </summary>
        public static float TrackRounding { get; } = 100;

        /// <summary>
        ///     The speed at which objects fall down from the screen.
        /// </summary>
        public static float ScrollSpeed
        {
            get
            {
                var speed = ConfigManager.ScrollSpeed4K;

                if (MapManager.Selected.Value.Qua != null)
                    speed = MapManager.Selected.Value.Qua.Mode == GameMode.Keys4 ? ConfigManager.ScrollSpeed4K : ConfigManager.ScrollSpeed7K;

                var scalingFactor = QuaverGame.SkinScalingFactor;

                var game = GameBase.Game as QuaverGame;

                if (game?.CurrentScreen is IHasLeftPanel)
                    scalingFactor = (1920f - GameplayPlayfieldKeys.PREVIEW_PLAYFIELD_WIDTH) / 1366f;

                var scrollSpeed = (speed.Value / 10f) / (20f * AudioEngine.Track.Rate) * scalingFactor * WindowManager.BaseToVirtualRatio;

                return scrollSpeed;
            }
        }

        /// <summary>
        ///     Reference to the ruleset this HitObject manager is for.
        /// </summary>
        public GameplayRulesetKeys Ruleset { get; }

        /// <summary>
        ///     Qua with normalized SVs.
        /// </summary>
        private Qua Map;

        /// <summary>
        ///     Length of the Map.
        /// </summary>
        private int MapLength { get; }

        /// <summary>
        ///     Hit Object info used for object pool and gameplay
        ///     Every hit object in the pool is split by the hit object's lane
        /// </summary>
        public List<Queue<HitObjectInfo>> HitObjectQueueLanes { get; set; }

        /// <summary>
        ///     Object pool for every hit object.
        ///     Every hit object in the pool is split by the hit object's lane
        /// </summary>
        public List<Queue<GameplayHitObjectKeys>> ActiveNoteLanes { get; set; }

        /// <summary>
        ///     The list of dead notes (grayed out LN's)
        ///     Every hit object in the pool is split by the hit object's lane
        /// </summary>
        public List<Queue<GameplayHitObjectKeys>> DeadNoteLanes { get; private set; }

        /// <summary>
        ///     The list of currently held long notes.
        ///     Every hit object in the pool is split by the hit object's lane
        /// </summary>
        public List<Queue<GameplayHitObjectKeys>> HeldLongNoteLanes { get; private set; }

        /// <summary>
        ///     List of added hit object positions calculated from SV. Used for optimization
        /// </summary>
        public List<long> VelocityPositionMarkers { get; set; } = new List<long>();

        /// <summary>
        ///     The object pool size.
        /// </summary>
        public int InitialPoolSizePerLane { get; } = 2;

        /// <summary>
        ///     Used to determine the max position for object pooling recycling/creation.
        /// </summary>
        private float ObjectPositionMagnitude { get; } = 300000;

        /// <summary>
        ///     The position at which the next Hit Object must be at in order to add a new Hit Object to the pool.
        ///     TODO: Update upon scroll speed changes
        /// </summary>
        public float CreateObjectPositionThreshold { get; private set; }

        /// <summary>
        ///     The position at which the earliest Hit Object must be at before its recycled.
        ///     TODO: Update upon scroll speed changes
        /// </summary>
        public float RecycleObjectPositionThreshold { get; private set; }

        /// <summary>
        ///     A new hitobject is added to the pool if the next one is needs to be hit within this many milliseconds
        ///     TODO: Update upon scroll speed changes
        /// </summary>
        public float CreateObjectTimeThreshold { get; private set; }

        /// <summary>
        ///     Current position for Hit Objects.
        /// </summary>
        public long CurrentTrackPosition { get; private set; }

        /// <summary>
        ///     Current SV index used for optimization when using UpdateCurrentPosition()
        ///     Default value is 0. "0" means that Current time has not passed first SV point yet.
        /// </summary>
        private int CurrentSvIndex { get; set; } = 0;

        /// <summary>
        ///     Current audio position with song and user offset values applied.
        /// </summary>
        public double CurrentAudioPosition { get; private set; }

        /// <summary>
        ///     Current audio position with song, user and visual offset values applied.
        /// </summary>
        public double CurrentVisualPosition { get; private set; }

        /// <summary>
        ///     A mapping from hit objects to the associated hit stats from a replay.
        ///
        ///     Set to null when not applicable (e.g. outside of a replay).
        /// </summary>
        public Dictionary<HitObjectInfo, List<HitStat>> HitStats { get; private set; }

        /// <summary>
        ///     Note alpha when showing hits.
        /// </summary>
        public const float SHOW_HITS_NOTE_ALPHA = 0.3f;

        /// <summary>
        ///     Whether hits are currently shown.
        /// </summary>
        private bool _showHits = false;
        public bool ShowHits
        {
            get => _showHits;
            set
            {
                if (HitStats == null)
                    return;

                _showHits = value;

                foreach (GameplayHitObjectKeys hitObject in ActiveNoteLanes.Concat(DeadNoteLanes).Concat(HeldLongNoteLanes).Flatten())
                {
                    var tint = hitObject.Tint * (_showHits ? 1 : SHOW_HITS_NOTE_ALPHA);
                    var newTint = hitObject.Tint * (_showHits ? SHOW_HITS_NOTE_ALPHA : 1);

                    hitObject.HitObjectSprite.Tint = tint;
                    hitObject.HitObjectSprite.ClearAnimations();
                    hitObject.HitObjectSprite.FadeToColor(newTint, Easing.OutQuad, 250);
                    hitObject.LongNoteBodySprite.Tint = tint;
                    hitObject.LongNoteBodySprite.ClearAnimations();
                    hitObject.LongNoteBodySprite.FadeToColor(newTint, Easing.OutQuad, 250);
                    hitObject.LongNoteEndSprite.Tint = tint;
                    hitObject.LongNoteEndSprite.ClearAnimations();
                    hitObject.LongNoteEndSprite.FadeToColor(newTint, Easing.OutQuad, 250);
                }

                var playfield = (GameplayPlayfieldKeys) Ruleset.Playfield;

                playfield.Stage.HitContainer.Children.ForEach(x =>
                {
                    if (!(x is Sprite sprite))
                        return;

                    if (_showHits)
                    {
                        sprite.Alpha = 0;
                        sprite.FadeTo(1, Easing.OutQuad, 250);
                    }
                    else
                    {
                        sprite.Alpha = 1;
                        sprite.FadeTo(0, Easing.OutQuad, 250);
                    }
                });
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override bool IsComplete
        {
            get
            {
                // If there are objects to hit, we're not done.
                if (ActiveNoteLanes.Any(lane => lane.Any()))
                    return false;

                // If there are held LNs, we're not done.
                if (HeldLongNoteLanes.Any(lane => lane.Any()))
                    return false;

                // If there are dead LNs, we're done when we're past the map length.
                if (DeadNoteLanes.Any(lane => lane.Any()))
                    // If this is "return false;" then the game never ends if the map ends with an LN and a 0× SV
                    // and the LN is missed. This is because it never leaves DeadNoteLanes since the playfield doesn't
                    // move.
                    return CurrentVisualPosition > MapLength;

                // If there are no objects left, we're done.
                return true;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override HitObjectInfo NextHitObject
        {
            get
            {
                HitObjectInfo nextObject = null;

                var earliestObjectTime = int.MaxValue;

                // Some objects are already queued in ActiveNoteLanes, check that first.
                foreach (var objectsInLane in ActiveNoteLanes)
                {
                    if (objectsInLane.Count == 0)
                        continue;

                    var hitObject = objectsInLane.Peek();

                    if (hitObject.Info.StartTime >= earliestObjectTime)
                        continue;

                    earliestObjectTime = hitObject.Info.StartTime;
                    nextObject = hitObject.Info;
                }

                foreach (var objectsInLane in HitObjectQueueLanes)
                {
                    if (objectsInLane.Count == 0)
                        continue;

                    var hitObject = objectsInLane.Peek();

                    if (hitObject.StartTime >= earliestObjectTime)
                        continue;

                    earliestObjectTime = hitObject.StartTime;
                    nextObject = hitObject;
                }

                return nextObject;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override bool OnBreak
        {
            get
            {
                var nextObject = NextHitObject;

                if (nextObject == null)
                    return false;

                var isHoldingAnyNotes = false;

                foreach (var laneObjects in HeldLongNoteLanes)
                {
                    if (laneObjects.Count == 0)
                        continue;

                    isHoldingAnyNotes = true;
                }

                return !(nextObject.StartTime - CurrentAudioPosition < GameplayAudioTiming.StartDelay + 5000) && !isHoldingAnyNotes;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="ruleset"></param>
        /// <param name="size"></param>
        public HitObjectManagerKeys(GameplayRulesetKeys ruleset, Qua map) : base(map)
        {
            Ruleset = ruleset;
            Map = map.WithNormalizedSVs();
            MapLength = Map.Length;

            // Initialize SV
            UpdatePoolingPositions();
            InitializePositionMarkers();
            UpdateCurrentTrackPosition();
            InitializeBuckets();

            InitializeHitStats();

            // Initialize Object Pool
            InitializeInfoPool(map);
            InitializeObjectPool();

            AudioEngine.Track.RateChanged += OnRateChanged;
            ConfigManager.ScrollSpeed4K.ValueChanged += On4KScrollSpeedChanged;
            ConfigManager.ScrollSpeed7K.ValueChanged += On7KScrollSpeedChanged;
        }

        public override void Destroy()
        {
            AudioEngine.Track.RateChanged -= OnRateChanged;

            // ReSharper disable twice DelegateSubtraction
            ConfigManager.ScrollSpeed4K.ValueChanged -= On4KScrollSpeedChanged;
            ConfigManager.ScrollSpeed7K.ValueChanged -= On7KScrollSpeedChanged;

            base.Destroy();
        }

        /// <summary>
        ///     Fills in the HitStats dictionary.
        /// </summary>
        private void InitializeHitStats()
        {
            // Don't show hit stats in the song select preview.
            if (Ruleset.Screen.IsSongSelectPreview)
                return;

            var inputManager = ((KeysInputManager) Ruleset.InputManager).ReplayInputManager;

            if (inputManager == null)
                return;

            HitStats = new Dictionary<HitObjectInfo, List<HitStat>>();

            foreach (var hitStat in inputManager.VirtualPlayer.ScoreProcessor.Stats)
            {
                if (!HitStats.ContainsKey(hitStat.HitObject))
                    HitStats.Add(hitStat.HitObject, new List<HitStat>());

                HitStats[hitStat.HitObject].Add(hitStat);
            }
        }

        /// <summary>
        ///     Initialize Info Pool. Info pool is used to pass info around to Hit Objects.
        /// </summary>
        /// <param name="map"></param>
        private void InitializeInfoPool(Qua map, bool skipObjects = false)
        {
            // Initialize collections
            var keyCount = Ruleset.Map.GetKeyCount(map.HasScratchKey);
            HitObjectQueueLanes = new List<Queue<HitObjectInfo>>(keyCount);
            ActiveNoteLanes = new List<Queue<GameplayHitObjectKeys>>(keyCount);
            DeadNoteLanes = new List<Queue<GameplayHitObjectKeys>>(keyCount);
            HeldLongNoteLanes = new List<Queue<GameplayHitObjectKeys>>(keyCount);

            // Add HitObject Info to Info pool
            for (var i = 0; i < keyCount; i++)
            {
                HitObjectQueueLanes.Add(new Queue<HitObjectInfo>());
                ActiveNoteLanes.Add(new Queue<GameplayHitObjectKeys>(InitialPoolSizePerLane));
                DeadNoteLanes.Add(new Queue<GameplayHitObjectKeys>());
                HeldLongNoteLanes.Add(new Queue<GameplayHitObjectKeys>());
            }

            // Sort Hit Object Info into their respective lanes
            foreach (var info in map.HitObjects)
            {
                // Skip objects that aren't a second within range
                if (skipObjects)
                {
                    if (!info.IsLongNote)
                    {
                        if (info.StartTime < CurrentAudioPosition)
                            continue;
                    }
                    else
                    {
                        if (info.StartTime < CurrentAudioPosition && info.EndTime < CurrentAudioPosition)
                            continue;
                    }
                }

                HitObjectQueueLanes[info.Lane - 1].Enqueue(info);
            }
        }

        /// <summary>
        ///     Create the initial objects in the object pool
        /// </summary>
        private void InitializeObjectPool()
        {
            foreach (var lane in HitObjectQueueLanes)
            {
                for (var i = 0; i < InitialPoolSizePerLane && lane.Count > 0; i++)
                {
                    CreatePoolObject(lane.Dequeue());
                }
            }
        }

        /// <summary>
        ///     Create new Hit Object and add it into the pool with respect to its lane
        /// </summary>
        /// <param name="info"></param>
        private void CreatePoolObject(API.Maps.Structures.HitObjectInfo info) => ActiveNoteLanes[info.Lane - 1].Enqueue(new GameplayHitObjectKeys(info, Ruleset, this));

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            UpdateCurrentTrackPosition();
            UpdateAndScoreActiveObjects();
            UpdateAndScoreHeldObjects();
            UpdateDeadObjects();
        }

        /// <summary>
        ///     Returns the earliest un-tapped Hit Object
        /// </summary>
        /// <param name="laneIndex"></param>
        /// <returns></returns>
        public GameplayHitObjectKeys GetClosestTap(int lane) => ActiveNoteLanes[lane].Count > 0 ? ActiveNoteLanes[lane].Peek() : null;

        /// <summary>
        ///     Returns the earliest active Long Note
        /// </summary>
        /// <param name="laneIndex"></param>
        /// <returns></returns>
        public GameplayHitObjectKeys GetClosestRelease(int lane) => HeldLongNoteLanes[lane].Count > 0 ? HeldLongNoteLanes[lane].Peek() : null;

        /// <summary>
        ///     Updates the active objects in the pool + adds to score when applicable.
        /// </summary>
        private void UpdateAndScoreActiveObjects()
        {
            // Add more hit objects to the pool if necessary
            foreach (var lane in HitObjectQueueLanes)
            {
                while (lane.Count > 0 && ((Math.Abs(CurrentTrackPosition - GetPositionFromTime(lane.Peek().StartTime)) < CreateObjectPositionThreshold) ||
                      (lane.Peek().StartTime - CurrentAudioPosition < CreateObjectTimeThreshold)))
                {
                    CreatePoolObject(lane.Dequeue());
                }
            }

            ScoreActiveObjects();

            // Update active objects.
            foreach (var lane in ActiveNoteLanes)
            {
                foreach (var hitObject in lane)
                    hitObject.UpdateSpritePositions(CurrentTrackPosition, CurrentVisualPosition);
            }
        }

        /// <summary>
        /// </summary>
        private void ScoreActiveObjects()
        {
            if (Ruleset.Screen.Failed)
                return;

            // Check to see if the player missed any active notes
            foreach (var lane in ActiveNoteLanes)
            {
                while (lane.Count > 0 && (int)CurrentAudioPosition > lane.Peek().Info.StartTime + Ruleset.ScoreProcessor.JudgementWindow[Judgement.Okay])
                {
                    // Current hit object
                    var hitObject = lane.Dequeue();

                    // Update scoreboard for simulated plays
                    var screenView = (GameplayScreenView)Ruleset.Screen.View;
                    screenView.UpdateScoreboardUsers();

                    // Add new hit stat data and update score
                    var stat = new HitStat(HitStatType.Miss, KeyPressType.None, hitObject.Info, hitObject.Info.StartTime, Judgement.Miss,
                                            int.MinValue, Ruleset.ScoreProcessor.Accuracy, Ruleset.ScoreProcessor.Health);
                    Ruleset.ScoreProcessor.Stats.Add(stat);

                    var im = Ruleset.InputManager as KeysInputManager;

                    if (im?.ReplayInputManager == null)
                        Ruleset.ScoreProcessor.CalculateScore(Judgement.Miss);

                    var view = (GameplayScreenView)Ruleset.Screen.View;
                    view.UpdateScoreAndAccuracyDisplays();

                    // Perform Playfield animations
                    var playfield = (GameplayPlayfieldKeys)Ruleset.Playfield;

                    if (im?.ReplayInputManager == null)
                    {
                        playfield.Stage.ComboDisplay.MakeVisible();
                        playfield.Stage.JudgementHitBursts[Math.Clamp(hitObject.Info.Lane - 1, 0, playfield.Stage.JudgementHitBursts.Count - 1)].PerformJudgementAnimation(Judgement.Miss);
                    }

                    // If ManiaHitObject is an LN, kill it and count it as another miss because of the tail.
                    // - missing an LN counts as two misses
                    if (hitObject.Info.IsLongNote)
                    {
                        KillPoolObject(hitObject);

                        if (im?.ReplayInputManager == null)
                            Ruleset.ScoreProcessor.CalculateScore(Judgement.Miss, true);

                        view.UpdateScoreAndAccuracyDisplays();
                        Ruleset.ScoreProcessor.Stats.Add(stat);
                        screenView.UpdateScoreboardUsers();
                    }
                    // Otherwise just kill the object.
                    else
                    {
                        KillPoolObject(hitObject);
                    }
                }
            }
        }

        /// <summary>
        ///     Updates the held long note objects in the pool + adds to score when applicable.
        /// </summary>
        private void UpdateAndScoreHeldObjects()
        {
            ScoreHeldObjects();

            // Update the currently held long notes.
            foreach (var lane in HeldLongNoteLanes)
            {
                foreach (var hitObject in lane)
                    hitObject.UpdateSpritePositions(CurrentTrackPosition, CurrentVisualPosition);
            }
        }

        /// <summary>
        /// </summary>
        private void ScoreHeldObjects()
        {
            if (Ruleset.Screen.Failed)
                return;

            // The release window. (Window * Multiplier)
            var window = Ruleset.ScoreProcessor.JudgementWindow[Judgement.Okay] * Ruleset.ScoreProcessor.WindowReleaseMultiplier[Judgement.Okay];

            // Check to see if any LN releases were missed (Counts as an okay instead of a miss.)
            foreach (var lane in HeldLongNoteLanes)
            {
                while (lane.Count > 0 && (int)CurrentAudioPosition > lane.Peek().Info.EndTime + window)
                {
                    // Current hit object
                    var hitObject = lane.Dequeue();

                    // The judgement that is given when a user completely fails to release.
                    var missedReleaseJudgement = Judgement.Good;

                    // Add new hit stat data and update score
                    var stat = new HitStat(HitStatType.Miss, KeyPressType.None, hitObject.Info, hitObject.Info.EndTime, missedReleaseJudgement,
                                                int.MinValue, Ruleset.ScoreProcessor.Accuracy, Ruleset.ScoreProcessor.Health);

                    Ruleset.ScoreProcessor.Stats.Add(stat);

                    var im = Ruleset.InputManager as KeysInputManager;

                    if (im?.ReplayInputManager == null)
                        Ruleset.ScoreProcessor.CalculateScore(missedReleaseJudgement, true);

                    // Update scoreboard for simulated plays
                    var screenView = (GameplayScreenView)Ruleset.Screen.View;
                    screenView.UpdateScoreboardUsers();
                    screenView.UpdateScoreAndAccuracyDisplays();

                    // Perform Playfield animations
                    var stage = ((GameplayPlayfieldKeys)Ruleset.Playfield).Stage;

                    if (im?.ReplayInputManager == null)
                    {
                        stage.ComboDisplay.MakeVisible();
                        stage.JudgementHitBursts[Math.Clamp(hitObject.Info.Lane - 1, 0, stage.JudgementHitBursts.Count - 1)].PerformJudgementAnimation(missedReleaseJudgement);
                    }

                    stage.HitLightingObjects[hitObject.Info.Lane - 1].StopHolding();

                    // Update Pooling
                    RecyclePoolObject(hitObject);
                }
            }
        }

        /// <summary>
        ///     Updates all of the dead objects in the pool.
        /// </summary>
        private void UpdateDeadObjects()
        {
            // Check to see if dead object is ready for recycle
            foreach (var lane in DeadNoteLanes)
            {
                while (lane.Count > 0 &&
                    Math.Abs(CurrentTrackPosition - lane.Peek().LatestTrackPosition) > RecycleObjectPositionThreshold)
                {
                    RecyclePoolObject(lane.Dequeue());
                }
            }

            // Update dead objects.
            foreach (var lane in DeadNoteLanes)
            {
                foreach (var hitObject in lane)
                {
                    hitObject.UpdateSpritePositions(CurrentTrackPosition, CurrentVisualPosition);
                }
            }
        }

        /// <summary>
        ///     Force update LN Size if user changes scroll speed settings during gameplay.
        /// </summary>
        public void ForceUpdateLNSize()
        {
            // Update Object Reference Positions with new scroll speed
            UpdatePoolingPositions();

            // Update HitObject LN size
            for (var i = 0; i < ActiveNoteLanes.Count; i++)
            {
                foreach (var hitObject in ActiveNoteLanes[i])
                    hitObject.ForceUpdateLongnote(CurrentTrackPosition, CurrentVisualPosition);
                foreach (var hitObject in DeadNoteLanes[i])
                    hitObject.ForceUpdateLongnote(CurrentTrackPosition, CurrentVisualPosition);
                foreach (var hitObject in HeldLongNoteLanes[i])
                    hitObject.ForceUpdateLongnote(CurrentTrackPosition, CurrentVisualPosition);
            }
        }

        /// <summary>
        ///     Update Hitobject pooling positions to compensate for scroll speed.
        /// </summary>
        private void UpdatePoolingPositions()
        {
            RecycleObjectPositionThreshold = ObjectPositionMagnitude / ScrollSpeed;
            CreateObjectPositionThreshold = ObjectPositionMagnitude / ScrollSpeed;

            CreateObjectTimeThreshold = ObjectPositionMagnitude / ScrollSpeed / TrackRounding;
        }

        /// <summary>
        ///     Kills a note at a specific index of the object pool.
        /// </summary>
        /// <param name="index"></param>
        public void KillPoolObject(GameplayHitObjectKeys gameplayHitObject)
        {
            // Change the sprite color to dead.
            gameplayHitObject.Kill();

            // Add to dead notes pool
            DeadNoteLanes[gameplayHitObject.Info.Lane - 1].Enqueue(gameplayHitObject);
        }

        /// <summary>
        ///     Recycles a pool object.
        /// </summary>
        /// <param name="index"></param>
        public void RecyclePoolObject(GameplayHitObjectKeys gameplayHitObject)
        {
            var lane = HitObjectQueueLanes[gameplayHitObject.Info.Lane - 1];
            if (lane.Count > 0)
            {
                var info = lane.Dequeue();
                gameplayHitObject.InitializeObject(this, info);
                ActiveNoteLanes[info.Lane - 1].Enqueue(gameplayHitObject);
            }
            else
            {
                gameplayHitObject.Destroy();
            }
        }

        /// <summary>
        ///     Changes a pool object to a long note that is held at the receptors.
        /// </summary>
        /// <param name="index"></param>
        public void ChangePoolObjectStatusToHeld(GameplayHitObjectKeys gameplayHitObject)
        {
            // Add to the held long notes.
            HeldLongNoteLanes[gameplayHitObject.Info.Lane - 1].Enqueue(gameplayHitObject);
            gameplayHitObject.CurrentlyBeingHeld = true;
        }

        /// <summary>
        ///     Kills a hold pool object.
        /// </summary>
        /// <param name="gameplayHitObject"></param>
        public void KillHoldPoolObject(GameplayHitObjectKeys gameplayHitObject, bool setTint = true)
        {
            // Change start time and LN size.
            gameplayHitObject.InitialTrackPosition = GetPositionFromTime(CurrentVisualPosition);
            gameplayHitObject.CurrentlyBeingHeld = false;
            gameplayHitObject.UpdateLongNoteSize(CurrentTrackPosition, CurrentVisualPosition);

            if (setTint)
                gameplayHitObject.Kill();

            // Add to dead notes pool
            DeadNoteLanes[gameplayHitObject.Info.Lane - 1].Enqueue(gameplayHitObject);
        }


        /// <summary>
        ///     Create SV-position points for computation optimization
        /// </summary>
        private void InitializePositionMarkers()
        {
            if (Map.SliderVelocities.Count == 0)
                return;

            // Compute for Change Points
            var position = (long)(Map.SliderVelocities[0].StartTime * Map.InitialScrollVelocity * TrackRounding);
            VelocityPositionMarkers.Add(position);

            for (var i = 1; i < Map.SliderVelocities.Count; i++)
            {
                position += (long)((Map.SliderVelocities[i].StartTime - Map.SliderVelocities[i - 1].StartTime)
                                   * Map.SliderVelocities[i - 1].Multiplier * TrackRounding);
                VelocityPositionMarkers.Add(position);
            }
        }

        /// <summary>
        ///     Get Hit Object (End/Start) position from audio time (Unoptimized.)
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public long GetPositionFromTime(double time)
        {
            int i;
            for (i = 0; i < Map.SliderVelocities.Count; i++)
            {
                if (time < Map.SliderVelocities[i].StartTime)
                    break;
            }

            return GetPositionFromTime(time, i);
        }

        /// <summary>
        ///     Get Hit Object (End/Start) position from audio time and SV Index.
        ///     Index used for optimization
        /// </summary>
        /// <param name="time"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public long GetPositionFromTime(double time, int index)
        {
            // NoSV Modifier is toggled on
            if (Ruleset.ScoreProcessor.Mods.HasFlag(ModIdentifier.NoSliderVelocity))
                return (long)(time * TrackRounding);

            if (index == 0)
            {
                // Time starts before the first SV point
                return (long) (time * Map.InitialScrollVelocity * TrackRounding);
            }

            index--;

            var curPos = VelocityPositionMarkers[index];
            curPos += (long)((time - Map.SliderVelocities[index].StartTime) * Map.SliderVelocities[index].Multiplier * TrackRounding);
            return curPos;
        }

        // indices of SVs associated with intervals of position
        // used for finding times associated with a position
        public List<long> BucketPositions { get; private set; }
        public List<List<int>> ScrollVelocityBuckets { get; private set; }
        public Dictionary<long, List<(float, float)>> ConstPositionIntervals { get; private set; }

        private void AddConstPositionInterval(long position, (float, float) timeInterval)
        {
            if (!ConstPositionIntervals.ContainsKey(position))
                ConstPositionIntervals.Add(position, new List<(float, float)>());

            List<(float, float)> intervals;
            if (!ConstPositionIntervals.TryGetValue(position, out intervals))
                throw new Exception("This shouldn't happen");

            intervals.Add(timeInterval);

            Logger.Debug("Added const position interval: " + position + ", " + timeInterval, LogType.Runtime);
        }

        // make sure weird numbers +-inf, nan, don't break stuff, such as when using Math.Sign()
        private void InitializeBuckets()
        {
            BucketPositions = new List<long>();
            ScrollVelocityBuckets = new List<List<int>>();
            ConstPositionIntervals = new Dictionary<long, List<(float, float)>>();

            // first check initial scroll velocity
            int prevSign = Math.Sign(Map.InitialScrollVelocity); // what about weird numbers?

            if (prevSign == 0)
            {
                long pos = VelocityPositionMarkers.Count > 0 ? VelocityPositionMarkers[0] : 0;
                float endTime = Map.SliderVelocities.Count > 0 ? Map.SliderVelocities[0].StartTime : float.PositiveInfinity;

                AddConstPositionInterval(pos, (float.NegativeInfinity, endTime));
            }
            else
            {
                long bucketStart = prevSign == 1 ? long.MinValue : long.MaxValue;
                BucketPositions.Add(bucketStart);

                Logger.Debug("Added bucket at " + bucketStart, LogType.Runtime);
            }

            // then check normal SVs
            for (int i = 0; i < Map.SliderVelocities.Count; i++)
            {
                int currentSign = Math.Sign(Map.SliderVelocities[i].Multiplier); // what about the weird numbers, +-inf and nan?

                // handle 0x SVs differently
                if (currentSign == 0)
                {
                    float start = Map.SliderVelocities[i].StartTime;
                    float end = i != Map.SliderVelocities.Count - 1 ? Map.SliderVelocities[i + 1].StartTime : float.MaxValue;

                    AddConstPositionInterval(VelocityPositionMarkers[i], (start, end));

                    continue;
                }

                if (currentSign != prevSign)
                {
                    BucketPositions.Add(VelocityPositionMarkers[i]);
                    prevSign = currentSign;

                    Logger.Debug("Added bucket at " + VelocityPositionMarkers[i], LogType.Runtime);
                }
            }

            BucketPositions = BucketPositions.Distinct().ToList();
            BucketPositions.Sort();

            BucketPositions.ForEach(x => ScrollVelocityBuckets.Add(new List<int>()));

            // populate buckets
            // i == -1 indicates initial scroll velocity
            for (int i = -1; i < Map.SliderVelocities.Count; i++)
            {
                // find position range of sv
                long[] svPositions = new long[2];

                // initial position of sv
                if (i != -1)
                {
                    svPositions[0] = VelocityPositionMarkers[i];
                }
                else // positions before 1st SV are based on initial scroll velocity
                {
                    svPositions[0] = Math.Sign(Map.InitialScrollVelocity) switch // what if initial scroll velocity is a weird number?
                    {
                        1 => long.MinValue,
                        -1 => long.MaxValue,
                        0 => Map.SliderVelocities.Count > 0 ? VelocityPositionMarkers[0] : 0,
                        _ => throw new Exception("This should never happen")
                    };
                }

                // end position of sv
                if (i != VelocityPositionMarkers.Count - 1)
                {
                    svPositions[1] = VelocityPositionMarkers[i + 1];
                }
                else // last SV
                {
                    svPositions[1] = Math.Sign(Map.SliderVelocities.ElementAtOrDefault(i)?.Multiplier ?? Map.InitialScrollVelocity) switch
                    {
                        1 => long.MaxValue,
                        -1 => long.MinValue,
                        0 => Map.SliderVelocities.Count > 0 ? VelocityPositionMarkers[i] : 0,
                        _ => throw new Exception("This should never happen")
                    };
                }

                // skip if 0x SV, already handled
                if (svPositions[0] == svPositions[1])
                    continue;

                Array.Sort(svPositions);

                // add sv to all buckets where sv and bucket position ranges overlap
                int j = FindBucket(svPositions[0]);
                int direction = Math.Sign(i != -1 ? Map.SliderVelocities[i].Multiplier : Map.InitialScrollVelocity); // what about the weird numbers, +=inf and nan?

                while (true)
                {
                    if (j < 0 || j >= BucketPositions.Count)
                        break;

                    long bucketStartPos = BucketPositions[j];
                    long bucketEndPos = j != BucketPositions.Count - 1 ? BucketPositions[j + 1] : long.MaxValue;

                    // no overlap in range
                    if ((svPositions[1] <= bucketStartPos || svPositions[0] >= bucketEndPos))
                        break;

                    // overlaps in range
                    ScrollVelocityBuckets[j].Add(i);
                    j += direction;
                }
            }
        }

        private int FindBucket(long position)
        {
            return FindBucket(position, 0, BucketPositions.Count - 1);
        }

        // basically binary search
        private int FindBucket(long position, int a, int b)
        {
            if (a == b)
                return a;

            int i = (a + b) / 2;
            long start = BucketPositions[i];
            long end = BucketPositions[i + 1];

            if (start <= position && position < end)
            {
                return i;
            }
            else if (position < start)
            {
                return FindBucket(position, a, i - 1);
            }
            else
            {
                return FindBucket(position, i + 1, b);
            }
        }

        private int FindIntervalIndex<T>(List<T> intervals, T position) where T : IComparable<T>
        {
            return FindIntervalIndex(intervals, position, 0, intervals.Count - 1);
        }

        private int FindIntervalIndex<T>(List<T> intervals, T position, int a, int b) where T : IComparable<T>
        {
            if (a == b)
                return a;

            int i = (a + b) / 2;
            T start = intervals[i];
            T end = intervals[i + 1];

            int startRelation, endRelation;
            startRelation = position.CompareTo(start);
            endRelation = position.CompareTo(end);

            // start <= position && position < end
            if ((startRelation >= 0 || endRelation < 0))
            {
                return i;
            }
            // position < start
            else if (startRelation < 0)
            {
                return FindIntervalIndex(intervals, position, a, i - 1);
            }
            else
            {
                return FindIntervalIndex(intervals, position, i + 1, b);
            }
        }

        public (List<float> exactTimes, List<(float, float)> timeIntervals) GetTimesFromPosition(long position)
        {
            var bucket = ScrollVelocityBuckets[FindBucket(position)];
            var exactTimes = new List<float>();

            // check each sv in bucket
            Logger.Debug(bucket.Count + " SVs in bucket", LogType.Runtime);

            foreach (var i in bucket)
            {
                float time;

                // -1 means use initial scroll velocity
                if (i == -1)
                {
                    // skip if position is not within initial scroll velocity position range
                    long startPos = Math.Sign(Map.InitialScrollVelocity) == 1 ? long.MinValue : long.MaxValue;
                    long endPos = VelocityPositionMarkers.Count > 0 ? VelocityPositionMarkers[0] :
                                  Math.Sign(Map.InitialScrollVelocity) == 1 ? long.MaxValue : long.MinValue;

                    if (position < startPos || endPos <= position)
                        continue;

                    if (Map.SliderVelocities.Count == 0 || Map.SliderVelocities[0].StartTime >= 0)
                    {
                        time = (position / Map.InitialScrollVelocity) / TrackRounding;
                    }
                    else
                    {
                        long startingPos = VelocityPositionMarkers[0];
                        long deltaPos = position - startingPos;
                        float deltaTime = (deltaPos / Map.InitialScrollVelocity) / TrackRounding;
                        time = deltaTime + Map.SliderVelocities[0].StartTime;
                    }

                    exactTimes.Add(time);
                }
                // position is between current and next sv
                else if (VelocityPositionMarkers[i] <= position && position < (i != VelocityPositionMarkers.Count - 1 ? VelocityPositionMarkers[i + 1] : long.MaxValue))
                {
                    long deltaPosition = position - VelocityPositionMarkers[i];
                    float deltaTime = (deltaPosition / TrackRounding) / Map.SliderVelocities[i].Multiplier;
                    time = Map.SliderVelocities[i].StartTime + deltaTime;

                    exactTimes.Add(time);
                }
            }

            List<(float, float)> timeIntervals;
            if (!ConstPositionIntervals.TryGetValue(position, out timeIntervals))
                timeIntervals = new List<(float, float)>();

            return (exactTimes, timeIntervals);
        }

        // public U GetValue<T, U>(Dictionary<T, U> dict, T key)
        // {
        //     U value;
        //     if (!dict.TryGetValue(key, out value))
        //         throw new Exception("No value associated with key");

        //     return value;
        // }

        // public Dictionary<long, (List<float>, List<(float, float)>)> GetTimesFromPositions(List<long> positions)
        // {
        //     var result = new Dictionary<long, (List<float>, List<(float, float)>)>();

        //     positions.Sort();
        //     positions.ForEach(x => result.Add(x, (new List<float>(), new List<(float, float)>())));

        //     for (int i = 0; i < Map.SliderVelocities.Count - 1; i++)
        //     {
        //         Logger.Debug("Processing " + i + "/" + Map.SliderVelocities.Count + " SVs", LogType.Runtime);

        //         if (Map.SliderVelocities[i].Multiplier == 0)
        //         {
        //             if (result.ContainsKey(VelocityPositionMarkers[i]))
        //             {
        //                 GetValue(result, VelocityPositionMarkers[i]).Item2.Add((Map.SliderVelocities[i].StartTime, Map.SliderVelocities[i + 1].StartTime));
        //             }

        //             continue;
        //         }

        //         long svStartPosition, svEndPosition;
        //         float svStartTime, svEndTime;

        //         svStartPosition = VelocityPositionMarkers[i];
        //         svEndPosition = VelocityPositionMarkers[i + 1];
        //         svStartTime = Map.SliderVelocities[i].StartTime;
        //         svEndTime = Map.SliderVelocities[i + 1].StartTime;

        //         int startPositionIndex = FindIntervalIndex(positions, svStartPosition) + 1;
        //         int endPositionIndex = FindIntervalIndex(positions, svEndPosition);
        //         if (positions[endPositionIndex] == svEndPosition)
        //             endPositionIndex--;

        //         long distance = svEndPosition - svStartPosition;
        //         float duration = svEndTime - svStartTime;

        //         for (int j = startPositionIndex; j <= endPositionIndex; j++)
        //         {
        //             double t = (positions[j] - svStartPosition) / (distance);
        //             float time = (float)(duration * t + svStartTime);

        //             var times = GetValue(result, positions[j]);
        //             times.Item1.Add(time);
        //         }
        //     }

        //     return result;
        // }

        /// <summary>
        ///     Get SV direction changes between startTime and endTime.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public List<SVDirectionChange> GetSVDirectionChanges(double startTime, double endTime)
        {
            var changes = new List<SVDirectionChange>();

            if (Ruleset.ScoreProcessor.Mods.HasFlag(ModIdentifier.NoSliderVelocity))
                return changes;

            // Find the first SV index.
            int i;
            for (i = 0; i < Map.SliderVelocities.Count; i++)
            {
                if (startTime < Map.SliderVelocities[i].StartTime)
                    break;
            }

            bool forward;
            if (i == 0)
                forward = Map.InitialScrollVelocity >= 0;
            else
                forward = Map.SliderVelocities[i - 1].Multiplier >= 0;

            // Loop over SV changes between startTime and endTime.
            for (; i < Map.SliderVelocities.Count && endTime >= Map.SliderVelocities[i].StartTime; i++)
            {
                var multiplier = Map.SliderVelocities[i].Multiplier;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (multiplier == 0)
                    // Zero speed means we're staying in the same spot.
                    continue;

                if (forward == (multiplier > 0))
                    // The direction hasn't changed.
                    continue;

                forward = multiplier > 0;
                changes.Add(new SVDirectionChange
                {
                    StartTime = Map.SliderVelocities[i].StartTime,
                    Position = VelocityPositionMarkers[i]
                });
            }

            return changes;
        }

        /// <summary>
        ///     Returns true if the playfield is going backwards at the given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool IsSVNegative(double time)
        {
            if (Ruleset.ScoreProcessor.Mods.HasFlag(ModIdentifier.NoSliderVelocity))
                return false;

            // Find the SV index at time.
            int i;
            for (i = 0; i < Map.SliderVelocities.Count; i++)
            {
                if (time < Map.SliderVelocities[i].StartTime)
                    break;
            }

            i--;

            // Find index of the last non-zero SV.
            for (; i >= 0; i--)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (Map.SliderVelocities[i].Multiplier != 0)
                    break;
            }

            if (i == -1)
                return Map.InitialScrollVelocity < 0;

            return Map.SliderVelocities[i].Multiplier < 0;
        }

        /// <summary>
        ///     Update Current position of the hit objects
        /// </summary>
        /// <param name="audioTime"></param>
        public void UpdateCurrentTrackPosition()
        {
            CurrentAudioPosition = Ruleset.Screen.Timing.Time + ConfigManager.GlobalAudioOffset.Value * AudioEngine.Track.Rate
                                   - MapManager.Selected.Value.LocalOffset - MapManager.Selected.Value.OnlineOffset;

            CurrentVisualPosition = CurrentAudioPosition + ConfigManager.VisualOffset.Value * AudioEngine.Track.Rate;

            // Update SV index if necessary. Afterwards update Position.
            while (CurrentSvIndex < Map.SliderVelocities.Count && CurrentVisualPosition >= Map.SliderVelocities[CurrentSvIndex].StartTime)
            {
                CurrentSvIndex++;
            }
            CurrentTrackPosition = GetPositionFromTime(CurrentVisualPosition, CurrentSvIndex);
        }

        /// <summary>
        ///     Handles skipping forward in the pool
        /// </summary>
        public void HandleSkip()
        {
            DestroyAllObjects();

            CurrentSvIndex = 0;
            UpdateCurrentTrackPosition();

            InitializeInfoPool(Ruleset.Map, true);
            InitializeObjectPool();

            foreach (var timingLineManager in Ruleset.TimingLineManager)
            {
                timingLineManager.InitializeObjectPool();
            }

            Update(new GameTime());
        }

        /// <summary>
        /// </summary>
        public void DestroyAllObjects()
        {
            DestroyPoolList(ActiveNoteLanes);
            DestroyPoolList(HeldLongNoteLanes);
            DestroyPoolList(DeadNoteLanes);

            var playfield = (GameplayPlayfieldKeys)Ruleset.Playfield;

            for (var i = playfield.Stage.HitObjectContainer.Children.Count - 1; i >= 0; i--)
                playfield.Stage.HitObjectContainer.Children[i].Destroy();

            playfield.Stage.HitObjectContainer.Children.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="objects"></param>
        private void DestroyPoolList(List<Queue<GameplayHitObjectKeys>> objects)
        {
            foreach (var lane in objects)
            {
                while (lane.Count > 0)
                    lane.Dequeue().Destroy();
            }
        }

        private void OnRateChanged(object sender, TrackRateChangedEventArgs e) => ForceUpdateLNSize();

        private void On7KScrollSpeedChanged(object sender, BindableValueChangedEventArgs<int> e) => ForceUpdateLNSize();

        private void On4KScrollSpeedChanged(object sender, BindableValueChangedEventArgs<int> e) => ForceUpdateLNSize();
    }
}
