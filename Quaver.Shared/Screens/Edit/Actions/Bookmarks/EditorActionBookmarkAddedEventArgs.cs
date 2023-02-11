using System;
using Quaver.API.Maps.Structures;

namespace Quaver.Shared.Screens.Edit.Actions.Bookmarks
{
    public class EditorActionBookmarkAddedEventArgs : EventArgs
    {
        public BookmarkInfo Bookmark { get; }

        public EditorActionBookmarkAddedEventArgs(BookmarkInfo bookmark) => Bookmark = bookmark;
    }
}