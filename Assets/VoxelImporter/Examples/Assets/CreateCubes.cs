using UnityEngine;
using System.Collections;

namespace VoxelImporter
{
    public class CreateCubes : MonoBehaviour
    {
        public VoxelStructure voxelStructure;

        private Material[] materials;

        public void Awake()
        {
            if (voxelStructure == null) return;

            materials = new Material[voxelStructure.palettes.Length];
            for (int i = 0; i < voxelStructure.palettes.Length; i++)
            {
                materials[i] = new Material(Shader.Find("Standard"));
                materials[i].name = string.Format("Palette {0}", i);
                materials[i].color = voxelStructure.palettes[i];
            }

            for (int i = 0; i < voxelStructure.voxels.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = string.Format("{0} : ({1}, {2}, {3})", i, voxelStructure.voxels[i].x, voxelStructure.voxels[i].y, voxelStructure.voxels[i].z);
                go.transform.localPosition = voxelStructure.voxels[i].position;
                go.transform.SetParent(transform);
                {
                    var renderer = go.GetComponent<Renderer>();
                    renderer.sharedMaterial = materials[voxelStructure.voxels[i].palette];
                }
                {
                    var rigidbody = go.AddComponent<Rigidbody>();
                    rigidbody.isKinematic = true;
                    rigidbody.Sleep();
                }
            }

            enabled = false;
        }
    }
}
