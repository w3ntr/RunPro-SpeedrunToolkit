using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace SpeedrunToolkitMod
{
    public class DeathZoneVisualizerModule
    {
        public bool IsVisualsOn = true;
        public bool XRay = true;
        public bool WireframeMode = false;
        public float Transparency = 0.5f;
        public float ColorR = 1.0f;
        public float ColorG = 0.0f;
        public float ColorB = 0.0f;

        private Material vizMaterial;
        private List<GameObject> overlays = new List<GameObject>();
        private Mesh wireMesh;

        public void Init()
        {
            CreateMaterial();
        }

        private void CreateMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("GUI/Text Shader");

            vizMaterial = new Material(shader);
            UpdateMaterialProperties();
        }

        public void UpdateMaterialProperties()
        {
            if (vizMaterial == null) return;

            vizMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            vizMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            vizMaterial.SetInt("_Cull", (int)CullMode.Off);
            vizMaterial.SetInt("_ZWrite", 0);

            vizMaterial.SetInt("_ZTest", XRay ? (int)CompareFunction.Always : (int)CompareFunction.LessEqual);
            vizMaterial.renderQueue = XRay ? 5000 : 3000;

            vizMaterial.color = new Color(ColorR, ColorG, ColorB, Transparency);
        }

        public void OnSceneWasLoaded(string sceneName)
        {
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            ClearOverlays();
            if (IsVisualsOn) RenderDeathZones();
        }

        private void ClearOverlays()
        {
            foreach (GameObject obj in overlays)
            {
                if (obj != null) Object.Destroy(obj);
            }
            overlays.Clear();
        }

        private void RenderDeathZones()
        {
            if (vizMaterial == null) CreateMaterial();

            Collider[] allColliders = Object.FindObjectsOfType<Collider>();

            foreach (Collider col in allColliders)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;

                if (IsDeathZone(col))
                {
                    CreateOverlay(col);
                }
            }
        }

        private bool IsDeathZone(Collider col)
        {
            GameObject go = col.gameObject;
            string name = go.name.ToLower();
            string tag = go.tag.ToLower();
            string layer = LayerMask.LayerToName(go.layer).ToLower();

            if (name.Contains("player") || name.Contains("start") || name.Contains("finish") ||
                name.Contains("checkpoint") || name.Contains("spawn") || name.Contains("camera") ||
                name.Contains("portal") || name.Contains("teleport"))
                return false;

            if (name.Contains("death") || name.Contains("kill") || name.Contains("dead") ||
                name.Contains("hazard") || name.Contains("lava") || name.Contains("void") ||
                name.Contains("fall") || name.Contains("trigger") || name.Contains("out") ||
                tag.Contains("death") || tag.Contains("kill") || tag.Contains("hazard") ||
                layer.Contains("death") || layer.Contains("hazard"))
            {
                return true;
            }

            foreach (var script in go.GetComponents<MonoBehaviour>())
            {
                if (script == null) continue;
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("death") || scriptName.Contains("kill") ||
                    scriptName.Contains("hazard") || scriptName.Contains("fall") || scriptName.Contains("damage"))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateOverlay(Collider col)
        {
            GameObject overlayObj;

            if (WireframeMode)
            {
                overlayObj = new GameObject("[VizWire]");
                MeshFilter mf = overlayObj.AddComponent<MeshFilter>();
                mf.sharedMesh = GetWireframeMesh();
                MeshRenderer mr = overlayObj.AddComponent<MeshRenderer>();
                mr.material = vizMaterial;
            }
            else
            {
                overlayObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(overlayObj.GetComponent<Collider>());
                Renderer ren = overlayObj.GetComponent<Renderer>();
                if (ren != null) ren.material = vizMaterial;
            }

            Bounds bounds = col.bounds;
            overlayObj.transform.position = bounds.center;
            overlayObj.transform.localScale = bounds.size + new Vector3(0.01f, 0.01f, 0.01f);

            overlays.Add(overlayObj);
        }

        private Mesh GetWireframeMesh()
        {
            if (wireMesh != null) return wireMesh;

            wireMesh = new Mesh();
            Vector3[] verts = new Vector3[8] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f)
            };
            int[] lines = new int[24] {
                0,1, 1,2, 2,3, 3,0,
                4,5, 5,6, 6,7, 7,4,
                0,4, 1,5, 2,6, 3,7
            };
            wireMesh.vertices = verts;
            wireMesh.SetIndices(lines, MeshTopology.Lines, 0);
            return wireMesh;
        }
    }
}