using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    [CustomEditor(typeof(VoxelObject))]
    public class VoxelObjectEditor : VoxelBaseEditor
    {
        public VoxelObject objectTarget { get; protected set; }
        public VoxelObjectCore objectCore { get; protected set; }

        public virtual Mesh mesh { get { return objectTarget.mesh; } set { objectTarget.mesh = value; } }
        public virtual List<Material> materials { get { return objectTarget.materials; } set { objectTarget.materials = value; } }
        public virtual Texture2D atlasTexture { get { return objectTarget.atlasTexture; } set { objectTarget.atlasTexture = value; } }

        protected override void OnEnable()
        {
            base.OnEnable();

            objectTarget = target as VoxelObject;

            if (objectTarget != null)
            {
                baseCore = objectCore = new VoxelObjectCore(objectTarget);
                OnEnableInitializeSet();
            }
        }

        protected override void InspectorGUI()
        {
            base.InspectorGUI();

#if UNITY_2018_3_OR_NEWER
            {
                if (prefabType == PrefabAssetType.Regular && !baseCore.isPrefabEditMode)
                {
                    EditorGUI.BeginDisabledGroup(true);
                }
            }
#endif

            InspectorGUI_Import();

            InspectorGUI_Object();

            InspectorGUI_Refresh();

#if UNITY_2018_3_OR_NEWER
            {
                if (prefabType == PrefabAssetType.Regular && !baseCore.isPrefabEditMode)
                {
                    EditorGUI.EndDisabledGroup();
                }
            }
#endif
        }

        protected virtual void InspectorGUI_Object()
        {
            #region Object
            if (!string.IsNullOrEmpty(baseTarget.voxelFilePath))
            {
                //Object
                baseTarget.edit_objectFoldout = EditorGUILayout.Foldout(baseTarget.edit_objectFoldout, "Object", guiStyleFoldoutBold);
                if (baseTarget.edit_objectFoldout)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    InspectorGUI_Object_Mesh();
                    InspectorGUI_Object_Material();
                    InspectorGUI_Object_Texture();
                    EditorGUILayout.EndVertical();
                }
            }
            #endregion
        }
        protected void TypeTitle(UnityEngine.Object o, string title)
        {
            if (o == null)
                EditorGUILayout.LabelField(title, guiStyleMagentaBold);
            else if (prefabEnable && !AssetDatabase.Contains(o))
                EditorGUILayout.LabelField(title, guiStyleRedBold);
            else
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
        protected virtual void InspectorGUI_Object_Mesh()
        {
            #region Mesh
            if (baseTarget.advancedMode)
            {
                TypeTitle(mesh, "Mesh");
                EditorGUI.indentLevel++;
                #region Mesh
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(mesh, typeof(Mesh), false);
                        EditorGUI.EndDisabledGroup();
                    }
                    if (mesh != null)
                    {
                        if (!IsMainAsset(mesh))
                        {
                            if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Create Mesh
                                string path = EditorUtility.SaveFilePanel("Save mesh", objectCore.GetDefaultPath(), string.Format("{0}_mesh.asset", baseTarget.gameObject.name), "asset");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    if (path.IndexOf(Application.dataPath) < 0)
                                    {
                                        SaveInsideAssetsFolderDisplayDialog();
                                    }
                                    else
                                    {
                                        UndoRecordObject("Save Mesh");
                                        path = path.Replace(Application.dataPath, "Assets");
                                        AssetDatabase.CreateAsset(Mesh.Instantiate(mesh), path);
                                        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                                        Refresh();
                                    }
                                }
                                #endregion
                            }
                        }
                        {
                            if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Reset Mesh
                                UndoRecordObject("Reset Mesh");
                                mesh = null;
                                Refresh();
                                #endregion
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
                InspectorGUI_Object_Mesh_Settings();
                #region Vertex Count
                {
                    EditorGUILayout.LabelField("Vertex Count", mesh != null ? mesh.vertexCount.ToString() : "");
                }
                #endregion
                EditorGUI.indentLevel--;
            }
            #endregion
        }
        protected virtual void InspectorGUI_Object_Material()
        {
            #region Material
            {
                #region Title
                {
                    if (materials == null || materials.Count == 0)
                        EditorGUILayout.LabelField("Material", guiStyleMagentaBold);
                    else if (prefabEnable)
                    {
                        bool contains = true;
                        for (int i = 0; i < materials.Count; i++)
                        {
                            if (materials[i] == null || !AssetDatabase.Contains(materials[i]))
                            {
                                contains = false;
                                break;
                            }
                        }
                        EditorGUILayout.LabelField("Material", contains ? EditorStyles.boldLabel : guiStyleRedBold);
                    }
                    else
                        EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
                }
                #endregion
                EditorGUI.indentLevel++;
                #region updateMeshRendererMaterials
                if (baseTarget.advancedMode)
                {
                    EditorGUI.BeginChangeCheck();
                    var updateMeshRendererMaterials = EditorGUILayout.ToggleLeft("Update the Mesh Renderer Materials", baseTarget.updateMeshRendererMaterials);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (EditorUtility.DisplayDialog("Update the Mesh Renderer Materials", "It will be changed.\nAre you sure?", "ok", "cancel"))
                        {
                            UndoRecordObject("Inspector");
                            baseTarget.updateMeshRendererMaterials = updateMeshRendererMaterials;
                            baseCore.SetRendererCompornent();
                        }
                    }
                }
                #endregion
                if (materialList != null)
                {
                    materialList.DoLayoutList();
                }
                InspectorGUI_ConfigureMaterial();
                EditorGUI.indentLevel--;
            }
            #endregion
        }
        protected virtual void InspectorGUI_Object_Texture()
        {
            #region Texture
            if (baseTarget.advancedMode)
            {
                TypeTitle(atlasTexture, "Texture");
                EditorGUI.indentLevel++;
                #region updateMaterialTexture
                {
                    EditorGUI.BeginChangeCheck();
                    var updateMaterialTexture = EditorGUILayout.ToggleLeft("Update the Material Texture", baseTarget.updateMaterialTexture);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (EditorUtility.DisplayDialog("Update the Material Texture", "It will be changed.\nAre you sure?", "ok", "cancel"))
                        {
                            UndoRecordObject("Inspector");
                            baseTarget.updateMaterialTexture = updateMaterialTexture;
                            baseCore.SetRendererCompornent();
                        }
                    }
                }
                #endregion
                #region Texture
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(atlasTexture, typeof(Texture2D), false);
                        EditorGUI.EndDisabledGroup();
                    }
                    if (atlasTexture != null)
                    {
                        if (!IsMainAsset(atlasTexture))
                        {
                            if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Create Texture
                                string path = EditorUtility.SaveFilePanel("Save atlas texture", objectCore.GetDefaultPath(), string.Format("{0}_tex.png", baseTarget.gameObject.name), "png");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    if (path.IndexOf(Application.dataPath) < 0)
                                    {
                                        SaveInsideAssetsFolderDisplayDialog();
                                    }
                                    else
                                    {
                                        UndoRecordObject("Save Atlas Texture");
                                        var newTex = Texture2D.Instantiate(atlasTexture);
                                        File.WriteAllBytes(path, newTex.EncodeToPNG());
                                        path = path.Replace(Application.dataPath, "Assets");
                                        AssetDatabase.ImportAsset(path);
                                        objectCore.SetTextureImporterSetting(path, newTex);
                                        atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                                        Refresh();
                                    }
                                }
                                #endregion
                            }
                        }
                        {
                            if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Reset Texture
                                UndoRecordObject("Reset Atlas Texture");
                                atlasTexture = null;
                                Refresh();
                                #endregion
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
                #region Generate Mip Maps
                if (!IsMainAsset(atlasTexture))
                {
                    EditorGUI.BeginChangeCheck();
                    var generateMipMaps = EditorGUILayout.Toggle("Generate Mip Maps", baseTarget.generateMipMaps);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UndoRecordObject("Inspector");
                        baseTarget.generateMipMaps = generateMipMaps;
                        Refresh();
                    }
                }
                #endregion
                #region Texture Size
                {
                    EditorGUILayout.LabelField("Texture Size", atlasTexture != null ? string.Format("{0} x {1}", atlasTexture.width, atlasTexture.height) : "");
                }
                #endregion
                EditorGUI.indentLevel--;
            }
            #endregion
        }

        protected override List<Material> GetMaterialListMaterials()
        {
            return materials;
        }

        protected virtual void SaveAllUnsavedAssets()
        {
            ContextSaveAllUnsavedAssets(new MenuCommand(baseTarget));
        }

        [MenuItem("CONTEXT/VoxelObject/Save All Unsaved Assets")]
        private static void ContextSaveAllUnsavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelObjectCore(objectTarget);

            var folder = EditorUtility.OpenFolderPanel("Save all", objectCore.GetDefaultPath(), null);
            if (string.IsNullOrEmpty(folder)) return;
            if (folder.IndexOf(Application.dataPath) < 0)
            {
                SaveInsideAssetsFolderDisplayDialog();
                return;
            }

            Undo.RecordObject(objectTarget, "Save All Unsaved Assets");

            #region Mesh
            if (objectTarget.mesh != null && !IsMainAsset(objectTarget.mesh))
            {
                var path = folder + "/" + string.Format("{0}_mesh.asset", objectTarget.gameObject.name);
                path = path.Replace(Application.dataPath, "Assets");
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CreateAsset(Mesh.Instantiate(objectTarget.mesh), path);
                objectTarget.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }
            #endregion

            #region Material
            if (objectTarget.materials != null)
            {
                for (int index = 0; index < objectTarget.materials.Count; index++)
                {
                    if (objectTarget.materials[index] == null || IsMainAsset(objectTarget.materials[index])) continue;
                    var path = folder + "/" + string.Format("{0}_mat{1}.mat", objectTarget.gameObject.name, index);
                    path = path.Replace(Application.dataPath, "Assets");
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CreateAsset(Material.Instantiate(objectTarget.materials[index]), path);
                    objectTarget.materials[index] = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            #endregion

            #region Texture
            if (objectTarget.atlasTexture != null && !IsMainAsset(objectTarget.atlasTexture))
            {
                var path = folder + "/" + string.Format("{0}_tex.png", objectTarget.gameObject.name);
                {
                    path = AssetDatabase.GenerateUniqueAssetPath(path.Replace(Application.dataPath, "Assets"));
                    path = (Application.dataPath + path).Replace("AssetsAssets", "Assets");
                }
                File.WriteAllBytes(path, objectTarget.atlasTexture.EncodeToPNG());
                path = path.Replace(Application.dataPath, "Assets");
                AssetDatabase.ImportAsset(path);
                objectCore.SetTextureImporterSetting(path, objectTarget.atlasTexture);
                objectTarget.atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            #endregion

            objectCore.ReCreate();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/VoxelObject/Reset All Assets")]
        private static void ResetAllSavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelObjectCore(objectTarget);

            Undo.RecordObject(objectTarget, "Reset All Assets");

            #region Mesh
            objectTarget.mesh = null;
            #endregion

            #region Material
            if (objectTarget.materials != null)
            {
                for (int i = 0; i < objectTarget.materials.Count; i++)
                {
                    if (objectTarget.materials[i] == null) continue;
                    if (!IsMainAsset(objectTarget.materials[i]))
                        objectTarget.materials[i] = null;
                    else
                        objectTarget.materials[i] = Instantiate<Material>(objectTarget.materials[i]);
                }
            }
            #endregion

            #region Texture
            objectTarget.atlasTexture = null;
            #endregion

            objectCore.ReCreate();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/VoxelObject/Export COLLADA(dae) File", false, 10000)]
        private static void ExportDaeFile(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelObjectCore(objectTarget);

            string path = EditorUtility.SaveFilePanel("Export COLLADA(dae) File", objectCore.GetDefaultPath(), string.Format("{0}.dae", Path.GetFileNameWithoutExtension(objectTarget.voxelFilePath)), "dae");
            if (string.IsNullOrEmpty(path)) return;

            if (!objectCore.ExportDaeFile(path))
            {
                Debug.LogErrorFormat("<color=green>[Voxel Importer]</color> Export COLLADA(dae) File error. file:{0}", path);
            }
        }
        [MenuItem("CONTEXT/VoxelObject/Export COLLADA(dae) File", true)]
        private static bool IsValidateExportDaeFile(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelObject;
            if (objectTarget == null) return false;

#if UNITY_2018_3_OR_NEWER
            return true;
#else
            return PrefabUtility.GetPrefabType(objectTarget) != PrefabType.Prefab;
#endif
        }

        [MenuItem("CONTEXT/VoxelObject/Remove All Voxel Importer Compornent", false, 10100)]
        private static void RemoveAllVoxelImporterCompornent(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelObject;
            if (objectTarget == null) return;

            Undo.DestroyObjectImmediate(objectTarget);
        }
    }
}
