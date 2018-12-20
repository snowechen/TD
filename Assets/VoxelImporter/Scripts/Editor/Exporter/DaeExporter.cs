using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Linq;
using System.Collections.Generic;
using VoxelImporter.grendgine_collada;

namespace VoxelImporter
{
    public class DaeExporter
    {
        private string ContributorAuthoring_Tool = "Voxel Importer";
        //private string ContributorComments = "https://www.assetstore.unity3d.com/#!/content/96826";     //VA
        private string ContributorComments = "https://www.assetstore.unity3d.com/#!/content/62914";   //VI

        public bool Export(string path, List<Transform> transforms, AnimationClip[] clips = null)
        {
            int progressTotal = 1 + (clips != null ? clips.Length : 0);
            int progressIndex = 0;
            EditorUtility.DisplayProgressBar("Exporting Collada(dae) File...", Path.GetFileName(path), (progressIndex++ / (float)progressTotal));

            #region TransformSave
            TransformSave[] transformSaves = new TransformSave[transforms.Count];
            for (int i = 0; i < transforms.Count; i++)
            {
                transformSaves[i] = new TransformSave(transforms[i]);
            }
            #endregion
            #region FixTransform
            foreach (var t in transforms)
            {
                #region Do not allow scale zero
                {
                    bool update = false;
                    var scale = t.localScale;
                    for (int si = 0; si < 3; si++)
                    {
                        if (scale[si] == 0f)
                        {
                            scale[si] = Mathf.Epsilon;
                            update = true;
                        }
                    }
                    if (update)
                        t.localScale = scale;
                }
                #endregion
            }
            #endregion

            try
            {
                exportedFiles.Clear();

                var rootObject = transforms[0].gameObject;

                Grendgine_Collada gCollada = new Grendgine_Collada();

                Func<UnityEngine.Object, string> MakeID = (o) =>
                {
                    var id = o.GetInstanceID().ToString();
                    return id.Replace('-', 'n');
                };
                Func<Transform, Mesh> MeshFromTransform = (t) =>
                {
                    #region SkinnedMeshRenderer
                    {
                        var skinnedMeshRenderer = t.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedMeshRenderer != null && skinnedMeshRenderer.enabled && skinnedMeshRenderer.sharedMesh != null && skinnedMeshRenderer.sharedMaterials != null)
                            return skinnedMeshRenderer.sharedMesh;
                    }
                    #endregion
                    #region MeshFilter
                    {
                        var meshFilter = t.GetComponent<MeshFilter>();
                        var meshRenderer = t.GetComponent<MeshRenderer>();
                        if (meshFilter != null && meshFilter.sharedMesh != null && meshRenderer != null && meshRenderer.enabled && meshRenderer.sharedMaterials != null)
                            return meshFilter.sharedMesh;
                    }
                    #endregion
                    return null;
                };
                Func<Transform, Material[]> MaterialsFromTransform = (t) =>
                {
                    #region SkinnedMeshRenderer
                    {
                        var skinnedMeshRenderer = t.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedMeshRenderer != null && skinnedMeshRenderer.enabled && skinnedMeshRenderer.sharedMesh != null && skinnedMeshRenderer.sharedMaterials != null)
                            return skinnedMeshRenderer.sharedMaterials;
                    }
                    #endregion
                    #region MeshFilter
                    {
                        var meshFilter = t.GetComponent<MeshFilter>();
                        var meshRenderer = t.GetComponent<MeshRenderer>();
                        if (meshFilter != null && meshFilter.sharedMesh != null && meshRenderer != null && meshRenderer.enabled && meshRenderer.sharedMaterials != null)
                            return meshRenderer.sharedMaterials;
                    }
                    #endregion
                    return null;
                };
                Action<Action<Transform, Mesh, Material[]>> MakeFromTransform = (action) =>
                {
                    foreach (var t in transforms)
                    {
                        if (!t.gameObject.activeInHierarchy) continue;
                        #region SkinnedMeshRenderer
                        {
                            var skinnedMeshRenderer = t.GetComponent<SkinnedMeshRenderer>();
                            if (skinnedMeshRenderer != null && skinnedMeshRenderer.enabled && skinnedMeshRenderer.sharedMesh != null && skinnedMeshRenderer.sharedMaterials != null)
                            {
                                action(t, skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
                                continue;
                            }
                        }
                        #endregion
                        #region MeshFilter
                        {
                            var meshFilter = t.GetComponent<MeshFilter>();
                            var meshRenderer = t.GetComponent<MeshRenderer>();
                            if (meshFilter != null && meshFilter.sharedMesh != null && meshRenderer != null && meshRenderer.enabled && meshRenderer.sharedMaterials != null)
                            {
                                action(t, meshFilter.sharedMesh, meshRenderer.sharedMaterials);
                                continue;
                            }
                        }
                        #endregion
                    }
                };

                Matrix4x4 matMirrorX = Matrix4x4.identity;
                matMirrorX.m00 = -matMirrorX.m00;

                var MatrixIdentity = new Grendgine_Collada_Matrix()
                {
                    sID = "transform",
                };
                {
                    var mat = Matrix4x4.identity;
                    var sb = new StringBuilder();
                    for (int r = 0; r < 4; r++)
                        for (int c = 0; c < 4; c++)
                            sb.AppendFormat("{0} ", mat[r, c]);
                    sb.Remove(sb.Length - 1, 1);
                    MatrixIdentity.Value_As_String = sb.ToString();
                }

                bool makeJoint = rootObject.GetComponentInChildren<SkinnedMeshRenderer>() != null;

                #region Header
                {
                    gCollada.Collada_Version = "1.4.1";     //for Blender

                    gCollada.Asset = new Grendgine_Collada_Asset()
                    {
                        Created = DateTime.Now,
                        Modified = DateTime.Now,
                        Contributor = new Grendgine_Collada_Asset_Contributor[]
                        {
                            new Grendgine_Collada_Asset_Contributor()
                            {
                                Authoring_Tool = ContributorAuthoring_Tool,
                                Comments = ContributorComments,
                            },
                        },
                        Revision = "1.0",
                        Title = Path.GetFileNameWithoutExtension(path),
                        Unit = new Grendgine_Collada_Asset_Unit()
                        {
                            Name = "meter",
                            Meter = 1.0f,
                        },
                    };
                }
                #endregion

                #region Images
                var imagesDic = new Dictionary<Texture, Grendgine_Collada_Image>();
                if (settings_exportMesh)
                {
                    var li = gCollada.Library_Images = new Grendgine_Collada_Library_Images()
                    {
                        ID = "Images_" + MakeID(rootObject),
                        Name = "Images_" + rootObject.name,
                    };

                    bool singleImage;
                    {
                        var texList = new HashSet<Texture>();
                        MakeFromTransform((t, mesh, materials) =>
                        {
                            foreach (var material in materials)
                            {
                                if (material.mainTexture != null)
                                    texList.Add(material.mainTexture);
                            }
                        });
                        singleImage = texList.Count <= 1;
                    }

                    Func<Texture, string> ExportTexture = (tex) =>
                    {
                        string texpath;
                        if (AssetDatabase.Contains(tex) && AssetDatabase.IsMainAsset(tex))
                        {
                            if (path.StartsWith(Application.dataPath))
                            {
                                var assetPath = AssetDatabase.GetAssetPath(tex);
                                texpath = Path.GetDirectoryName(path).Replace(Application.dataPath, "Assets") + "/" + Path.GetFileName(assetPath);
                                if (AssetDatabase.LoadAssetAtPath<Texture2D>(texpath) == null)
                                    AssetDatabase.CopyAsset(assetPath, texpath);
                                else
                                {
                                    assetPath = Application.dataPath + AssetDatabase.GetAssetPath(tex).Remove(0, "Assets".Length);
                                    texpath = Path.GetDirectoryName(path) + "/" + Path.GetFileName(assetPath);
                                    if (assetPath != texpath)
                                        File.Copy(assetPath, texpath, true);
                                }
                            }
                            else
                            {
                                var assetPath = Application.dataPath + AssetDatabase.GetAssetPath(tex).Remove(0, "Assets".Length);
                                texpath = Path.GetDirectoryName(path) + "/" + Path.GetFileName(assetPath);
                                if (assetPath != texpath)
                                    File.Copy(assetPath, texpath, true);
                            }
                        }
                        else
                        {
                            const string EXT = ".png";
                            if (singleImage)
                                texpath = path.Remove(path.Length - EXT.Length, EXT.Length) + EXT;
                            else
                                texpath = string.Format("{0}/{1}_tex{2}{3}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), imagesDic.Count, EXT);

                            Texture2D tex2D = null;
                            bool created = false;
                            if(tex is Texture2D)
                            {
                                tex2D = tex as Texture2D;
                            }
                            else
                            {
                                var currentRT = RenderTexture.active;
                                RenderTexture rt = null;
                                try
                                {
                                    rt = new RenderTexture(tex.width, tex.height, 32);
                                    Graphics.Blit(tex, rt);
                                    RenderTexture.active = rt;
                                    tex2D = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                                    tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                                    tex2D.Apply();
                                    created = true;
                                }
                                finally
                                {
                                    RenderTexture.active = currentRT;
                                    if (rt != null)
                                    {
                                        rt.Release();
                                        RenderTexture.DestroyImmediate(rt);
                                    }
                                }
                            }
                            if (tex2D != null)
                            {
                                try
                                {
                                    File.WriteAllBytes(texpath, tex2D.EncodeToPNG());
                                }
                                catch
                                {
                                    Debug.LogWarningFormat("<color=green>[{0}]</color> Texture Export Error. '{1}'", ContributorAuthoring_Tool, tex.name);
                                }
                            }
                            else
                            {
                                Debug.LogWarningFormat("<color=green>[{0}]</color> Texture Export Error. '{1}'", ContributorAuthoring_Tool, tex.name);
                            }
                            if (created && tex2D)
                            {
                                Texture2D.DestroyImmediate(tex2D);
                            }
                        }
                        exportedFiles.Add(texpath);
                        return Path.GetFileName(texpath);
                    };

                    MakeFromTransform((t, mesh, materials) =>
                    {
                        foreach (var material in materials)
                        {
                            var tex = material.mainTexture;
                            if (tex == null || imagesDic.ContainsKey(tex)) continue;
                            var image = new Grendgine_Collada_Image()
                            {
                                ID = "Image_" + MakeID(tex),
                                Name = tex.name,
                                Init_From = Uri.EscapeDataString(ExportTexture(tex)),
                            };
                            imagesDic.Add(tex, image);
                        }
                    });
                    li.Image = imagesDic.Values.ToArray();
                }
                #endregion

                #region Effects
                var effectsDic = new Dictionary<Material, Grendgine_Collada_Effect>();
                if (settings_exportMesh)
                {
                    var le = gCollada.Library_Effects = new Grendgine_Collada_Library_Effects()
                    {
                        ID = "Effects_" + MakeID(rootObject),
                        Name = "Effects_" + rootObject.name,
                    };
                    MakeFromTransform((t, mesh, materials) =>
                    {
                        foreach (var material in materials)
                        {
                            var tex = material.mainTexture;
                            if (effectsDic.ContainsKey(material)) continue;
                            Grendgine_Collada_New_Param[] New_Param = null;
                            Grendgine_Collada_FX_Common_Color_Or_Texture_Type Diffuse = null;
                            if (tex != null && imagesDic.ContainsKey(tex))
                            {
                                Grendgine_Collada_New_Param surfaceParam = new Grendgine_Collada_New_Param()
                                {
                                    sID = "Surface_" + MakeID(material),
                                    Surface = new Grendgine_Collada_Surface_1_4_1()
                                    {
                                        Type = Grendgine_Collada_FX_Surface_Type._2D,
                                        Init_From = imagesDic[tex].ID,
                                    },
                                };
                                Grendgine_Collada_New_Param sampler2DParam = new Grendgine_Collada_New_Param()
                                {
                                    sID = "Sampler2D_" + MakeID(material),
                                    Sampler2D = new Grendgine_Collada_Sampler2D()
                                    {
                                        Source = surfaceParam.sID,
                                    },
                                };
                                New_Param = new Grendgine_Collada_New_Param[]
                                {
                                    surfaceParam,
                                    sampler2DParam,
                                };
                                Diffuse = new Grendgine_Collada_FX_Common_Color_Or_Texture_Type()
                                {
                                    Texture = new Grendgine_Collada_Texture()
                                    {
                                        TexCoord = "TexCoord_" + MakeID(material.mainTexture),
                                        Texture = sampler2DParam.sID,
                                    },
                                };
                            }
                            else
                            {
                                Color color = Color.white;
                                if (material.HasProperty("_Color"))
                                    color = material.color;
                                Diffuse = new Grendgine_Collada_FX_Common_Color_Or_Texture_Type()
                                {
                                    Color = new Grendgine_Collada_Color()
                                    {
                                        sID = "diffuse",
                                        Value_As_String = string.Format("{0} {1} {2} {3}", color.r, color.g, color.b, color.a),
                                    },
                                };
                            }

                            var e = new Grendgine_Collada_Effect()
                            {
                                ID = "Effect_" + MakeID(material),
                                Name = material.name,
                                Profile_COMMON = new Grendgine_Collada_Profile_COMMON[]
                                {
                                    new Grendgine_Collada_Profile_COMMON()
                                    {
                                        ID = "Profile_" + MakeID(material),
                                        New_Param = New_Param,
                                        Technique = new Grendgine_Collada_Effect_Technique_COMMON()
                                        {
                                            sID = "Technique_" + MakeID(material),
                                            Phong = new Grendgine_Collada_Phong()
                                            {
                                                Emission = new Grendgine_Collada_FX_Common_Color_Or_Texture_Type()
                                                {
                                                    Color = new Grendgine_Collada_Color()
                                                    {
                                                        sID = "emission",
                                                        Value_As_String = "0 0 0 1",
                                                    },
                                                },
                                                Ambient = new Grendgine_Collada_FX_Common_Color_Or_Texture_Type()
                                                {
                                                    Color = new Grendgine_Collada_Color()
                                                    {
                                                        sID = "ambient",
                                                        Value_As_String = "1 1 1 1",
                                                    },
                                                },
                                                Diffuse = Diffuse,
                                                Specular = new Grendgine_Collada_FX_Common_Color_Or_Texture_Type()
                                                {
                                                    Color = new Grendgine_Collada_Color()
                                                    {
                                                        sID = "specular",
                                                        Value_As_String = "0 0 0 1",
                                                    },
                                                },
                                                Shininess = new Grendgine_Collada_FX_Common_Float_Or_Param_Type()
                                                {
                                                    Float = new Grendgine_Collada_SID_Float()
                                                    {
                                                        sID = "shininess",
                                                        Value = 50,
                                                    },
                                                },
                                                Index_Of_Refraction = new Grendgine_Collada_FX_Common_Float_Or_Param_Type()
                                                {
                                                    Float = new Grendgine_Collada_SID_Float()
                                                    {
                                                        sID = "index_of_refraction",
                                                        Value = 1,
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            };
                            effectsDic.Add(material, e);
                        }
                    });
                    le.Effect = effectsDic.Values.ToArray();
                }
                #endregion

                #region Materials
                var materialsDic = new Dictionary<Material, Grendgine_Collada_Material>();
                if (settings_exportMesh)
                {
                    var lm = gCollada.Library_Materials = new Grendgine_Collada_Library_Materials()
                    {
                        ID = "Materials_" + MakeID(rootObject),
                        Name = "Materials_" + rootObject.name,
                    };
                    MakeFromTransform((t, mesh, materials) =>
                    {
                        foreach (var material in materials)
                        {
                            if (!effectsDic.ContainsKey(material)) continue;
                            if (materialsDic.ContainsKey(material)) continue;
                            var effect = effectsDic[material];
                            var m = new Grendgine_Collada_Material()
                            {
                                ID = "Material_" + MakeID(material),
                                Name = material.name,
                                Instance_Effect = new Grendgine_Collada_Instance_Effect()
                                {
                                    URL = "#" + effect.ID,
                                },
                            };
                            materialsDic.Add(material, m);
                        }
                    });
                    lm.Material = materialsDic.Values.ToArray();
                }
                #endregion

                #region Geometries
                var geometriesDic = new Dictionary<Transform, Grendgine_Collada_Geometry>();
                if (settings_exportMesh)
                {
                    var lg = gCollada.Library_Geometries = new Grendgine_Collada_Library_Geometries()
                    {
                        ID = "Geometries_" + MakeID(rootObject),
                        Name = "Geometries_" + rootObject.name,
                    };
                    MakeFromTransform((t, mesh, materials) =>
                    {
                        #region Source
                        #region Vertex
                        Grendgine_Collada_Source vertexSource;
                        {
                            Grendgine_Collada_Float_Array array;
                            {
                                var sb = new StringBuilder();
                                foreach (var v in mesh.vertices)
                                {
                                    var mv = matMirrorX.MultiplyPoint(v);
                                    sb.AppendFormat("\n{0} {1} {2}", mv.x, mv.y, mv.z);
                                }
                                array = new Grendgine_Collada_Float_Array()
                                {
                                    ID = "VertexArray_" + MakeID(mesh),
                                    Name = mesh.name + "_vertex",
                                    Count = mesh.vertexCount * 3,
                                    Value_As_String = sb.ToString(),
                                };
                            }
                            vertexSource = new Grendgine_Collada_Source()
                            {
                                ID = "VertexSource_" + MakeID(mesh),
                                Name = mesh.name + "_vertex",
                                Float_Array = array,
                                Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                {
                                    Accessor = new Grendgine_Collada_Accessor()
                                    {
                                        Count = (uint)mesh.vertexCount,
                                        Source = "#" + array.ID,
                                        Stride = 3,
                                        Param = new Grendgine_Collada_Param[]
                                        {
                                            new Grendgine_Collada_Param() { Name = "X", Type = "float", },
                                            new Grendgine_Collada_Param() { Name = "Y", Type = "float", },
                                            new Grendgine_Collada_Param() { Name = "Z", Type = "float", },
                                        },
                                    },
                                },
                            };
                        }
                        #endregion
                        #region UV
                        Grendgine_Collada_Source uvSource;
                        {
                            Grendgine_Collada_Float_Array array;
                            {
                                var sb = new StringBuilder();
                                foreach (var uv in mesh.uv)
                                    sb.AppendFormat("\n{0} {1}", uv.x, uv.y);
                                array = new Grendgine_Collada_Float_Array()
                                {
                                    ID = "UVArray_" + MakeID(mesh),
                                    Name = mesh.name + "_uv",
                                    Count = mesh.vertexCount * 2,
                                    Value_As_String = sb.ToString(),
                                };
                            }
                            uvSource = new Grendgine_Collada_Source()
                            {
                                ID = "UVSource_" + MakeID(mesh),
                                Name = mesh.name + "_uv",
                                Float_Array = array,
                                Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                {
                                    Accessor = new Grendgine_Collada_Accessor()
                                    {
                                        Count = (uint)mesh.vertexCount,
                                        Source = "#" + array.ID,
                                        Stride = 2,
                                        Param = new Grendgine_Collada_Param[]
                                        {
                                            new Grendgine_Collada_Param() { Name = "S", Type = "float", },
                                            new Grendgine_Collada_Param() { Name = "T", Type = "float", },
                                        },
                                    },
                                },
                            };
                        }
                        #endregion
                        #region Normal
                        Grendgine_Collada_Source normalSource;
                        {
                            Grendgine_Collada_Float_Array array;
                            {
                                var sb = new StringBuilder();
                                foreach (var n in mesh.normals)
                                {
                                    var mn = matMirrorX.MultiplyVector(n);
                                    sb.AppendFormat("\n{0} {1} {2}", mn.x, mn.y, mn.z);
                                }
                                array = new Grendgine_Collada_Float_Array()
                                {
                                    ID = "NormalArray_" + MakeID(mesh),
                                    Name = mesh.name + "_normal",
                                    Count = mesh.vertexCount * 3,
                                    Value_As_String = sb.ToString(),
                                };
                            }
                            normalSource = new Grendgine_Collada_Source()
                            {
                                ID = "NormalSource_" + MakeID(mesh),
                                Name = mesh.name + "_normal",
                                Float_Array = array,
                                Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                {
                                    Accessor = new Grendgine_Collada_Accessor()
                                    {
                                        Count = (uint)mesh.vertexCount,
                                        Source = "#" + array.ID,
                                        Stride = 3,
                                        Param = new Grendgine_Collada_Param[]
                                        {
                                            new Grendgine_Collada_Param() { Name = "X", Type = "float", },
                                            new Grendgine_Collada_Param() { Name = "Y", Type = "float", },
                                            new Grendgine_Collada_Param() { Name = "Z", Type = "float", },
                                        },
                                    },
                                },
                            };
                        }
                        #endregion
                        #endregion

                        #region Vertices
                        Grendgine_Collada_Vertices vertices;
                        {
                            vertices = new Grendgine_Collada_Vertices()
                            {
                                ID = "Vertices_" + MakeID(mesh),
                                Name = mesh.name + "_vertices",
                                Input = new Grendgine_Collada_Input_Unshared[]
                                {
                                    new Grendgine_Collada_Input_Unshared()
                                    {
                                        Semantic = Grendgine_Collada_Input_Semantic.POSITION,
                                        source = "#" + vertexSource.ID,
                                    },
                                },
                            };
                        }
                        #endregion

                        #region Triangles
                        Grendgine_Collada_Triangles[] triangles;
                        {
                            triangles = new Grendgine_Collada_Triangles[mesh.subMeshCount];
                            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                            {
                                if (mesh.GetTopology(subMesh) != MeshTopology.Triangles)
                                {
                                    Debug.LogWarningFormat("<color=green>[{0}]</color> MeshTopology is not Triangles. Mesh = {1} - {2}, MeshTopology = {3}", ContributorAuthoring_Tool, mesh.name, subMesh, mesh.GetTopology(subMesh));
                                    continue;
                                }
                                Grendgine_Collada_Material material = null;
                                materialsDic.TryGetValue(materials[subMesh], out material);
                                var ts = mesh.GetTriangles(subMesh);
                                var sb = new StringBuilder();
                                {
                                    for (int i = 0; i < ts.Length; i += 3)
                                        sb.AppendFormat("\n{0} {0} {0} {1} {1} {1} {2} {2} {2}", ts[i + 0], ts[i + 2], ts[i + 1]);
                                }
                                triangles[subMesh] = new Grendgine_Collada_Triangles()
                                {
                                    Count = ts.Length / 3,
                                    Name = mesh.name + "_triangles",
                                    Material = material != null ? material.ID : null,
                                    Input = new Grendgine_Collada_Input_Shared[]
                                    {
                                        new Grendgine_Collada_Input_Shared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.VERTEX,
                                            source = "#" + vertices.ID,
                                            Offset = 0,
                                        },
                                        new Grendgine_Collada_Input_Shared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.TEXCOORD,
                                            source = "#" + uvSource.ID,
                                            Offset = 1,
                                        },
                                        new Grendgine_Collada_Input_Shared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.NORMAL,
                                            source = "#" + normalSource.ID,
                                            Offset = 2,
                                        },
                                    },
                                    P = new Grendgine_Collada_Int_Array_String()
                                    {
                                        Value_As_String = sb.ToString(),
                                    },
                                };
                            }
                        }
                        #endregion

                        var g = new Grendgine_Collada_Geometry()
                        {
                            ID = "Geometry_" + MakeID(mesh),
                            Name = mesh.name,
                            Mesh = new Grendgine_Collada_Mesh()
                            {
                                Source = new Grendgine_Collada_Source[]
                                {
                                    vertexSource,
                                    uvSource,
                                    normalSource,
                                },
                                Vertices = vertices,
                                Triangles = triangles,
                            },
                        };
                        geometriesDic.Add(t, g);
                    });
                    lg.Geometry = geometriesDic.Values.ToArray();
                }
                #endregion

                #region Nodes
                var nodesDic = new Dictionary<Transform, Grendgine_Collada_Node>();
                {
                    Func<Transform, Grendgine_Collada_Node> MakeNode = null;
                    MakeNode = (t) =>
                    {
                        var node = new Grendgine_Collada_Node()
                        {
                            ID = "Node_" + MakeID(t),
                            Name = t.name,
                            sID = "Node_" + MakeID(t),
                            Type = Grendgine_Collada_Node_Type.NODE,
                        };
                        {
                            var mat = Matrix4x4.TRS(matMirrorX.MultiplyPoint3x4(t.localPosition),
                                                    new Quaternion(t.localRotation.x, -t.localRotation.y, -t.localRotation.z, t.localRotation.w), //mirrorX
                                                    t.localScale);
                            var sb = new StringBuilder();
                            for (int r = 0; r < 4; r++)
                                for (int c = 0; c < 4; c++)
                                    sb.AppendFormat("{0} ", mat[r, c]);
                            sb.Remove(sb.Length - 1, 1);
                            node.Matrix = new Grendgine_Collada_Matrix[]
                            {
                                new Grendgine_Collada_Matrix()
                                {
                                    sID = "transform",
                                    Value_As_String = sb.ToString(),
                                },
                            };
                        }
                        {
                            List<Grendgine_Collada_Node> nodes = new List<Grendgine_Collada_Node>();
                            for (int i = 0; i < t.childCount; i++)
                            {
                                var ct = t.GetChild(i);
                                if (!transforms.Contains(ct)) continue;
                                if (!t.gameObject.activeInHierarchy) continue;
                                var n = MakeNode(ct);
                                nodes.Add(n);
                                nodesDic.Add(ct, n);
                            }
                            node.node = nodes.ToArray();
                        }
                        if (geometriesDic.ContainsKey(t))
                        {
                            var mesh = MeshFromTransform(t);
                            if (mesh == null) return node;
                            var materials = MaterialsFromTransform(t);
                            if (materials == null) return node;
                            var Instance_Material = new Grendgine_Collada_Instance_Material_Geometry[materials.Length];
                            for (int j = 0; j < materials.Length; j++)
                            {
                                if (!materialsDic.ContainsKey(materials[j])) continue;
                                var mat = materialsDic[materials[j]];
                                Instance_Material[j] = new Grendgine_Collada_Instance_Material_Geometry()
                                {
                                    Target = "#" + mat.ID,
                                    Symbol = mat.ID,
                                };
                                if (effectsDic[materials[j]].Profile_COMMON[0].Technique.Phong.Diffuse.Texture != null)
                                {
                                    Instance_Material[j].Bind_Vertex_Input = new Grendgine_Collada_Bind_Vertex_Input[]
                                    {
                                        new Grendgine_Collada_Bind_Vertex_Input()
                                        {
                                            Input_Semantic = "TEXCOORD",
                                            Input_Set = 1,
                                            Semantic = effectsDic[materials[j]].Profile_COMMON[0].Technique.Phong.Diffuse.Texture.TexCoord,
                                        },
                                    };
                                }
                            }
                            node.Instance_Geometry = new Grendgine_Collada_Instance_Geometry[]
                            {
                                new Grendgine_Collada_Instance_Geometry()
                                {
                                    URL = "#" + geometriesDic[t].ID,
                                    Bind_Material = new Grendgine_Collada_Bind_Material[]
                                    {
                                        new Grendgine_Collada_Bind_Material()
                                        {
                                            Technique_Common = new Grendgine_Collada_Technique_Common_Bind_Material()
                                            {
                                                Instance_Material = Instance_Material,
                                            },
                                        },
                                    },
                                },
                            };
                        }
                        return node;
                    };
                    nodesDic.Add(rootObject.transform, MakeNode(rootObject.transform));
                }
                #endregion

                #region Joints
                var jointsDic = new Dictionary<Transform, Grendgine_Collada_Node>();
                if (makeJoint)
                {
                    Func<Transform, Grendgine_Collada_Node> MakeJoint = null;
                    MakeJoint = (t) =>
                    {
                        var Doc = new System.Xml.XmlDocument();
                        var Data = new System.Xml.XmlElement[]
                        {
                            Doc.CreateElement("tip_x"),
                            Doc.CreateElement("tip_y"),
                            Doc.CreateElement("tip_z"),
                        };
                        bool enable = true;
                        {
                            Vector3 offset = Vector3.zero;
                            if (t.childCount > 0)
                            {
                                float dotMax = float.MinValue;
                                for (int i = 0; i < t.childCount; i++)
                                {
                                    var vec = rootObject.transform.worldToLocalMatrix.MultiplyVector(t.GetChild(i).position - t.position);
                                    vec = matMirrorX.MultiplyVector(vec);
                                    var dot = Mathf.Abs(Vector3.Dot(vec, Vector3.up));
                                    if (dot > dotMax)
                                    {
                                        offset = vec;
                                        dotMax = dot;
                                    }
                                }
                            }
                            else
                            {
                                var vec = rootObject.transform.worldToLocalMatrix.MultiplyVector(t.position - t.parent.position);
                                if (vec.sqrMagnitude > 0)
                                {
                                    vec = vec.normalized * 0.0001f;
                                    offset = matMirrorX.MultiplyVector(vec);
                                }
                                else
                                {
                                    offset = new Vector3(0, 0, 0.0001f);
                                }
                            }
                            if (offset.sqrMagnitude <= 0f)
                                enable = false;
                            Data[0].InnerText = offset.x.ToString();
                            Data[1].InnerText = offset.y.ToString();
                            Data[2].InnerText = offset.z.ToString();
                        }
                        Grendgine_Collada_Node joint = new Grendgine_Collada_Node()
                        {
                            ID = "Joint_" + MakeID(t),
                            Name = t.name,
                            sID = "Joint_" + MakeID(t),
                            Type = Grendgine_Collada_Node_Type.JOINT,
                            Matrix = nodesDic[t].Matrix,
                            Extra = enable ? new Grendgine_Collada_Extra[]
                            {
                                new Grendgine_Collada_Extra()
                                {
                                    Technique = new Grendgine_Collada_Technique[]
                                    {
                                        new Grendgine_Collada_Technique()
                                        {
                                            profile = "blender",
                                            Data = Data,
                                        },
                                    },
                                },
                            } : null,
                        };

                        List<Grendgine_Collada_Node> joints = new List<Grendgine_Collada_Node>();
                        for (int i = 0; i < t.childCount; i++)
                        {
                            var ct = t.GetChild(i);
                            if (!transforms.Contains(ct)) continue;
                            if (!t.gameObject.activeInHierarchy) continue;
                            var n = MakeJoint(ct);
                            joints.Add(n);
                            jointsDic.Add(ct, n);
                        }
                        joint.node = joints.ToArray();
                        return joint;
                    };
                    jointsDic.Add(rootObject.transform, MakeJoint(rootObject.transform));
                }
                #endregion

                #region Controllers
                var controllersDic = new Dictionary<Transform, Grendgine_Collada_Controller>();
                if (makeJoint && settings_exportMesh)
                {
                    var lc = gCollada.Library_Controllers = new Grendgine_Collada_Library_Controllers()
                    {
                        ID = "Controllers_" + MakeID(rootObject),
                        Name = "Controllers_" + rootObject.name,
                    };

                    foreach (var t in transforms)
                    {
                        if (!t.gameObject.activeInHierarchy) continue;
                        Mesh mesh = null;
                        Material[] materials = null;
                        #region SkinnedMeshRenderer
                        var skinnedMeshRenderer = t.GetComponent<SkinnedMeshRenderer>();
                        {
                            if (skinnedMeshRenderer != null && skinnedMeshRenderer.enabled && skinnedMeshRenderer.sharedMesh != null && skinnedMeshRenderer.sharedMaterials != null)
                            {
                                mesh = skinnedMeshRenderer.sharedMesh;
                                materials = skinnedMeshRenderer.sharedMaterials;
                            }
                        }
                        #endregion
                        #region MeshFilter
                        var meshFilter = t.GetComponent<MeshFilter>();
                        var meshRenderer = t.GetComponent<MeshRenderer>();
                        {
                            if (meshFilter != null && meshFilter.sharedMesh != null && meshRenderer != null && meshRenderer.enabled && meshRenderer.sharedMaterials != null)
                            {
                                mesh = meshFilter.sharedMesh;
                                materials = meshRenderer.sharedMaterials;
                            }
                        }
                        #endregion

                        Grendgine_Collada_Controller c;
                        if (mesh != null && materials != null && skinnedMeshRenderer != null && mesh.boneWeights.Length > 0)
                        {
                            #region SkinnedMeshRenderer
                            var bones = skinnedMeshRenderer.bones;
                            var boneWeights = mesh.boneWeights;

                            #region ErrorCheck
                            {
                                var checkBones = new List<Transform>(bones.Distinct());
                                if (checkBones.Count != bones.Length)
                                {
                                    Debug.LogWarningFormat("<color=green>[{0}]</color> There are two or more same Transforms in SkinnedMeshRenderer.bones. This is not supported.", ContributorAuthoring_Tool);
                                }
                            }
                            #endregion

                            #region Joints_Source
                            Grendgine_Collada_Source Joints_Source;
                            {
                                var Joints_Name_Array = new Grendgine_Collada_Name_Array()
                                {
                                    ID = "Joints_Name_Array_" + MakeID(t),
                                    Count = bones.Length,
                                };
                                {
                                    var names = new StringBuilder();
                                    foreach (var bone in bones)
                                    {
                                        if (bone != null && jointsDic.ContainsKey(bone))
                                            names.AppendFormat("\n{0}", jointsDic[bone].ID);
                                        else
                                            names.AppendFormat("\n{0}", 0);
                                    }
                                    Joints_Name_Array.Value_Pre_Parse = names.ToString();
                                }
                                Joints_Source = new Grendgine_Collada_Source()
                                {
                                    ID = "Joints_" + MakeID(t),
                                    Name_Array = Joints_Name_Array,
                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                    {
                                        Accessor = new Grendgine_Collada_Accessor()
                                        {
                                            Count = (uint)Joints_Name_Array.Count,
                                            Source = "#" + Joints_Name_Array.ID,
                                            Param = new Grendgine_Collada_Param[]
                                            {
                                                new Grendgine_Collada_Param()
                                                {
                                                    Type = "name",
                                                },
                                            },
                                        },
                                    },
                                };
                            }
                            #endregion
                            #region Weights_Source
                            Grendgine_Collada_Source Weights_Source;
                            StringBuilder weightsVCountString = new StringBuilder();
                            StringBuilder weightsVString = new StringBuilder();
                            List<float> weightList = new List<float>();
                            {
                                var Weights_Float_Array = new Grendgine_Collada_Float_Array()
                                {
                                    ID = "Weights_Float_Array_" + MakeID(t),
                                };
                                {
                                    var sb = new StringBuilder();
                                    for (int i = 0; i < boneWeights.Length; i++)
                                    {
                                        int count = 0;
                                        {
                                            if (!weightList.Contains(boneWeights[i].weight0))
                                            {
                                                weightList.Add(boneWeights[i].weight0);
                                                sb.AppendFormat("\n{0}", boneWeights[i].weight0);
                                            }
                                            weightsVString.AppendFormat("\n{0} {1}", boneWeights[i].boneIndex0, weightList.IndexOf(boneWeights[i].weight0));
                                            count++;
                                        }
                                        if (boneWeights[i].weight1 > 0f)
                                        {
                                            if (!weightList.Contains(boneWeights[i].weight1))
                                            {
                                                weightList.Add(boneWeights[i].weight1);
                                                sb.AppendFormat("\n{0}", boneWeights[i].weight1);
                                            }
                                            weightsVString.AppendFormat(" {0} {1}", boneWeights[i].boneIndex1, weightList.IndexOf(boneWeights[i].weight1));
                                            count++;
                                        }
                                        if (boneWeights[i].weight2 > 0f)
                                        {
                                            if (!weightList.Contains(boneWeights[i].weight2))
                                            {
                                                weightList.Add(boneWeights[i].weight2);
                                                sb.AppendFormat("\n{0}", boneWeights[i].weight2);
                                            }
                                            weightsVString.AppendFormat(" {0} {1}", boneWeights[i].boneIndex2, weightList.IndexOf(boneWeights[i].weight2));
                                            count++;
                                        }
                                        if (boneWeights[i].weight3 > 0f)
                                        {
                                            if (!weightList.Contains(boneWeights[i].weight3))
                                            {
                                                weightList.Add(boneWeights[i].weight3);
                                                sb.AppendFormat("\n{0}", boneWeights[i].weight3);
                                            }
                                            weightsVString.AppendFormat(" {0} {1}", boneWeights[i].boneIndex3, weightList.IndexOf(boneWeights[i].weight3));
                                            count++;
                                        }
                                        weightsVCountString.AppendFormat("\n{0}", count);
                                    }
                                    Weights_Float_Array.Count = weightList.Count;
                                    Weights_Float_Array.Value_As_String = sb.ToString();
                                }
                                Weights_Source = new Grendgine_Collada_Source()
                                {
                                    ID = "Weights_" + MakeID(t),
                                    Float_Array = Weights_Float_Array,
                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                    {
                                        Accessor = new Grendgine_Collada_Accessor()
                                        {
                                            Count = (uint)Weights_Float_Array.Count,
                                            Source = "#" + Weights_Float_Array.ID,
                                            Param = new Grendgine_Collada_Param[]
                                            {
                                                new Grendgine_Collada_Param()
                                                {
                                                    Type = "float",
                                                },
                                            },
                                        },
                                    },
                                };
                            }
                            #endregion
                            #region Inv_Bind_Mats_Source
                            Grendgine_Collada_Source Inv_Bind_Mats_Source;
                            {
                                var Inv_Bind_Mats_Float_Array = new Grendgine_Collada_Float_Array()
                                {
                                    ID = "Inv_Bind_Mats_" + MakeID(t),
                                };
                                {
                                    var bindposes = skinnedMeshRenderer.sharedMesh.bindposes;
                                    var sb = new StringBuilder();
                                    for (int i = 0; i < bindposes.Length; i++)
                                    {
                                        Matrix4x4 mat = bindposes[i];
                                        {
                                            var position = mat.GetColumn(3);
                                            var rotation = (mat.GetColumn(2).sqrMagnitude > 0f && mat.GetColumn(1).sqrMagnitude > 0f) ? Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1)) : Quaternion.identity;
                                            var scale = new Vector3(mat.GetColumn(0).magnitude, mat.GetColumn(1).magnitude, mat.GetColumn(2).magnitude);
                                            #region Do not allow scale zero
                                            {
                                                for (int si = 0; si < 3; si++)
                                                {
                                                    if (scale[si] == 0f)
                                                        scale[si] = Mathf.Epsilon;
                                                }
                                            }
                                            #endregion
                                            mat = Matrix4x4.TRS(matMirrorX.MultiplyPoint3x4(position),
                                                                new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w), //mirrorX
                                                                scale);
                                        }
                                        for (int r = 0; r < 4; r++)
                                            sb.AppendFormat("\n{0} {1} {2} {3}", mat[r, 0], mat[r, 1], mat[r, 2], mat[r, 3]);
                                    }
                                    Inv_Bind_Mats_Float_Array.Count = bindposes.Length * 16;
                                    Inv_Bind_Mats_Float_Array.Value_As_String = sb.ToString();
                                }
                                Inv_Bind_Mats_Source = new Grendgine_Collada_Source()
                                {
                                    ID = "Inv_Bind_Mats_" + MakeID(t),
                                    Float_Array = Inv_Bind_Mats_Float_Array,
                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                    {
                                        Accessor = new Grendgine_Collada_Accessor()
                                        {
                                            Count = (uint)(Inv_Bind_Mats_Float_Array.Count / 16),
                                            Source = "#" + Inv_Bind_Mats_Float_Array.ID,
                                            Stride = 16,
                                            Param = new Grendgine_Collada_Param[]
                                            {
                                                new Grendgine_Collada_Param()
                                                {
                                                    Type = "float4x4",
                                                },
                                            },
                                        },
                                    },
                                };
                            }
                            #endregion

                            c = new Grendgine_Collada_Controller()
                            {
                                ID = "Controller_" + MakeID(t),
                                Skin = new Grendgine_Collada_Skin()
                                {
                                    SourceAt = "#" + geometriesDic[t].ID,
                                    Source = new Grendgine_Collada_Source[]
                                    {
                                        Joints_Source,
                                        Weights_Source,
                                        Inv_Bind_Mats_Source,
                                    },
                                    Joints = new Grendgine_Collada_Joints()
                                    {
                                        Input = new Grendgine_Collada_Input_Unshared[]
                                        {
                                        new Grendgine_Collada_Input_Unshared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.JOINT,
                                            source = "#" + Joints_Source.ID,
                                        },
                                        new Grendgine_Collada_Input_Unshared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.INV_BIND_MATRIX,
                                            source = "#" + Inv_Bind_Mats_Source.ID,
                                        },
                                        },
                                    },
                                    Vertex_Weights = new Grendgine_Collada_Vertex_Weights()
                                    {
                                        Count = (uint)boneWeights.Length,
                                        Input = new Grendgine_Collada_Input_Shared[]
                                        {
                                        new Grendgine_Collada_Input_Shared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.JOINT,
                                            source = "#" + Joints_Source.ID,
                                            Offset = 0,
                                            Set = 0,
                                        },
                                        new Grendgine_Collada_Input_Shared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.WEIGHT,
                                            source = "#" + Weights_Source.ID,
                                            Offset = 1,
                                            Set = 0,
                                        },
                                        },
                                        VCount = new Grendgine_Collada_Int_Array_String()
                                        {
                                            Value_As_String = weightsVCountString.ToString(),
                                        },
                                        V = new Grendgine_Collada_Int_Array_String()
                                        {
                                            Value_As_String = weightsVString.ToString(),
                                        },
                                    },
                                },
                            };
                            #endregion
                        }
                        else if (mesh != null && materials != null)
                        {
                            #region MeshRenderer
                            var vertexCount = mesh.vertexCount;
                            const int boneCount = 1;

                            #region Joints_Source
                            Grendgine_Collada_Source Joints_Source;
                            {
                                var Joints_Name_Array = new Grendgine_Collada_Name_Array()
                                {
                                    ID = "Joints_Name_Array_" + MakeID(t),
                                    Count = boneCount,
                                };
                                {
                                    var names = new StringBuilder();
                                    {
                                        names.AppendFormat("\n{0}", jointsDic[t].ID);
                                    }
                                    Joints_Name_Array.Value_Pre_Parse = names.ToString();
                                }
                                Joints_Source = new Grendgine_Collada_Source()
                                {
                                    ID = "Joints_" + MakeID(t),
                                    Name_Array = Joints_Name_Array,
                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                    {
                                        Accessor = new Grendgine_Collada_Accessor()
                                        {
                                            Count = (uint)Joints_Name_Array.Count,
                                            Source = "#" + Joints_Name_Array.ID,
                                            Param = new Grendgine_Collada_Param[]
                                            {
                                                new Grendgine_Collada_Param()
                                                {
                                                    Type = "name",
                                                },
                                            },
                                        },
                                    },
                                };
                            }
                            #endregion
                            #region Weights_Source
                            Grendgine_Collada_Source Weights_Source;
                            StringBuilder weightsVCountString = new StringBuilder();
                            StringBuilder weightsVString = new StringBuilder();
                            List<float> weightList = new List<float>();
                            {
                                var Weights_Float_Array = new Grendgine_Collada_Float_Array()
                                {
                                    ID = "Weights_Float_Array_" + MakeID(t),
                                };
                                {
                                    const float weight0 = 1f;
                                    const int index0 = 0;

                                    var sb = new StringBuilder();
                                    for (int i = 0; i < vertexCount; i++)
                                    {
                                        int count = 0;
                                        {
                                            if (!weightList.Contains(weight0))
                                            {
                                                weightList.Add(weight0);
                                                sb.AppendFormat("\n{0}", weight0);
                                            }
                                            weightsVString.AppendFormat("\n{0} {1}", 0, index0);
                                            count++;
                                        }
                                        weightsVCountString.AppendFormat("\n{0}", count);
                                    }
                                    Weights_Float_Array.Count = weightList.Count;
                                    Weights_Float_Array.Value_As_String = sb.ToString();
                                }
                                Weights_Source = new Grendgine_Collada_Source()
                                {
                                    ID = "Weights_" + MakeID(t),
                                    Float_Array = Weights_Float_Array,
                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                    {
                                        Accessor = new Grendgine_Collada_Accessor()
                                        {
                                            Count = (uint)Weights_Float_Array.Count,
                                            Source = "#" + Weights_Float_Array.ID,
                                            Param = new Grendgine_Collada_Param[]
                                            {
                                                new Grendgine_Collada_Param()
                                                {
                                                    Type = "float",
                                                },
                                            },
                                        },
                                    },
                                };
                            }
                            #endregion
                            #region Inv_Bind_Mats_Source
                            Grendgine_Collada_Source Inv_Bind_Mats_Source;
                            {
                                var Inv_Bind_Mats_Float_Array = new Grendgine_Collada_Float_Array()
                                {
                                    ID = "Inv_Bind_Mats_" + MakeID(t),
                                };
                                {
                                    var sb = new StringBuilder();
                                    for (int i = 0; i < boneCount; i++)
                                    {
                                        Matrix4x4 mat = Matrix4x4.identity;
                                        for (int r = 0; r < 4; r++)
                                            sb.AppendFormat("\n{0} {1} {2} {3}", mat[r, 0], mat[r, 1], mat[r, 2], mat[r, 3]);
                                    }
                                    Inv_Bind_Mats_Float_Array.Count = boneCount * 16;
                                    Inv_Bind_Mats_Float_Array.Value_As_String = sb.ToString();
                                }
                                Inv_Bind_Mats_Source = new Grendgine_Collada_Source()
                                {
                                    ID = "Inv_Bind_Mats_" + MakeID(t),
                                    Float_Array = Inv_Bind_Mats_Float_Array,
                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                    {
                                        Accessor = new Grendgine_Collada_Accessor()
                                        {
                                            Count = (uint)(Inv_Bind_Mats_Float_Array.Count / 16),
                                            Source = "#" + Inv_Bind_Mats_Float_Array.ID,
                                            Stride = 16,
                                            Param = new Grendgine_Collada_Param[]
                                            {
                                                new Grendgine_Collada_Param()
                                                {
                                                    Type = "float4x4",
                                                },
                                            },
                                        },
                                    },
                                };
                            }
                            #endregion

                            c = new Grendgine_Collada_Controller()
                            {
                                ID = "Controller_" + MakeID(t),
                                Skin = new Grendgine_Collada_Skin()
                                {
                                    SourceAt = "#" + geometriesDic[t].ID,
                                    Source = new Grendgine_Collada_Source[]
                                    {
                                        Joints_Source,
                                        Weights_Source,
                                        Inv_Bind_Mats_Source,
                                    },
                                    Joints = new Grendgine_Collada_Joints()
                                    {
                                        Input = new Grendgine_Collada_Input_Unshared[]
                                        {
                                        new Grendgine_Collada_Input_Unshared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.JOINT,
                                            source = "#" + Joints_Source.ID,
                                        },
                                        new Grendgine_Collada_Input_Unshared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.INV_BIND_MATRIX,
                                            source = "#" + Inv_Bind_Mats_Source.ID,
                                        },
                                        },
                                    },
                                    Vertex_Weights = new Grendgine_Collada_Vertex_Weights()
                                    {
                                        Count = (uint)vertexCount,
                                        Input = new Grendgine_Collada_Input_Shared[]
                                        {
                                        new Grendgine_Collada_Input_Shared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.JOINT,
                                            source = "#" + Joints_Source.ID,
                                            Offset = 0,
                                            Set = 0,
                                        },
                                        new Grendgine_Collada_Input_Shared()
                                        {
                                            Semantic = Grendgine_Collada_Input_Semantic.WEIGHT,
                                            source = "#" + Weights_Source.ID,
                                            Offset = 1,
                                            Set = 0,
                                        },
                                        },
                                        VCount = new Grendgine_Collada_Int_Array_String()
                                        {
                                            Value_As_String = weightsVCountString.ToString(),
                                        },
                                        V = new Grendgine_Collada_Int_Array_String()
                                        {
                                            Value_As_String = weightsVString.ToString(),
                                        },
                                    },
                                },
                            };
                            #endregion
                        }
                        else
                        {
                            continue;
                        }

                        controllersDic.Add(t, c);
                        #region Node
                        nodesDic[t].Instance_Controller = new Grendgine_Collada_Instance_Controller[]
                        {
                            new Grendgine_Collada_Instance_Controller()
                            {
                                URL = "#" + c.ID,
                                Bind_Material = nodesDic[t].Instance_Geometry[0].Bind_Material,
                            },
                        };
                        nodesDic[t].Instance_Geometry = null;
                        #endregion
                    }
                    lc.Controller = controllersDic.Values.ToArray();
                }
                #endregion

                #region Scene
                {
                    gCollada.Library_Visual_Scene = new Grendgine_Collada_Library_Visual_Scenes()
                    {
                        Visual_Scene = new Grendgine_Collada_Visual_Scene[]
                        {
                            new Grendgine_Collada_Visual_Scene()
                            {
                                ID = "Scene_" + MakeID(rootObject),
                                Name = "Scene",
                            },
                        },
                    };
                    {
                        List<Grendgine_Collada_Node> nodes = new List<Grendgine_Collada_Node>();
                        if (makeJoint)
                        {
                            for (int i = 0; i < rootObject.transform.childCount; i++)
                            {
                                var t = rootObject.transform.GetChild(i);
                                if (jointsDic.ContainsKey(t))
                                    nodes.Add(jointsDic[t]);
                            }
                            {
                                Func<string, string> GetUniqueName = null;
                                GetUniqueName = (name) =>
                                {
                                    foreach (var pair in jointsDic)
                                    {
                                        if (pair.Value.Name == name)
                                            return GetUniqueName(name + "_");
                                    }
                                    foreach (var pair in nodesDic)
                                    {
                                        if (pair.Value.Name == name)
                                            return GetUniqueName(name + "_");
                                    }
                                    return name;
                                };

                                List<Grendgine_Collada_Node> list = new List<Grendgine_Collada_Node>();
                                foreach (var pair in nodesDic)
                                {
                                    if (pair.Value.Instance_Geometry != null ||
                                        pair.Value.Instance_Controller != null)
                                    {
                                        var node = pair.Value;
                                        node.Name = GetUniqueName("Mesh_" + node.Name);
                                        node.node = null;
                                        node.Matrix = new Grendgine_Collada_Matrix[]
                                        {
                                            MatrixIdentity,
                                        };
                                        list.Add(node);
                                    }
                                }
                                nodes.AddRange(list);
                            }
                        }
                        else
                        {
                            nodes.Add(nodesDic[rootObject.transform]);
                        }
                        gCollada.Library_Visual_Scene.Visual_Scene[0].Node = nodes.ToArray();
                    }
                    gCollada.Scene = new Grendgine_Collada_Scene()
                    {
                        Visual_Scene = new Grendgine_Collada_Instance_Visual_Scene()
                        {
                            URL = "#" + gCollada.Library_Visual_Scene.Visual_Scene[0].ID,
                        },
                    };
                }
                #endregion

                #region Write
                {
                    using (var writer = new StreamWriter(path))
                    {
                        var xmlSerializer = new XmlSerializer(typeof(Grendgine_Collada));
                        xmlSerializer.Serialize(writer, gCollada);
                    }
                    exportedFiles.Add(path);
                }
                if (clips != null)
                {
                    var tmpObject = GameObject.Instantiate<GameObject>(rootObject);
                    tmpObject.hideFlags |= HideFlags.HideAndDontSave;
                    tmpObject.transform.SetParent(null);
                    tmpObject.transform.localPosition = Vector3.zero;
                    tmpObject.transform.localRotation = Quaternion.identity;
                    tmpObject.transform.localScale = Vector3.one;
                    { //Remove OnAnimatorMove etc
                        var component = tmpObject.GetComponents<Component>();
                        for (int i = 0; i < component.Length; i++)
                        {
                            if (component[i] is Transform || component[i] is Animator || component[i] is Animation || component[i] is Renderer || component[i] is MeshFilter) continue;
                            try
                            {
                                Component.DestroyImmediate(component[i]);
                            }
                            catch
                            {
                            }
                        }
                    }
                    Avatar tmpAvatar = null;
                    AnimationClip tmpClip = null;
                    try
                    {
                        var animator = tmpObject.GetComponent<Animator>();
                        if (animator != null && animator.isHuman && animator.avatar != null)
                        {
                            tmpAvatar = Avatar.Instantiate<Avatar>(animator.avatar);
                            tmpAvatar.hideFlags |= HideFlags.HideAndDontSave;
                            {
                                var serializedObject = new UnityEditor.SerializedObject(tmpAvatar);
                                var pArmStretch = serializedObject.FindProperty("m_Avatar.m_Human.data.m_ArmStretch");
                                pArmStretch.floatValue = 0.0001f;   //Since it is occasionally wrong value when it is 0
                                var pLegStretch = serializedObject.FindProperty("m_Avatar.m_Human.data.m_LegStretch");
                                pLegStretch.floatValue = 0.0001f;   //Since it is occasionally wrong value when it is 0
                                serializedObject.ApplyModifiedProperties();
                            }
                            animator.avatar = tmpAvatar;
                        }
                        var animation = tmpObject.GetComponent<Animation>();
                        if (animator != null || animation != null)
                        {
                            string[] paths = new string[transforms.Count];
                            Transform[] tmpTransforms = new Transform[transforms.Count];
                            {
                                Func<Transform, string, Transform> FindPathTransform = null;
                                FindPathTransform = (t, p) =>
                                {
                                    if (AnimationUtility.CalculateTransformPath(t, tmpObject.transform) == p)
                                        return t;
                                    for (int i = 0; i < t.childCount; i++)
                                    {
                                        var rt = FindPathTransform(t.GetChild(i), p);
                                        if (rt != null) return rt;
                                    }
                                    return null;
                                };

                                for (int i = 0; i < transforms.Count; i++)
                                {
                                    paths[i] = AnimationUtility.CalculateTransformPath(transforms[i], rootObject.transform);
                                    tmpTransforms[i] = FindPathTransform(tmpObject.transform, paths[i]);
                                }
                            }
                            foreach (var clip in clips)
                            {
                                tmpClip = AnimationClip.Instantiate<AnimationClip>(clip);
                                tmpClip.hideFlags |= HideFlags.HideAndDontSave;
                                tmpClip.name = clip.name;
                                AnimationUtility.SetAnimationEvents(tmpClip, new AnimationEvent[0]);

                                var pathAnim = path.Insert(path.LastIndexOf(".dae"), "@" + tmpClip.name);
                                EditorUtility.DisplayProgressBar("Exporting Collada(dae) File...", Path.GetFileName(pathAnim), (progressIndex++ / (float)progressTotal));
                                #region enableTransforms
                                bool[] enableTransforms = new bool[transforms.Count];
                                {
                                    foreach (var binding in AnimationUtility.GetCurveBindings(tmpClip))
                                    {
                                        if (binding.type == typeof(Transform))
                                        {
                                            var index = ArrayUtility.IndexOf(paths, binding.path);
                                            if (index >= 0)
                                                enableTransforms[index] = true;
                                        }
                                    }
                                    if (animator != null && animator.isHuman)
                                    {
                                        if (!animator.isInitialized)
                                            animator.Rebind();
                                        for (HumanBodyBones hi = 0; hi < HumanBodyBones.LastBone; hi++)
                                        {
                                            var t = animator.GetBoneTransform(hi);
                                            while (t != null)
                                            {
                                                var index = ArrayUtility.IndexOf(tmpTransforms, t);
                                                if (index >= 0)
                                                    enableTransforms[index] = true;
                                                t = t.parent;
                                            }
                                        }
                                    }
                                }
                                #endregion
                                #region transformCurves
                                TransformCurves[] transformCurves = new TransformCurves[tmpTransforms.Length];
                                for (int i = 0; i < tmpTransforms.Length; i++)
                                {
                                    transformCurves[i] = new TransformCurves()
                                    {
                                        position = new AnimationCurve[]
                                        {
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalPosition.x")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalPosition.y")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalPosition.z")),
                                        },
                                        rotation = new AnimationCurve[]
                                        {
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalRotation.x")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalRotation.y")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalRotation.z")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalRotation.w")),
                                        },
                                        scale = new AnimationCurve[]
                                        {
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalScale.x")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalScale.y")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "m_LocalScale.z")),
                                        },
                                    };
                                    if (transformCurves[i].rotation[0] == null)
                                    {
                                        transformCurves[i].rotation = new AnimationCurve[]
                                        {
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "localEulerAnglesRaw.x")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "localEulerAnglesRaw.y")),
                                            AnimationUtility.GetEditorCurve(tmpClip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), "localEulerAnglesRaw.z")),
                                        };
                                    }
                                }
                                #endregion
                                AnimationClipSettings animationClipSettings = AnimationUtility.GetAnimationClipSettings(tmpClip);
                                var totalTime = animationClipSettings.stopTime - animationClipSettings.startTime;
                                #region frameTimes
                                float[] frameTimes;
                                {
                                    var lastFrame = Mathf.RoundToInt(totalTime * tmpClip.frameRate);
                                    frameTimes = new float[lastFrame + 1];
                                    for (int i = 0; i <= lastFrame; i++)
                                    {
                                        var time = i * (1f / tmpClip.frameRate);
                                        frameTimes[i] = Mathf.Round(time * tmpClip.frameRate) / tmpClip.frameRate;
                                    }
                                }
                                #endregion
                                #region Transforms
                                Matrix4x4[,] tmpTransformMatrixs = new Matrix4x4[tmpTransforms.Length, frameTimes.Length];
                                if (animator != null)
                                {
                                    animator.enabled = true;
                                    animator.applyRootMotion = true;
                                    animator.updateMode = AnimatorUpdateMode.Normal;
                                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                                    var originalAnimatorController = (animator != null) ? animator.runtimeAnimatorController : null;
                                    var tmpAnimatorController = new UnityEditor.Animations.AnimatorController();
                                    tmpAnimatorController.name = "Temporary Controller";
                                    tmpAnimatorController.hideFlags |= HideFlags.HideAndDontSave;
                                    {
                                        tmpAnimatorController.AddLayer("Temporary Layer");
                                        var layers = tmpAnimatorController.layers;
                                        foreach (var layer in layers)
                                        {
                                            var stateMachine = layer.stateMachine;
                                            stateMachine.hideFlags |= HideFlags.HideAndDontSave;
                                            var state = stateMachine.AddState(tmpClip.name);
                                            state.hideFlags |= HideFlags.HideAndDontSave;
                                            state.iKOnFeet = settings_iKOnFeet;
                                            state.motion = tmpClip;
                                            layer.stateMachine = stateMachine;
                                        }
                                        tmpAnimatorController.layers = layers;
                                    }
                                    UnityEditor.Animations.AnimatorController.SetAnimatorController(animator, tmpAnimatorController);
                                    if (!animator.isInitialized)
                                        animator.Rebind();
                                    #region ResetTransform
                                    {
                                        tmpTransforms[0].localPosition = Vector3.zero;
                                        tmpTransforms[0].localRotation = Quaternion.identity;
                                        tmpTransforms[0].localScale = Vector3.one;
                                    }
                                    for (int i = 1; i < transforms.Count; i++)
                                    {
                                        tmpTransforms[i].localPosition = transforms[i].localPosition;
                                        tmpTransforms[i].localRotation = transforms[i].localRotation;
                                        tmpTransforms[i].localScale = transforms[i].localScale;
                                    }
                                    #endregion
                                    {
                                        if (animator.isHuman)
                                        {
                                            #region Humanoid
                                            animator.Play(tmpClip.name);
                                            animator.Update(0f);
                                            for (int i = 0; i < frameTimes.Length; i++)
                                            {
                                                tmpClip.SampleAnimation(tmpObject, frameTimes[i]); //It is not essential. Workarounds for unknown bugs where hair etc. are not reflected correctly.

                                                var currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
                                                var time = currentAnimatorStateInfo.length * currentAnimatorStateInfo.normalizedTime;
                                                animator.Update(frameTimes[i] - time);

                                                for (int j = 0; j < tmpTransforms.Length; j++)
                                                {
                                                    var position = transformCurves[j].GetPosition(frameTimes[i]);
                                                    var rotation = transformCurves[j].GetRotation(frameTimes[i]);
                                                    var scale = transformCurves[j].GetScale(frameTimes[i]);
                                                    tmpTransformMatrixs[j, i] = Matrix4x4.TRS(position.HasValue ? position.Value : tmpTransforms[j].localPosition,
                                                                                                rotation.HasValue ? rotation.Value : tmpTransforms[j].localRotation,
                                                                                                scale.HasValue ? scale.Value : tmpTransforms[j].localScale);
                                                }
                                            }
                                            #region BakeRootMotion
                                            {
                                                int bakeIndex = -1;
                                                {
                                                    var t = animator.GetBoneTransform(HumanBodyBones.Hips);
                                                    while (t.parent != tmpObject.transform)
                                                        t = t.parent;
                                                    bakeIndex = ArrayUtility.IndexOf(tmpTransforms, t);
                                                }
                                                if (bakeIndex >= 0)
                                                {
                                                    for (int i = 0; i < frameTimes.Length; i++)
                                                    {
                                                        tmpTransformMatrixs[bakeIndex, i] = tmpTransformMatrixs[0, i] * tmpTransformMatrixs[bakeIndex, i];
                                                    }
                                                    for (int i = 0; i < frameTimes.Length; i++)
                                                    {
                                                        tmpTransformMatrixs[0, i] = Matrix4x4.identity;
                                                    }
                                                }
                                            }
                                            #endregion
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Generic
                                            for (int i = 0; i < frameTimes.Length; i++)
                                            {
                                                tmpClip.SampleAnimation(tmpObject, frameTimes[i]);

                                                for (int j = 0; j < tmpTransforms.Length; j++)
                                                {
                                                    var position = transformCurves[j].GetPosition(frameTimes[i]);
                                                    var rotation = transformCurves[j].GetRotation(frameTimes[i]);
                                                    var scale = transformCurves[j].GetScale(frameTimes[i]);
                                                    tmpTransformMatrixs[j, i] = Matrix4x4.TRS(position.HasValue ? position.Value : tmpTransforms[j].localPosition,
                                                                                                rotation.HasValue ? rotation.Value : tmpTransforms[j].localRotation,
                                                                                                scale.HasValue ? scale.Value : tmpTransforms[j].localScale);
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    {
                                        var layerCount = tmpAnimatorController.layers.Length;
                                        var parameterCount = tmpAnimatorController.parameters.Length;
                                        for (int i = 0; i < layerCount; i++)
                                            tmpAnimatorController.RemoveLayer(0);
                                        for (int i = 0; i < parameterCount; i++)
                                            tmpAnimatorController.RemoveParameter(0);
                                    }
                                    UnityEditor.Animations.AnimatorController.DestroyImmediate(tmpAnimatorController);
                                    tmpAnimatorController = null;
                                    animator.runtimeAnimatorController = originalAnimatorController;
                                }
                                else if (animation != null)
                                {
                                    #region Legacy
                                    for (int i = 0; i < frameTimes.Length; i++)
                                    {
                                        tmpClip.SampleAnimation(tmpObject, frameTimes[i]);

                                        for (int j = 0; j < tmpTransforms.Length; j++)
                                        {
                                            var position = transformCurves[j].GetPosition(frameTimes[i]);
                                            var rotation = transformCurves[j].GetRotation(frameTimes[i]);
                                            var scale = transformCurves[j].GetScale(frameTimes[i]);
                                            tmpTransformMatrixs[j, i] = Matrix4x4.TRS(position.HasValue ? position.Value : tmpTransforms[j].localPosition,
                                                                                        rotation.HasValue ? rotation.Value : tmpTransforms[j].localRotation,
                                                                                        scale.HasValue ? scale.Value : tmpTransforms[j].localScale);
                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                #region Animations
                                var animationsDic = new Dictionary<AnimationClip, Grendgine_Collada_Animation>();
                                {
                                    var la = gCollada.Library_Animations = new Grendgine_Collada_Library_Animations()
                                    {
                                        ID = "Animations_" + MakeID(rootObject),
                                        Name = "Animations_" + rootObject.name,
                                    };
                                    {
                                        List<Grendgine_Collada_Animation> animations = new List<Grendgine_Collada_Animation>();
                                        for (int j = 0; j < tmpTransforms.Length; j++)
                                        {
                                            if (!enableTransforms[j])
                                                continue;
                                            if (makeJoint)
                                            {
                                                if (!jointsDic.ContainsKey(transforms[j]))
                                                    continue;
                                                if (jointsDic[transforms[j]].Type != Grendgine_Collada_Node_Type.JOINT)
                                                    continue;
                                            }
                                            else
                                            {
                                                if (!nodesDic.ContainsKey(transforms[j]))
                                                    continue;
                                            }

                                            #region InputSource
                                            Grendgine_Collada_Source InputSource;
                                            {
                                                var Input_Float_Array = new Grendgine_Collada_Float_Array()
                                                {
                                                    ID = "Input_Float_Array_" + MakeID(transforms[j]),
                                                    Count = frameTimes.Length,
                                                };
                                                {
                                                    var sb = new StringBuilder();
                                                    for (int i = 0; i < frameTimes.Length; i++)
                                                    {
                                                        sb.AppendFormat("\n{0}", frameTimes[i]);
                                                    }
                                                    Input_Float_Array.Value_As_String = sb.ToString();
                                                }
                                                InputSource = new Grendgine_Collada_Source()
                                                {
                                                    ID = "InputSource_" + MakeID(transforms[j]),
                                                    Float_Array = Input_Float_Array,
                                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                                    {
                                                        Accessor = new Grendgine_Collada_Accessor()
                                                        {
                                                            Count = (uint)Input_Float_Array.Count,
                                                            Source = "#" + Input_Float_Array.ID,
                                                            Param = new Grendgine_Collada_Param[]
                                                            {
                                                                new Grendgine_Collada_Param()
                                                                {
                                                                    Name = "TIME",
                                                                    Type = "float",
                                                                },
                                                            },
                                                        },
                                                    },
                                                };
                                            }
                                            #endregion
                                            #region OutputSource
                                            Grendgine_Collada_Source OutputSource;
                                            {
                                                var Output_Float_Array = new Grendgine_Collada_Float_Array()
                                                {
                                                    ID = "Output_Float_Array_" + MakeID(transforms[j]),
                                                    Count = frameTimes.Length * 16,
                                                };
                                                {
                                                    var sb = new StringBuilder();
                                                    for (int i = 0; i < frameTimes.Length; i++)
                                                    {
                                                        Matrix4x4 mat = tmpTransformMatrixs[j, i];
                                                        {
                                                            var position = mat.GetColumn(3);
                                                            var rotation = (mat.GetColumn(2).sqrMagnitude > 0f && mat.GetColumn(1).sqrMagnitude > 0f) ? Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1)) : Quaternion.identity;
                                                            var scale = new Vector3(mat.GetColumn(0).magnitude, mat.GetColumn(1).magnitude, mat.GetColumn(2).magnitude);
                                                            #region Do not allow scale zero
                                                            {
                                                                for (int si = 0; si < 3; si++)
                                                                {
                                                                    if (scale[si] == 0f)
                                                                        scale[si] = Mathf.Epsilon;
                                                                }
                                                            }
                                                            #endregion
                                                            mat = Matrix4x4.TRS(matMirrorX.MultiplyPoint3x4(position),
                                                                                new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w), //mirrorX
                                                                                scale);
                                                        }
                                                        for (int r = 0; r < 4; r++)
                                                            sb.AppendFormat("\n{0} {1} {2} {3}", mat[r, 0], mat[r, 1], mat[r, 2], mat[r, 3]);
                                                    }
                                                    Output_Float_Array.Value_As_String = sb.ToString();
                                                }
                                                OutputSource = new Grendgine_Collada_Source()
                                                {
                                                    ID = "OutputSource_" + MakeID(transforms[j]),
                                                    Float_Array = Output_Float_Array,
                                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                                    {
                                                        Accessor = new Grendgine_Collada_Accessor()
                                                        {
                                                            Count = (uint)(Output_Float_Array.Count / 16),
                                                            Source = "#" + Output_Float_Array.ID,
                                                            Stride = 16,
                                                            Param = new Grendgine_Collada_Param[]
                                                            {
                                                                new Grendgine_Collada_Param()
                                                                {
                                                                    Name = "TRANSFORM",
                                                                    Type = "float4x4",
                                                                },
                                                            }
                                                        },
                                                    },
                                                };
                                            }
                                            #endregion
                                            #region InterpolationSource
                                            Grendgine_Collada_Source InterpolationSource;
                                            {
                                                var Interpolation_Name_Array = new Grendgine_Collada_Name_Array()
                                                {
                                                    ID = "Interpolation_Name_Array" + MakeID(transforms[j]),
                                                    Count = frameTimes.Length,
                                                };
                                                {
                                                    var sb = new StringBuilder();
                                                    for (int i = 0; i < frameTimes.Length; i++)
                                                    {
                                                        sb.AppendFormat("\n{0}", "LINEAR");
                                                    }
                                                    Interpolation_Name_Array.Value_Pre_Parse = sb.ToString();
                                                }
                                                InterpolationSource = new Grendgine_Collada_Source()
                                                {
                                                    ID = "InterpolationSource_" + MakeID(transforms[j]),
                                                    Name_Array = Interpolation_Name_Array,
                                                    Technique_Common = new Grendgine_Collada_Technique_Common_Source()
                                                    {
                                                        Accessor = new Grendgine_Collada_Accessor()
                                                        {
                                                            Count = (uint)Interpolation_Name_Array.Count,
                                                            Source = "#" + Interpolation_Name_Array.ID,
                                                            Param = new Grendgine_Collada_Param[]
                                                            {
                                                                new Grendgine_Collada_Param()
                                                                {
                                                                    Name = "INTERPOLATION",
                                                                    Type = "name",
                                                                },
                                                            },
                                                        },
                                                    },
                                                };
                                            }
                                            #endregion
                                            #region Sampler
                                            var Sampler = new Grendgine_Collada_Sampler[]
                                            {
                                                new Grendgine_Collada_Sampler()
                                                {
                                                    ID = "Sampler_" + MakeID(transforms[j]),
                                                    Input = new Grendgine_Collada_Input_Unshared[]
                                                    {
                                                        new Grendgine_Collada_Input_Unshared()
                                                        {
                                                            Semantic = Grendgine_Collada_Input_Semantic.INPUT,
                                                            source = "#" + InputSource.ID,
                                                        },
                                                        new Grendgine_Collada_Input_Unshared()
                                                        {
                                                            Semantic = Grendgine_Collada_Input_Semantic.OUTPUT,
                                                            source = "#" + OutputSource.ID,
                                                        },
                                                        new Grendgine_Collada_Input_Unshared()
                                                        {
                                                            Semantic = Grendgine_Collada_Input_Semantic.INTERPOLATION,
                                                            source = "#" + InterpolationSource.ID,
                                                        },
                                                    },
                                                },
                                            };
                                            #endregion
                                            var a = new Grendgine_Collada_Animation()
                                            {
                                                ID = "Animation_" + MakeID(transforms[j]),
                                                Source = new Grendgine_Collada_Source[]
                                                {
                                                    InputSource,
                                                    OutputSource,
                                                    InterpolationSource,
                                                },
                                                Sampler = Sampler,
                                                Channel = new Grendgine_Collada_Channel[]
                                                {
                                                    new Grendgine_Collada_Channel()
                                                    {
                                                        Source = "#" + Sampler[0].ID,
                                                        Target = (makeJoint ? jointsDic[transforms[j]].sID : nodesDic[transforms[j]].sID) + "/transform",
                                                    },
                                                },
                                            };
                                            animations.Add(a);
                                        }
                                        var ra = new Grendgine_Collada_Animation()
                                        {
                                            ID = "Animation_" + MakeID(tmpClip),
                                            Name = "Animation_" + tmpClip.name,
                                            Animation = animations.ToArray(),
                                        };
                                        animationsDic.Add(tmpClip, ra);
                                    }
                                    la.Animation = animationsDic.Values.ToArray();
                                }
                                #endregion

                                #region AnimationClips
                                var animationClipsDic = new Dictionary<AnimationClip, Grendgine_Collada_Animation_Clip>();
                                {
                                    var la = gCollada.Library_Animation_Clips = new Grendgine_Collada_Library_Animation_Clips()
                                    {
                                        ID = "Animation_Clips_" + MakeID(rootObject),
                                        Name = "Animation_Clips_" + rootObject.name,
                                    };
                                    {
                                        var ac = new Grendgine_Collada_Animation_Clip()
                                        {
                                            ID = "Animation_Clips_" + MakeID(tmpClip),
                                            Name = tmpClip.name,
                                            Start = 0f,
                                            End = totalTime,
                                            Instance_Animation = new Grendgine_Collada_Instance_Animation[1]
                                            {
                                                new Grendgine_Collada_Instance_Animation()
                                                {
                                                    URL = "#" + animationsDic[tmpClip].ID,
                                                },
                                            },
                                        };
                                        animationClipsDic.Add(tmpClip, ac);
                                    }
                                    la.Animation_Clip = animationClipsDic.Values.ToArray();
                                }
                                #endregion

                                #region Scene
                                {
                                    var Doc = new System.Xml.XmlDocument();
                                    var frame_rate = Doc.CreateElement("frame_rate");
                                    {
                                        frame_rate.InnerText = tmpClip.frameRate.ToString();
                                    }
                                    var start_time = Doc.CreateElement("start_time");
                                    {
                                        start_time.InnerText = 0f.ToString();
                                    }
                                    var end_time = Doc.CreateElement("end_time");
                                    {
                                        end_time.InnerText = totalTime.ToString();
                                    }
                                    gCollada.Library_Visual_Scene.Visual_Scene[0].Extra = new Grendgine_Collada_Extra[]
                                    {
                                        new Grendgine_Collada_Extra()
                                        {
                                            Technique = new Grendgine_Collada_Technique[]
                                            {
                                                new Grendgine_Collada_Technique()
                                                {
                                                    profile = "MAX3D",
                                                    Data = new System.Xml.XmlElement[]
                                                    {
                                                        frame_rate,
                                                    },
                                                },
                                                new Grendgine_Collada_Technique()
                                                {
                                                    profile = "FCOLLADA",
                                                    Data = new System.Xml.XmlElement[]
                                                    {
                                                        start_time,
                                                        end_time,
                                                    },
                                                },
                                            },
                                        },
                                    };
                                }
                                #endregion

                                using (var writer = new StreamWriter(pathAnim))
                                {
                                    var xmlSerializer = new XmlSerializer(typeof(Grendgine_Collada));
                                    xmlSerializer.Serialize(writer, gCollada);
                                }
                                exportedFiles.Add(pathAnim);
                            }
                        }

                        AnimationClip.DestroyImmediate(tmpClip);
                        tmpClip = null;
                    }
                    finally
                    {
                        GameObject.DestroyImmediate(tmpObject);
                        if (tmpAvatar != null)
                            Avatar.DestroyImmediate(tmpAvatar);
                        if (tmpClip != null)
                            AnimationClip.DestroyImmediate(tmpClip);
                    }
                }
                #endregion
            }
            finally
            {
                #region TransformSave
                for (int i = 0; i < transforms.Count; i++)
                {
                    transformSaves[i].Load(transforms[i]);
                }
                #endregion

                EditorUtility.ClearProgressBar();
            }
            return true;
        }

        #region Settings
        public bool settings_exportMesh = true;
        public bool settings_iKOnFeet = true;
        #endregion

        public List<string> exportedFiles = new List<string>();

        private class TransformSave
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;

            public TransformSave(Transform t)
            {
                localPosition = t.localPosition;
                localRotation = t.localRotation;
                localScale = t.localScale;
            }
            public void Load(Transform t)
            {
                t.localPosition = localPosition;
                t.localRotation = localRotation;
                t.localScale = localScale;
            }
        }
        private class TransformCurves
        {
            public AnimationCurve[] position;
            public AnimationCurve[] rotation;
            public AnimationCurve[] scale;

            public Vector3? GetPosition(float time)
            {
                Vector3 result = Vector3.zero;
                int count = 0;
                for (int i = 0; i < position.Length; i++)
                {
                    if (position[i] == null) continue;
                    result[i] = position[i].Evaluate(time);
                    count++;
                }
                if (count == 3) return result;
                else return null;
            }
            public Quaternion? GetRotation(float time)
            {
                if (rotation.Length == 3)
                {
                    Vector3 result = Vector3.zero;
                    int count = 0;
                    for (int i = 0; i < rotation.Length; i++)
                    {
                        if (rotation[i] == null) continue;
                        result[i] = rotation[i].Evaluate(time);
                        count++;
                    }
                    if (count == 3) return Quaternion.Euler(result);
                    else return null;
                }
                else
                {
                    Vector4 result = new Vector4(0, 0, 0, 1);
                    int count = 0;
                    for (int i = 0; i < rotation.Length; i++)
                    {
                        if (rotation[i] == null) continue;
                        result[i] = rotation[i].Evaluate(time);
                        count++;
                    }
                    if (count == 4 && result.sqrMagnitude > 0)
                    {
                        result.Normalize();
                        return new Quaternion(result[0], result[1], result[2], result[3]);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            public Vector3? GetScale(float time)
            {
                Vector3 result = Vector3.one;
                int count = 0;
                for (int i = 0; i < scale.Length; i++)
                {
                    if (scale[i] == null) continue;
                    result[i] = scale[i].Evaluate(time);
                    count++;
                }
                if (count == 3) return result;
                else return null;
            }
        }
    }
}
