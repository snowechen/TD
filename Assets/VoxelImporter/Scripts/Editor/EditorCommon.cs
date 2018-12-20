using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    public abstract class EditorCommon : Editor
    {
        protected static void SaveInsideAssetsFolderDisplayDialog()
        {
            EditorUtility.DisplayDialog("Need to save in the Assets folder", "You need to save the file inside of the project's assets floder", "ok");
        }
        
        protected static string GetHelpStrings(List<string> helpList)
        {
            if (helpList.Count > 0)
            {
                string text = "";
                if (helpList.Count >= 3)
                {
                    int i = 0;
                    var enu = helpList.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        if (i == helpList.Count - 1)
                            text += ", and ";
                        else if (i != 0)
                            text += ", ";
                        text += enu.Current;
                        i++;
                    }
                }
                else if (helpList.Count == 2)
                {
                    var enu = helpList.GetEnumerator();
                    enu.MoveNext();
                    text += enu.Current;
                    text += " and ";
                    enu.MoveNext();
                    text += enu.Current;
                }
                else if (helpList.Count == 1)
                {
                    var enu = helpList.GetEnumerator();
                    enu.MoveNext();
                    text += enu.Current;
                }
                return string.Format("If it is not Prefab you need to save the file.\nPlease create a Prefab for this GameObject.\nIf you do not want to Prefab, please save {0}.", text);
            }
            return null;
        }

        public static bool IsMainAsset(UnityEngine.Object obj)
        {
            return (obj != null && AssetDatabase.Contains(obj) && AssetDatabase.IsMainAsset(obj));
        }
        public static bool IsSubAsset(UnityEngine.Object obj)
        {
            return (obj != null && AssetDatabase.Contains(obj) && AssetDatabase.IsSubAsset(obj));
        }

        public static Texture2D EditorGUIUtilityLoadIcon(string name)
        {
            var mi = typeof(EditorGUIUtility).GetMethod("LoadIcon", BindingFlags.NonPublic | BindingFlags.Static);
            return (Texture2D)mi.Invoke(null, new object[] { name });
        }
    }
}
