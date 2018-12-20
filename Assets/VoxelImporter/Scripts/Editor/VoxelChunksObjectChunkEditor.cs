using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_2018_3_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

namespace VoxelImporter
{
    [CustomEditor(typeof(VoxelChunksObjectChunk))]
    public class VoxelChunksObjectChunkEditor : EditorCommon
    {
        public VoxelChunksObjectChunk chunkTarget { get; private set; }
        public VoxelChunksObject objectTarget { get; private set; }

        public VoxelChunksObjectChunkCore chunkCore { get; protected set; }
        public VoxelChunksObjectCore objectCore { get; protected set; }

        #region GuiStyle
        private GUIStyle guiStyleMagentaBold;
        private GUIStyle guiStyleRedBold;
        private GUIStyle guiStyleFoldoutBold;
        #endregion

        #region Prefab
#if UNITY_2018_3_OR_NEWER
        protected PrefabAssetType prefabType { get { return PrefabUtility.GetPrefabAssetType(objectTarget.gameObject); } }
        protected bool prefabEnable { get { return prefabType == PrefabAssetType.Regular || isPrefabEditMode; } }
        protected bool isPrefab { get { return false; } }
        protected bool isPrefabEditMode { get { return PrefabStageUtility.GetCurrentPrefabStage() != null && PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot != null; } }
#else
        protected PrefabType prefabType { get { return PrefabUtility.GetPrefabType(objectTarget.gameObject); } }
        protected bool prefabEnable { get { var type = prefabType; return type == PrefabType.Prefab || type == PrefabType.PrefabInstance || type == PrefabType.DisconnectedPrefabInstance; } }
        protected bool isPrefab { get { return prefabType == PrefabType.Prefab; } }
#endif
        #endregion

        void OnEnable()
        {
            chunkTarget = target as VoxelChunksObjectChunk;
            if (chunkTarget == null) return;
            chunkCore = new VoxelChunksObjectChunkCore(chunkTarget);
            if (chunkTarget.transform.parent == null) return;
            objectTarget = chunkTarget.transform.parent.GetComponent<VoxelChunksObject>();
            if (objectTarget == null) return;
            objectCore = new VoxelChunksObjectCore(objectTarget);

            chunkCore.Initialize();
        }

        public override void OnInspectorGUI()
        {
            if (chunkTarget == null || objectTarget == null)
            {
                DrawDefaultInspector();
                return;
            }

#if UNITY_2018_3_OR_NEWER
            {
                if (prefabType == PrefabAssetType.Regular && !isPrefabEditMode)
                {
                    EditorGUILayout.HelpBox("Prefab can only be edited in Prefab mode.", MessageType.Info);
                    EditorGUI.BeginDisabledGroup(true);
                }
            }
#endif

            #region GuiStyle
            if (guiStyleMagentaBold == null)
                guiStyleMagentaBold = new GUIStyle(EditorStyles.boldLabel);
            guiStyleMagentaBold.normal.textColor = Color.magenta;
            if (guiStyleRedBold == null)
                guiStyleRedBold = new GUIStyle(EditorStyles.boldLabel);
            guiStyleRedBold.normal.textColor = Color.red;
            if (guiStyleFoldoutBold == null)
                guiStyleFoldoutBold = new GUIStyle(EditorStyles.foldout);
            guiStyleFoldoutBold.fontStyle = FontStyle.Bold;
            #endregion

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(isPrefab);

            InspectorGUI();

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

#if UNITY_2018_3_OR_NEWER
            {
                if (prefabType == PrefabAssetType.Regular && !isPrefabEditMode)
                {
                    EditorGUI.EndDisabledGroup();
                }
            }
#endif
        }

        protected void InspectorGUI()
        {
            #region Simple
            {
                EditorGUI.BeginChangeCheck();
                var mode = GUILayout.Toolbar(objectTarget.advancedMode ? 1 : 0, VoxelBaseEditor.Edit_AdvancedModeStrings);
                if (EditorGUI.EndChangeCheck())
                {
                    objectTarget.advancedMode = mode != 0 ? true : false;
                }
            }
            #endregion

            EditorGUILayout.Space();

            Action<UnityEngine.Object, string> TypeTitle = (o, title) =>
            {
                if (o == null)
                    EditorGUILayout.LabelField(title, guiStyleMagentaBold);
                else if (prefabEnable && !AssetDatabase.Contains(o))
                    EditorGUILayout.LabelField(title, guiStyleRedBold);
                else
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            };

            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Reset Transform"))
                {
                    Undo.RecordObject(chunkTarget.transform, "Reset Transform");
                    chunkTarget.transform.localPosition = chunkTarget.basicOffset;
                    chunkTarget.transform.localRotation = Quaternion.identity;
                    chunkTarget.transform.localScale = Vector3.one;
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
            }

            #region Object
            if (objectTarget.advancedMode)
            {
                chunkTarget.edit_objectFoldout = EditorGUILayout.Foldout(chunkTarget.edit_objectFoldout, "Object", guiStyleFoldoutBold);
                if (chunkTarget.edit_objectFoldout)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    #region Mesh
                    {
                        TypeTitle(chunkTarget.mesh, "Mesh");
                        EditorGUI.indentLevel++;
                        #region Mesh
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.ObjectField(chunkTarget.mesh, typeof(Mesh), false);
                                EditorGUI.EndDisabledGroup();
                            }
                            if (chunkTarget.mesh != null)
                            {
                                if (!IsMainAsset(chunkTarget.mesh))
                                {
                                    if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                                    {
                                        #region Create Mesh
                                        string path = EditorUtility.SaveFilePanel("Save mesh", chunkCore.GetDefaultPath(), string.Format("{0}_{1}_mesh.asset", objectTarget.gameObject.name, chunkTarget.chunkName), "asset");
                                        if (!string.IsNullOrEmpty(path))
                                        {
                                            if (path.IndexOf(Application.dataPath) < 0)
                                            {
                                                SaveInsideAssetsFolderDisplayDialog();
                                            }
                                            else
                                            {
                                                Undo.RecordObject(objectTarget, "Save Mesh");
                                                Undo.RecordObject(chunkTarget, "Save Mesh");
                                                path = path.Replace(Application.dataPath, "Assets");
                                                AssetDatabase.CreateAsset(Mesh.Instantiate(chunkTarget.mesh), path);
                                                chunkTarget.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
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
                                        Undo.RecordObject(objectTarget, "Reset Mesh");
                                        Undo.RecordObject(chunkTarget, "Reset Mesh");
                                        chunkTarget.mesh = null;
                                        Refresh();
                                        #endregion
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion
                        #region Vertex Count
                        {
                            EditorGUILayout.LabelField("Vertex Count", chunkTarget.mesh != null ? chunkTarget.mesh.vertexCount.ToString() : "");
                        }
                        #endregion
                        EditorGUI.indentLevel--;
                    }
                    #endregion
                    #region Material
                    if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Individual)
                    {
                        {
                            if (chunkTarget.materials == null || chunkTarget.materials.Count == 0)
                                EditorGUILayout.LabelField("Material", guiStyleMagentaBold);
                            else if (prefabEnable)
                            {
                                bool contains = true;
                                for (int i = 0; i < chunkTarget.materials.Count; i++)
                                {
                                    if (!AssetDatabase.Contains(chunkTarget.materials[i]))
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
                        EditorGUI.indentLevel++;
                        #region Material
                        for (int i = 0; i < chunkTarget.materials.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.ObjectField(chunkTarget.materials[i], typeof(Material), false);
                                EditorGUI.EndDisabledGroup();
                            }
                            if (chunkTarget.materials[i] != null)
                            {
                                if (!IsMainAsset(chunkTarget.materials[i]))
                                {
                                    if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                                    {
                                        #region Create Material
                                        string defaultName = string.Format("{0}_{1}_mat{2}.mat", objectTarget.gameObject.name, chunkTarget.chunkName, i);
                                        string path = EditorUtility.SaveFilePanel("Save material", chunkCore.GetDefaultPath(), defaultName, "mat");
                                        if (!string.IsNullOrEmpty(path))
                                        {
                                            if (path.IndexOf(Application.dataPath) < 0)
                                            {
                                                SaveInsideAssetsFolderDisplayDialog();
                                            }
                                            else
                                            {
                                                Undo.RecordObject(objectTarget, "Save Material");
                                                Undo.RecordObject(chunkTarget, "Save Material");
                                                path = path.Replace(Application.dataPath, "Assets");
                                                AssetDatabase.CreateAsset(Material.Instantiate(chunkTarget.materials[i]), path);
                                                chunkTarget.materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
                                                Refresh();
                                            }
                                        }

                                        #endregion
                                    }
                                }
                                {
                                    if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                                    {
                                        #region Reset Material
                                        Undo.RecordObject(objectTarget, "Reset Material");
                                        Undo.RecordObject(chunkTarget, "Reset Material");
                                        if (!IsMainAsset(chunkTarget.materials[i]))
                                            chunkTarget.materials[i] = null;
                                        else
                                            chunkTarget.materials[i] = Instantiate<Material>(chunkTarget.materials[i]);
                                        Refresh();
                                        #endregion
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion
                        EditorGUI.indentLevel--;
                    }
                    #endregion
                    #region Texture
                    if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Individual)
                    {
                        TypeTitle(chunkTarget.atlasTexture, "Texture");
                        EditorGUI.indentLevel++;
                        #region Texture
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.ObjectField(chunkTarget.atlasTexture, typeof(Texture2D), false);
                                EditorGUI.EndDisabledGroup();
                            }
                            if (chunkTarget.atlasTexture != null)
                            {
                                if (!IsMainAsset(chunkTarget.atlasTexture))
                                {
                                    if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                                    {
                                        #region Create Texture
                                        string defaultName = string.Format("{0}_{1}_tex.png", objectTarget.gameObject.name, chunkTarget.chunkName);
                                        string path = EditorUtility.SaveFilePanel("Save atlas texture", chunkCore.GetDefaultPath(), defaultName, "png");
                                        if (!string.IsNullOrEmpty(path))
                                        {
                                            if (path.IndexOf(Application.dataPath) < 0)
                                            {
                                                SaveInsideAssetsFolderDisplayDialog();
                                            }
                                            else
                                            {
                                                Undo.RecordObject(objectTarget, "Save Atlas Texture");
                                                Undo.RecordObject(chunkTarget, "Save Atlas Texture");
                                                File.WriteAllBytes(path, chunkTarget.atlasTexture.EncodeToPNG());
                                                path = path.Replace(Application.dataPath, "Assets");
                                                AssetDatabase.ImportAsset(path);
                                                objectCore.SetTextureImporterSetting(path, chunkTarget.atlasTexture);
                                                chunkTarget.atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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
                                        Undo.RecordObject(objectTarget, "Reset Atlas Texture");
                                        Undo.RecordObject(chunkTarget, "Reset Atlas Texture");
                                        chunkTarget.atlasTexture = null;
                                        Refresh();
                                        #endregion
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion
                        #region Texture Size
                        {
                            EditorGUILayout.LabelField("Texture Size", chunkTarget.atlasTexture != null ? string.Format("{0} x {1}", chunkTarget.atlasTexture.width, chunkTarget.atlasTexture.height) : "");
                        }
                        #endregion
                        EditorGUI.indentLevel--;
                    }
                    #endregion
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUILayout.Space();
            }
            #endregion

            #region Refresh
            if (GUILayout.Button("Refresh"))
            {
                Undo.RecordObject(objectTarget, "Inspector");
                Undo.RecordObject(chunkTarget, "Inspector");
                Refresh();
            }
            #endregion
        }

        protected void Refresh()
        {
            objectCore.ReCreate();
        }
    }
}
