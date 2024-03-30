using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ProceduralNoiseProject;
using Common.Unity.Drawing;
using WARP.Terraform.API;

namespace MarchingCubesProject
{
    public enum MARCHING_MODE {  CUBES, TETRAHEDRON };

    public struct PointData : IVertexData
    {
        /// <inheritdoc />
        public Vector3 Position { get; set; }

        /// <inheritdoc />
        public float Density { get; set; }

        public Color Color { get; set; }
    }

    [ExecuteInEditMode]
    public class Example : MonoBehaviour
    {
        public Material material;

        public MARCHING_MODE mode = MARCHING_MODE.CUBES;

        public int seed = 0;

        public bool smoothNormals = false;

        public bool drawNormals = false;

        private List<GameObject> meshes = new List<GameObject>();

        private NormalRenderer normalRenderer;

        void Start()
        {

            INoise perlin = new PerlinNoise(seed, 1.0f);
            FractalNoise fractal = new FractalNoise(perlin, 3, 1.0f);

            //Set the mode used to create the mesh.
            //Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface.
            //var marching = new MarchingCubes<PointData>();
            var marching = new MarchingTertrahedron();

            //The size of voxel array.
            int width = 16;
            int height = 16;
            int depth = 16;

            var voxels = new VoxelArray(width, height, depth);

            //Fill voxels with values. Im using perlin noise but any method to create voxels will work.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        float u = x / (width - 1.0f);
                        float v = y / (height - 1.0f);
                        float w = z / (depth - 1.0f);

                        voxels[x, y, z] = new TerraformPoint()
                        {
                            Density = fractal.Sample3D(u, v, w),
                            Color   = new Color(Mathf.Clamp(1.0f / depth * z, 0, 1), 0, 0, 1)
                        };
                    }
                }
            }

            List<TerraformPoint> points = new();
            List<Vector3> normals = new List<Vector3>();
            List<int> indices = new List<int>();

            //The mesh produced is not optimal. There is one vert for each index.
            //Would need to weld vertices for better quality mesh.
            //marching.Generate(voxels, points, indices);

            //Create the normals from the voxel.

            if (smoothNormals)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    //Presumes the vertex is in local space where
                    //the min value is 0 and max is width/height/depth.
                    Vector3 p = points[i].Position;

                    float u = p.x / (width - 1.0f);
                    float v = p.y / (height - 1.0f);
                    float w = p.z / (depth - 1.0f);

                    Vector3 n = voxels.GetNormal(u, v, w);

                    normals.Add(n);
                }

                normalRenderer = new NormalRenderer();
                normalRenderer.DefaultColor = Color.red;
                normalRenderer.Length = 0.25f;
                //normalRenderer.Load(points, normals);
            }

            var position = new Vector3(-width / 2, -height / 2, -depth / 2);

            CreateMesh32(points, normals, indices, position);

        }

        private void CreateMesh32(List<TerraformPoint> points, List<Vector3> normals, List<int> indices, Vector3 position)
        {
            List<Vector3> positions = new(points.Count);
            List<Color>   colors    = new(points.Count);

            foreach (TerraformPoint v in points)
            {
                positions.Add(v.Position);
                colors.Add(v.Color);
            }


            Mesh mesh = new();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetTriangles(indices, 0);
            mesh.SetColors(colors);

            if (normals.Count > 0)
                mesh.SetNormals(normals);
            else
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();
            mesh.OptimizeIndexBuffers();
            mesh.Optimize();

            GameObject go = new GameObject("Mesh");
            go.transform.parent = transform;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.GetComponent<Renderer>().material = material;
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.transform.localPosition = position;

            meshes.Add(go);
        }


        private void Update()
        {
            //transform.Rotate(Vector3.up, 10.0f * Time.deltaTime);
        }

        private void OnRenderObject()
        {
            if(normalRenderer != null && meshes.Count > 0 && drawNormals)
            {
                var m = meshes[0].transform.localToWorldMatrix;

                normalRenderer.LocalToWorld = m;
                normalRenderer.Draw();
            }
            
        }

    }

}
