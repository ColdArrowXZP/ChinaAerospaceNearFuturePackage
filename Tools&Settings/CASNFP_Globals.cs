using System.Reflection;
using System.IO;
using VehiclePhysics;

namespace ChinaAeroSpaceNearFuturePackage
{
    /// <summary>
    /// 该类用于配置版本号和程序集路径等。
    /// </summary>
    public static class CASNFP_Globals
    {
        /// <summary>
        /// 中国航天近未来包版本号。
        /// </summary>
        public const string CASNFP_VERSION = "1.0.0.0";
        private static string assemblyFile;
        private static string assemblyName;
        private static string assemblyPath;
        private static string settingsPath;
        /// <summary>
        /// 中国航天近未来包程序集文件。
        /// </summary>
        public static string AssemblyFile
        {
            get
            {
                return assemblyFile ?? (assemblyFile = Assembly.GetExecutingAssembly().Location);
            }
        }

        /// <summary>
        /// 中国航天近未来包程序集文件名。
        /// </summary>
        public static string AssemblyName
        {
            get
            {
                return assemblyName ?? (assemblyName = new FileInfo(AssemblyFile).Name);
            }
        }

        /// <summary>
        /// 中国航天近未来包程序集所在路径。
        /// 该字符串格式"...\GameData\CASNFP\"。
        /// </summary>
        public static string AssemblyPath
        {
            get
            {
                return assemblyPath
                    ?? (assemblyPath = AssemblyFile.Replace(AssemblyName, ""));
            }
        }
        /// <summary>
        /// 中国航天近未来包程序集设置文件路径。
        /// </summary>
        public static string SettingsPath
        {
            get
            {
                return settingsPath
                    ?? (settingsPath = Path.Combine(AssemblyPath, @"Settings\"));
            }
        }
    }
}
