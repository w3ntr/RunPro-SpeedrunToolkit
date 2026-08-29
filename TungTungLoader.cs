using MelonLoader;
using MelonLoader.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public static class TungTungLoader
    {
        public static GameObject tungObject;
        public static Transform tungTransform;
        private static Vector3 lastPos;
        private static float stepTimer;

        // Базовое смещение, чтобы палка и модель не выпирали в камеру от 1-го лица
        private static readonly Vector3 baseOffset = new Vector3(0f, -1.15f, -0.3f);

        public static GameObject SpawnTungTung(Transform parentTransform)
        {
            if (tungObject != null)
            {
                tungObject.SetActive(true);
                return tungObject;
            }

            string folder = Path.Combine(MelonEnvironment.UserDataDirectory, "SpeedrunToolkit");
            string objPath = Path.Combine(folder, "tungtung.obj");

            string imgPath = Path.Combine(folder, "tungtung.png");
            if (!File.Exists(imgPath)) imgPath = Path.Combine(folder, "tungtung.jpg");

            if (!File.Exists(objPath)) return null;

            Mesh mesh = LoadObjMesh(objPath);
            if (mesh == null) return null;

            tungObject = new GameObject("TungTungModel");
            tungObject.transform.SetParent(parentTransform, false);
            tungObject.transform.localPosition = baseOffset;
            tungObject.transform.localRotation = Quaternion.identity;
            tungObject.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            MeshFilter mf = tungObject.AddComponent<MeshFilter>();
            MeshRenderer mr = tungObject.AddComponent<MeshRenderer>();
            mf.mesh = mesh;

            Material mat = new Material(Shader.Find("Standard"));
            if (File.Exists(imgPath))
            {
                byte[] bytes = File.ReadAllBytes(imgPath);
                Texture2D tex = new Texture2D(2, 2);
                ImageConversion.LoadImage(tex, bytes);
                mat.mainTexture = tex;
            }
            mr.material = mat;

            tungTransform = tungObject.transform;
            if (parentTransform != null) lastPos = parentTransform.position;

            return tungObject;
        }

        public static void SetActive(bool active)
        {
            if (tungObject != null)
            {
                tungObject.SetActive(active);
            }
        }

        public static void UpdateAnimation(Transform playerTransform)
        {
            if (tungTransform == null || playerTransform == null || !tungObject.activeSelf) return;

            float speed = (playerTransform.position - lastPos).magnitude / Time.deltaTime;
            lastPos = playerTransform.position;

            if (speed > 0.5f)
            {
                stepTimer += Time.deltaTime * 14f;
                float bounce = Mathf.Abs(Mathf.Sin(stepTimer)) * 0.12f;
                float tilt = Mathf.Sin(stepTimer) * 8f;

                tungTransform.localPosition = baseOffset + new Vector3(0f, bounce, 0f);
                tungTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }
            else
            {
                float idle = Mathf.Sin(Time.time * 2.5f) * 0.02f;
                tungTransform.localPosition = Vector3.Lerp(tungTransform.localPosition, baseOffset + new Vector3(0f, idle, 0f), Time.deltaTime * 6f);
                tungTransform.localRotation = Quaternion.Lerp(tungTransform.localRotation, Quaternion.identity, Time.deltaTime * 6f);
            }
        }

        private static Mesh LoadObjMesh(string filePath)
        {
            List<Vector3> rawVerts = new List<Vector3>();
            List<Vector2> rawUVs = new List<Vector2>();
            List<Vector3> finalVerts = new List<Vector3>();
            List<Vector2> finalUVs = new List<Vector2>();
            List<int> triangles = new List<int>();
            Dictionary<string, int> indexMap = new Dictionary<string, int>();

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string l = line.Trim();
                if (l.StartsWith("v "))
                {
                    string[] p = l.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4) rawVerts.Add(new Vector3(-float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3])));
                }
                else if (l.StartsWith("vt "))
                {
                    string[] p = l.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 3) rawUVs.Add(new Vector2(float.Parse(p[1]), 1.0f - float.Parse(p[2])));
                }
                else if (l.StartsWith("f "))
                {
                    string[] p = l.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4)
                    {
                        int[] faceIndices = new int[3];
                        for (int i = 0; i < 3; i++)
                        {
                            string[] idx = p[i + 1].Split('/');
                            int vIdx = int.Parse(idx[0]) - 1;
                            int vtIdx = (idx.Length > 1 && !string.IsNullOrEmpty(idx[1])) ? int.Parse(idx[1]) - 1 : -1;

                            string key = $"{vIdx}/{vtIdx}";
                            if (!indexMap.ContainsKey(key))
                            {
                                indexMap[key] = finalVerts.Count;
                                finalVerts.Add(rawVerts[vIdx]);
                                finalUVs.Add((vtIdx >= 0 && vtIdx < rawUVs.Count) ? rawUVs[vtIdx] : Vector2.zero);
                            }
                            faceIndices[i] = indexMap[key];
                        }
                        triangles.Add(faceIndices[0]);
                        triangles.Add(faceIndices[2]);
                        triangles.Add(faceIndices[1]);
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = finalVerts.ToArray();
            mesh.uv = finalUVs.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}