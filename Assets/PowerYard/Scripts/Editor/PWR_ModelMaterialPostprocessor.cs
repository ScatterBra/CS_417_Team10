using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Deadzone.Power.EditorTools
{
    /// <summary>
    /// PWR models only ship base color textures. Some FBX files still carry a white emissive color
    /// that was meant to be masked by an emission map; without the map the whole mesh glows white,
    /// so emission is turned off on any imported material that has no emission texture.
    /// Runs after URP's FBX material description preprocessor, which sets the emission.
    /// </summary>
    public class PWR_ModelMaterialPostprocessor : AssetPostprocessor
    {
        const string ModelDir = "Assets/PowerYard/Models/";

        public override int GetPostprocessOrder() => 100;

        void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (!assetPath.StartsWith(ModelDir)) return;
            if (!material.HasProperty("_EmissionColor")) return;
            if (material.HasProperty("_EmissionMap") && material.GetTexture("_EmissionMap") != null) return;
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
    }
}
