using System.IO;
using System.Reflection;

namespace UnityEditor.Sequences
{
    internal static class PackageUtility
    {
        public static readonly string packageName = "com.unity.sequences";
        public static readonly string packageBaseFolder = Path.Combine("Packages", packageName);
        public static readonly string editorResourcesFolder = Path.Combine(packageBaseFolder, "Editor/Editor Default Resources");

        internal static bool GetPackageVersion(out SemanticVersion version)
        {
            version = new SemanticVersion();

            var assembly = Assembly.GetExecutingAssembly();
            var info = PackageManager.PackageInfo.FindForAssembly(assembly);
            return SemanticVersion.TryGetVersionInfo(info.version, out version);
        }
    }
}
