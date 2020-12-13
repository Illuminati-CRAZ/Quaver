using System;
using System.Collections.Generic;
using Quaver.API.Maps;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.PlaceBatch;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.RemoveBatch;
using Quaver.Shared.Screens.Edit.Actions.Layers.Create;
using Wobble.Bindables;

namespace Quaver.Shared.Screens.Edit.Actions.Layers.Remove
{
    public class EditorActionRemoveLayer : IEditorAction
    {
        public EditorActionType Type { get; } = EditorActionType.RemoveLayer;

        private EditorActionManager ActionManager { get; }

        private Qua WorkingMap { get; }

        private EditorLayerInfo Layer { get; }

        /// <summary>
        ///     The list of objects that existed in this layer
        /// </summary>
        private List<HitObjectInfo> HitObjectsInLayer { get; set; }

        private BindableList<HitObjectInfo> SelectedHitObjects { get; }

        /// <summary>
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="workingMap"></param>
        /// <param name="selectedHitObjects"></param>
        /// <param name="layer"></param>
        public EditorActionRemoveLayer(EditorActionManager manager, Qua workingMap, BindableList<HitObjectInfo> selectedHitObjects,
            EditorLayerInfo layer)
        {
            ActionManager = manager;
            WorkingMap = workingMap;
            Layer = layer;
            SelectedHitObjects = selectedHitObjects;
        }

        public void Perform()
        {
            var index = WorkingMap.EditorLayers.IndexOf(Layer) + 1;

            HitObjectsInLayer = WorkingMap.HitObjects.FindAll(x => x.EditorLayer == index);

            new EditorActionRemoveHitObjectBatch(ActionManager, WorkingMap, HitObjectsInLayer, SelectedHitObjects).Perform();

            WorkingMap.EditorLayers.Remove(Layer);

            // Find HitObjects at the indices above it and update them
            var hitObjects = WorkingMap.HitObjects.FindAll(x => x.EditorLayer > index);
            hitObjects.ForEach(x => x.EditorLayer--);

            ActionManager.TriggerEvent(EditorActionType.RemoveLayer, new EditorLayerRemovedEventArgs(Layer));
        }

        public void Undo()
        {
            new EditorActionPlaceHitObjectBatch(ActionManager, WorkingMap, HitObjectsInLayer, SelectedHitObjects).Perform();
            new EditorActionCreateLayer(WorkingMap, ActionManager, SelectedHitObjects, Layer).Perform();
            HitObjectsInLayer.ForEach(x => x.EditorLayer = WorkingMap.EditorLayers.Count);
        }
    }
}