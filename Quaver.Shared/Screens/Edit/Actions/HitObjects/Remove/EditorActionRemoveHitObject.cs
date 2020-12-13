using Quaver.API.Maps;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Screens.Edit.Actions.HitObjects.Place;
using Wobble.Bindables;

namespace Quaver.Shared.Screens.Edit.Actions.HitObjects.Remove
{
    public class EditorActionRemoveHitObject : IEditorAction
    {
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public EditorActionType Type { get; } = EditorActionType.RemoveHitObject;

        /// <summary>
        /// </summary>
        private EditorActionManager ActionManager { get; }

        /// <summary>
        /// </summary>
        private Qua WorkingMap { get; }

        /// <summary>
        /// </summary>
        private HitObjectInfo HitObject { get; }

        /// <summary>
        /// </summary>
        private BindableList<HitObjectInfo> SelectedHitObjects { get; }

        /// <summary>
        /// </summary>
        /// <param name="actionManager"></param>
        /// <param name="workingMap"></param>
        /// <param name="hitObject"></param>
        public EditorActionRemoveHitObject(EditorActionManager actionManager, Qua workingMap, HitObjectInfo hitObject, BindableList<HitObjectInfo> selectedHitObjects)
        {
            ActionManager = actionManager;
            WorkingMap = workingMap;
            HitObject = hitObject;
            SelectedHitObjects = selectedHitObjects;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Perform()
        {
            WorkingMap.HitObjects.Remove(HitObject);
            WorkingMap.Sort();

            SelectedHitObjects.Remove(HitObject);

            ActionManager.TriggerEvent(EditorActionType.RemoveHitObject, new EditorHitObjectRemovedEventArgs(HitObject));
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Undo() => new EditorActionPlaceHitObject(ActionManager, WorkingMap, HitObject, SelectedHitObjects).Perform();
    }
}