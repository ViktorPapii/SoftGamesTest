using UnityEditor;

namespace SoftGames.PhoenixFlame.EditorTools
{
    internal static class EditorFolders
    {
        // Creates every missing level of an "Assets/a/b/c" path.
        internal static void Ensure(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string built = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{built}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(built, parts[i]);
                }

                built = next;
            }
        }
    }
}
