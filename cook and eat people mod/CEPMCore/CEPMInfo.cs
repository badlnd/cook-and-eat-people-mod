using System.IO;
using System.Reflection;

namespace LC.CEPM.CEPMCore
{
    /// <summary>
    /// General info for mod setup.
    /// </summary>
    public class CEPMInfo
    {
        /// <summary>
        /// Returns the mod name.
        /// </summary>
        public const string name = "CookAndEatPeopleMod";

        /// <summary>
        /// Returns the mod Author(s).
        /// </summary>
        public const string authname = "fayemoddinggroup";

        /// <summary>
        /// Generates and returns a usable GUID from the name and author name
        /// </summary>
        /// <returns></returns>
        public const string longGuid = name + "." + authname;

        /// <summary>
        /// Returns the short GUID.
        /// </summary>
        public const string guid = "CEPM";

        /// <summary>
        /// Returns the current mod version.
        /// </summary>
        public const string version = "1.0.0";

        /// <summary>
        /// Returns the string used for startup.
        /// </summary>
        public const string StartupString = name + " Initialising...";

        /// <summary>
        /// The assembly location.
        /// </summary>
        public static string AssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}
