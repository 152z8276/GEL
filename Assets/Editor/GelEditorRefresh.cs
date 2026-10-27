using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace GEL.EditorTools
{
    public static class GelEditorRefresh
    {
        [MenuItem("GEL/Force Recompile Scripts")]
        public static void ForceRecompile()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            CompilationPipeline.RequestScriptCompilation();
            Debug.Log("GEL force script compilation requested.");
        }
    }
}
