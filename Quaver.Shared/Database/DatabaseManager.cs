using Quaver.Shared.Config;
using SQLite;

namespace Quaver.Shared.Database
{
    public class DatabaseManager
    {
        /// <summary>
        /// </summary>
        public static SQLiteConnection Connection { get; private set; }

        /// <summary>
        /// </summary>
        public static void Initialize() => Connection = new SQLiteConnection(ConfigManager.DatabasePath.Value);
    }
}