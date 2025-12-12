#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class MCPMenuForceRefresher
{
    static MCPMenuForceRefresher()
    {
        EditorApplication.delayCall += () =>
        {
            AssetDatabase.Refresh();
            Debug.Log("MCP menu refresh attempted.");
        };
    }
}
#endif
