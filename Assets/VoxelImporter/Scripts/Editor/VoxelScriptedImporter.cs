#if UNITY_2017_1_OR_NEWER

using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace VoxelImporter
{
    [ScriptedImporter(1, new string[] { "vox", "qb" })]
    public class VoxelScriptedImporter : ScriptedImporter
    {
        public VoxelBase.FileType fileType;
        public bool legacyVoxImport;
        public VoxelBase.ImportMode importMode = VoxelBase.ImportMode.LowPoly;
        public Vector3 importScale = Vector3.one;
        public Vector3 importOffset;
        public bool combineFaces = true;
        public bool ignoreCavity = true;
        public bool outputStructure;
        public bool generateLightmapUVs;
        public float generateLightmapUVsAngleError = 8f;
        public float generateLightmapUVsAreaError = 15f;
        public float generateLightmapUVsHardAngle = 88f;
        public float generateLightmapUVsPackMargin = 4f;
        public bool generateTangents;
        public float meshFaceVertexOffset;
        public bool retainExisting = true;
        public bool loadFromVoxelFile = true;
        public bool generateMipMaps;
        public enum ColliderType
        {
            None,
            Box,
            Sphere,
            Capsule,
            Mesh,
        }
        public ColliderType colliderType;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            {
                var ext = Path.GetExtension(ctx.assetPath).ToLower();
                if (ext == ".vox") fileType = VoxelBase.FileType.vox;
                else if (ext == ".qb") fileType = VoxelBase.FileType.qb;
                else return;
            }
            var gameObject = new GameObject(Path.GetFileNameWithoutExtension(ctx.assetPath));
            var voxelObject = gameObject.AddComponent<VoxelObject>();
            {
                voxelObject.legacyVoxImport = legacyVoxImport;
                voxelObject.importMode = importMode;
                voxelObject.importScale = importScale;
                voxelObject.importOffset = importOffset;
                voxelObject.combineFaces = combineFaces;
                voxelObject.ignoreCavity = ignoreCavity;
                voxelObject.voxelStructure = outputStructure ? ScriptableObject.CreateInstance<VoxelStructure>() : null;
                voxelObject.generateLightmapUVs = generateLightmapUVs;
                voxelObject.generateLightmapUVsAngleError = generateLightmapUVsAngleError;
                voxelObject.generateLightmapUVsAreaError = generateLightmapUVsAreaError;
                voxelObject.generateLightmapUVsHardAngle = generateLightmapUVsHardAngle;
                voxelObject.generateLightmapUVsPackMargin = generateLightmapUVsPackMargin;
                voxelObject.generateTangents = generateTangents;
                voxelObject.meshFaceVertexOffset = meshFaceVertexOffset;
                voxelObject.loadFromVoxelFile = loadFromVoxelFile;
                voxelObject.generateMipMaps = generateMipMaps;
            }
            var objectCore = new VoxelObjectCore(voxelObject);
            try
            {
                if (!objectCore.Create(ctx.assetPath, null))
                {
                    Debug.LogErrorFormat("<color=green>[Voxel Importer]</color> ScriptedImporter error. file:{0}", ctx.assetPath);
                    DestroyImmediate(gameObject);
                    return;
                }
            }
            catch
            {
                Debug.LogErrorFormat("<color=green>[Voxel Importer]</color> ScriptedImporter error. file:{0}", ctx.assetPath);
                DestroyImmediate(gameObject);
                return;
            }

            #region Material
            if (retainExisting)
            {
                bool changed = false;
                var assets = AssetDatabase.LoadAllAssetsAtPath(ctx.assetPath);
                for (int i = 0; i < voxelObject.materials.Count; i++)
                {
                    var material = assets.FirstOrDefault(c => c.name == string.Format("mat{0}", i)) as Material;
                    if (material != null)
                    {
                        material.mainTexture = voxelObject.atlasTexture;
                        voxelObject.materials[i] = material;
                        changed = true;
                    }
                }
                if (changed)
                {
                    var renderer = gameObject.GetComponent<MeshRenderer>();
                    renderer.sharedMaterials = voxelObject.materials.ToArray();
                }
            }
            #endregion

            #region Collider
            switch (colliderType)
            {
            case ColliderType.Box:
                gameObject.AddComponent<BoxCollider>();
                break;
            case ColliderType.Sphere:
                gameObject.AddComponent<SphereCollider>();
                break;
            case ColliderType.Capsule:
                gameObject.AddComponent<CapsuleCollider>();
                break;
            case ColliderType.Mesh:
                gameObject.AddComponent<MeshCollider>();
                break;
            }
            #endregion

#if UNITY_2017_3_OR_NEWER
            ctx.AddObjectToAsset(gameObject.name, gameObject);
            ctx.AddObjectToAsset(voxelObject.mesh.name = "mesh", voxelObject.mesh);
            for (int i = 0; i < voxelObject.materials.Count; i++)
            {
                ctx.AddObjectToAsset(voxelObject.materials[i].name = string.Format("mat{0}", i), voxelObject.materials[i]);
            }
            ctx.AddObjectToAsset(voxelObject.atlasTexture.name = "tex", voxelObject.atlasTexture);
            if (voxelObject.voxelStructure != null)
            {
                ctx.AddObjectToAsset(voxelObject.voxelStructure.name = "structure", voxelObject.voxelStructure);
            }

            VoxelObject.DestroyImmediate(voxelObject);

            ctx.SetMainObject(gameObject);
#else
            ctx.SetMainAsset(gameObject.name, gameObject);
            ctx.AddSubAsset(voxelObject.mesh.name = "mesh", voxelObject.mesh);
            for (int i = 0; i < voxelObject.materials.Count; i++)
            {
                ctx.AddSubAsset(voxelObject.materials[i].name = string.Format("mat{0}", i), voxelObject.materials[i]);
            }
            ctx.AddSubAsset(voxelObject.atlasTexture.name = "tex", voxelObject.atlasTexture);
            if (voxelObject.voxelStructure != null)
            {
                ctx.AddSubAsset(voxelObject.voxelStructure.name = "structure", voxelObject.voxelStructure);
            }

            VoxelObject.DestroyImmediate(voxelObject);
#endif
        }
    }
}
#endif