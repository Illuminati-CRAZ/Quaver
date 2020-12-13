using System.Collections.Generic;
using Quaver.API.Maps;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.PlaceBatch;
using Wobble.Bindables;

namespace Quaver.Shared.Screens.Edit.Actions.HitObjects.RemoveBatch
{
    public class EditorActionRemoveHitObjectBatch : IEditorAction
    {
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public EditorActionType Type { get; } = EditorActionType.RemoveHitObjectBatch;

        /// <summary>
        /// </summary>
        private EditorActionManager ActionManager { get; }

        /// <summary>
        /// </summary>
        private Qua WorkingMap { get; }

        /// <summary>
        /// </summary>
        private List<HitObjectInfo> HitObjects { get; }

        /// <summary>
        /// </summary>
        private BindableList<HitObjectInfo> SelectedHitObjects { get; }

        /// <summary>
        /// </summary>
        /// <param name="actionManager"></param>
        /// <param name="workingMap"></param>
        /// <param name="hitObjects"></param>
        public EditorActionRemoveHitObjectBatch(EditorActionManager actionManager, Qua workingMap, List<HitObjectInfo> hitObjects, BindableList<HitObjectInfo> selectedHitObjects)
        {
            ActionManager = actionManager;
            WorkingMap = workingMap;
            HitObjects = hitObjects;
            SelectedHitObjects = selectedHitObjects;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Perform()
        {
            HitObjects.ForEach(x => {WorkingMap.HitObjects.Remove(x); SelectedHitObjects.Remove(x);});
            WorkingMap.Sort();

            ActionManager.TriggerEvent(EditorActionType.RemoveHitObjectBatch, new EditorHitObjectBatchRemovedEventArgs(HitObjects));
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Undo() => new EditorActionPlaceHitObjectBatch(ActionManager, WorkingMap, HitObjects, SelectedHitObjects)?.Perform();
    }
}