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
    [CustomEditor(typeof(VoxelObjectExplosion))]
    public class VoxelObjectExplosionEditor : VoxelBaseExplosionEditor
    {
        public VoxelObjectExplosion explosionObject { get; protected set; }
        public VoxelObjectExplosionCore explosionObjectCore { get; protected set; }

        protected override void OnEnable()
        {
            base.OnEnable();

            explosionBase = explosionObject = target as VoxelObjectExplosion;
            if (explosionObject == null) return;
            explosionCore = explosionObjectCore = new VoxelObjectExplosionCore(explosionObject);
            if (explosionCore.voxelBase == null)
            {
                Undo.DestroyObjectImmediate(explosionBase);
                return;
            }
            OnEnableInitializeSet();
        }

        protected override void Inspector_MeshMaterial()
        {
            #region Mesh
            {
                if (explosionObject.meshes != null)
                {
                    if (prefabEnable)
                    {
                        bool contains = true;
                        for (int i = 0; i < explosionObject.meshes.Count; i++)
                        {
                            if (explosionObject.meshes[i] == null || explosionObject.meshes[i].mesh == null || !AssetDatabase.Contains(explosionObject.meshes[i].mesh))
                                contains = false;
                        }
                        EditorGUILayout.LabelField("Mesh", contains ? EditorStyles.boldLabel : guiStyleRedBold);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Mesh", guiStyleMagentaBold);
                }
                {
                    EditorGUI.indentLevel++;
                    {
                        if (explosionObject.meshes != null)
                        {
                            for (int i = 0; i < explosionObject.meshes.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.ObjectField(explosionObject.meshes[i].mesh, typeof(Mesh), false);
                                    EditorGUI.EndDisabledGroup();
                                }
                                if (explosionObject.meshes[i] != null && explosionObject.meshes[i].mesh != null)
                                {
                                    if (!IsMainAsset(explosionObject.meshes[i].mesh))
                                    {
                                        if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                                        {
                                            #region Create Mesh
                                            string path = EditorUtility.SaveFilePanel("Save mesh", explosionCore.voxelBaseCore.GetDefaultPath(), string.Format("{0}_explosion_mesh{1}.asset", explosionObject.gameObject.name, i), "asset");
                                            if (!string.IsNullOrEmpty(path))
                                            {
                                                if (path.IndexOf(Application.dataPath) < 0)
                                                {
                                                    SaveInsideAssetsFolderDisplayDialog();
                                                }
                                                else
                                                {
                                                    Undo.RecordObject(explosionObject, "Save Mesh");
                                                    path = path.Replace(Application.dataPath, "Assets");
                                                    AssetDatabase.CreateAsset(Mesh.Instantiate(explosionObject.meshes[i].mesh), path);
                                                    explosionObject.meshes[i].mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                                                    explosionCore.Generate();
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    {
                                        if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                                        {
                                            #region Reset Mesh
                                            Undo.RecordObject(explosionObject, "Reset Mesh");
                                            explosionObject.meshes[i].mesh = null;
                                            explosionCore.Generate();
                                            #endregion
                                        }
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            #endregion

            #region Material
            {
                if (explosionObject.materials != null)
                {
                    if (prefabEnable)
                    {
                        bool contains = true;
                        for (int i = 0; i < explosionObject.materials.Count; i++)
                        {
                            if (explosionObject.materials[i] == null || !AssetDatabase.Contains(explosionObject.materials[i]))
                                contains = false;
                        }
                        EditorGUILayout.LabelField("Material", contains ? EditorStyles.boldLabel : guiStyleRedBold);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Material", guiStyleMagentaBold);
                }
                {
                    EditorGUI.indentLevel++;
                    {
                        if (explosionObject.materials != null)
                        {
                            for (int i = 0; i < explosionObject.materials.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.ObjectField(explosionObject.materials[i], typeof(Material), false);
                                    EditorGUI.EndDisabledGroup();
                                }
                                if (explosionObject.materials[i] != null)
                                {
                                    if (!IsMainAsset(explosionObject.materials[i]))
                                    {
                                        if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                                        {
                                            #region Create Material
                                            string path = EditorUtility.SaveFilePanel("Save material", explosionCore.voxelBaseCore.GetDefaultPath(), string.Format("{0}_explosion_mat{1}.mat", explosionObject.gameObject.name, i), "mat");
                                            if (!string.IsNullOrEmpty(path))
                                            {
                                                if (path.IndexOf(Application.dataPath) < 0)
                                                {
                                                    SaveInsideAssetsFolderDisplayDialog();
                                                }
                                                else
                                                {
                                                    Undo.RecordObject(explosionObject, "Save Material");
                                                    path = path.Replace(Application.dataPath, "Assets");
                                                    AssetDatabase.CreateAsset(Material.Instantiate(explosionObject.materials[i]), path);
                                                    explosionObject.materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
                                                    explosionCore.Generate();
                                                }
                                            }

                                            #endregion
                                        }
                                    }
                                    {
                                        if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                                        {
                                            #region Reset Material
                                            Undo.RecordObject(explosionObject, "Reset Material");
                                            if (!IsMainAsset(explosionObject.materials[i]))
                                                explosionObject.materials[i] = null;
                                            else
                                                explosionObject.materials[i] = Instantiate<Material>(explosionObject.materials[i]);
                                            explosionCore.Generate();
                                            #endregion
                                        }
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            #endregion
        }
        
        [MenuItem("CONTEXT/VoxelObjectExplosion/Save All Unsaved Assets")]
        private static void ContextSaveAllUnsavedAssets(MenuCommand menuCommand)
        {
            var explosionObject = menuCommand.context as VoxelObjectExplosion;
            if (explosionObject == null) return;

            var explosionCore = new VoxelObjectExplosionCore(explosionObject);

            var folder = EditorUtility.OpenFolderPanel("Save all", explosionCore.voxelBaseCore.GetDefaultPath(), null);
            if (string.IsNullOrEmpty(folder)) return;
            if (folder.IndexOf(Application.dataPath) < 0)
            {
                SaveInsideAssetsFolderDisplayDialog();
                return;
            }

            Undo.RecordObject(explosionObject, "Save All Unsaved Assets");

            #region Mesh
            if (explosionObject.meshes != null)
            {
                for (int i = 0; i < explosionObject.meshes.Count; i++)
                {
                    if (explosionObject.meshes[i] != null && explosionObject.meshes[i].mesh != null && !IsMainAsset(explosionObject.meshes[i].mesh))
                    {
                        var path = folder + "/" + string.Format("{0}_explosion_mesh{1}.asset", explosionObject.gameObject.name, i);
                        path = path.Replace(Application.dataPath, "Assets");
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                        AssetDatabase.CreateAsset(Mesh.Instantiate(explosionObject.meshes[i].mesh), path);
                        explosionObject.meshes[i].mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    }
                }
            }
            #endregion

            #region Material
            if (explosionObject.materials != null)
            {
                for (int index = 0; index < explosionObject.materials.Count; index++)
                {
                    if (explosionObject.materials[index] == null) continue;
                    if (IsMainAsset(explosionObject.materials[index])) continue;
                    var path = folder + "/" + string.Format("{0}_explosion_mat{1}.mat", explosionObject.gameObject.name, index);
                    path = path.Replace(Application.dataPath, "Assets");
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CreateAsset(Material.Instantiate(explosionObject.materials[index]), path);
                    explosionObject.materials[index] = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            #endregion

            explosionCore.Generate();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/VoxelObjectExplosion/Reset All Assets")]
        private static void ResetAllSavedAssets(MenuCommand menuCommand)
        {
            var explosionObject = menuCommand.context as VoxelObjectExplosion;
            if (explosionObject == null) return;

            var explosionCore = new VoxelObjectExplosionCore(explosionObject);

            Undo.RecordObject(explosionObject, "Reset All Assets");

            #region Mesh
            explosionObject.meshes = null;
            #endregion

            #region Material
            if (explosionObject.materials != null)
            {
                for (int i = 0; i < explosionObject.materials.Count; i++)
                {
                    if (explosionObject.materials[i] == null) continue;
                    if (!IsMainAsset(explosionObject.materials[i]))
                        explosionObject.materials[i] = null;
                    else
                        explosionObject.materials[i] = Instantiate<Material>(explosionObject.materials[i]);
                }
            }
            #endregion

            explosionCore.Generate();
            InternalEditorUtility.RepaintAllViews();
        }
    }
}

