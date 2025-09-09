using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Reflection;

public class ImportGLBbyURL : MonoBehaviour
{
    public string input_url = "";
    private bool _replaceExisting = false;
    // Debug support for regenerating mask map at runtime
    private Texture2D _lastOrmTexture;
    private Material _lastMaterial;

    [Serializable]
    private class GltfRoot
    {
        public Accessor[] accessors;
        public BufferView[] bufferViews;
        public Buffer[] buffers;
        public GltfImage[] images;
        public GltfTexture[] textures;
        public GltfMaterial[] materials;
        public GltfMesh[] meshes;
        public GltfNode[] nodes;
        public int scene;
        public GltfScene[] scenes;
    }

    [Serializable]
    private class Accessor
    {
        public int bufferView;
        public int byteOffset;
        public int componentType;
        public bool normalized;
        public int count;
        public string type;
    }

    [Serializable]
    private class BufferView
    {
        public int buffer;
        public int byteOffset;
        public int byteLength;
        public int byteStride;
    }

    [Serializable]
    private class Buffer
    {
        public int byteLength;
    }

    [Serializable]
    private class GltfImage
    {
        public string mimeType;
        public int bufferView;
        public string name;
    }

    [Serializable]
    private class GltfTexture
    {
        public int source;
    }

    [Serializable]
    private class TextureInfo
    {
        public int index;
        public int texCoord;
    }

    [Serializable]
    private class NormalTextureInfo
    {
        public int index;
        public float scale;
        public int texCoord;
    }

    [Serializable]
    private class PbrMetallicRoughness
    {
        public float[] baseColorFactor;
        public float metallicFactor = 1f;
        public float roughnessFactor = 1f;
        public TextureInfo baseColorTexture;
        public TextureInfo metallicRoughnessTexture;
    }

    [Serializable]
    private class GltfMaterial
    {
        public string name;
        public PbrMetallicRoughness pbrMetallicRoughness;
        public NormalTextureInfo normalTexture;
    }

    [Serializable]
    private class Attributes
    {
        public int POSITION;
        public int NORMAL;
        public int TEXCOORD_0;
    }

    [Serializable]
    private class MeshPrimitive
    {
        public Attributes attributes;
        public int indices;
        public int material;
        public int mode;
    }

    [Serializable]
    private class GltfMesh
    {
        public string name;
        public MeshPrimitive[] primitives;
    }

    [Serializable]
    private class GltfNode
    {
        public int mesh;
        public string name;
    }

    [Serializable]
    private class GltfScene
    {
        public int[] nodes;
    }

    private struct ParsedGlb
    {
        public GltfRoot gltf;
        public byte[] binChunk;
        public string json;
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(input_url))
        {
            Debug.LogError("ImportGLBbyURL: input_url is empty.");
            return;
        }
        _replaceExisting = true;
        StartCoroutine(DownloadAndImportGlb(input_url));
    }

    private IEnumerator DownloadAndImportGlb(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ImportGLBbyURL: Download failed -> " + req.error);
                yield break;
            }

            try
            {
                byte[] data = req.downloadHandler.data;
                ParsedGlb parsed = ParseGlb(data);
                if (_replaceExisting)
                {
                    ClearChildren();
                }
                BuildSceneFromGlb(parsed);
            }
            catch (Exception ex)
            {
                Debug.LogError("ImportGLBbyURL: Failed to process GLB -> " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

    public void ImportFromUrl(string url, bool replaceExisting = true)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("ImportGLBbyURL: ImportFromUrl called with empty url");
            return;
        }
        input_url = url;
        _replaceExisting = replaceExisting;
        StartCoroutine(DownloadAndImportGlb(input_url));
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private ParsedGlb ParseGlb(byte[] bytes)
    {
        if (bytes.Length < 12) throw new Exception("GLB too short");
        uint magic = BitConverter.ToUInt32(bytes, 0);
        uint version = BitConverter.ToUInt32(bytes, 4);
        uint length = BitConverter.ToUInt32(bytes, 8);
        if (magic != 0x46546C67) throw new Exception("Invalid GLB magic");
        if (version != 2) throw new Exception("Only GLB v2 is supported");
        if (length != bytes.Length)
        {
            if (length > bytes.Length) throw new Exception("GLB length mismatch");
        }

        int offset = 12;
        if (offset + 8 > bytes.Length) throw new Exception("Missing JSON chunk header");
        uint jsonLength = BitConverter.ToUInt32(bytes, offset + 0);
        uint jsonType = BitConverter.ToUInt32(bytes, offset + 4);
        offset += 8;
        if (jsonType != 0x4E4F534A) throw new Exception("First GLB chunk is not JSON");
        if (offset + (int)jsonLength > bytes.Length) throw new Exception("JSON chunk out of range");
        string json = Encoding.UTF8.GetString(bytes, offset, (int)jsonLength);
        offset += Align4((int)jsonLength);

        if (offset + 8 > bytes.Length) throw new Exception("Missing BIN chunk header");
        uint binLength = BitConverter.ToUInt32(bytes, offset + 0);
        uint binType = BitConverter.ToUInt32(bytes, offset + 4);
        offset += 8;
        if (binType != 0x004E4942) throw new Exception("Second GLB chunk is not BIN");
        if (offset + (int)binLength > bytes.Length) throw new Exception("BIN chunk out of range");
        byte[] bin = new byte[binLength];
        System.Buffer.BlockCopy(bytes, offset, bin, 0, (int)binLength);

        GltfRoot gltf = JsonUtility.FromJson<GltfRoot>(json);
        if (gltf == null) throw new Exception("Failed to parse glTF JSON");

        return new ParsedGlb { gltf = gltf, binChunk = bin, json = json };
    }

    private static int Align4(int value)
    {
        int mod = value & 3;
        return mod == 0 ? value : value + (4 - mod);
    }

    private void BuildSceneFromGlb(ParsedGlb parsed)
    {
        GltfRoot gltf = parsed.gltf;
        byte[] bin = parsed.binChunk;

        Dictionary<int, Texture2D> imageIndexToTexture = new Dictionary<int, Texture2D>();
        if (gltf.images != null)
        {
            for (int i = 0; i < gltf.images.Length; i++)
            {
                try
                {
                    Texture2D tex = LoadImageFromBufferView(bin, gltf, gltf.images[i]);
                    if (tex != null)
                    {
                        tex.name = string.IsNullOrEmpty(gltf.images[i].name) ? ($"image_{i}") : gltf.images[i].name;
                        imageIndexToTexture[i] = tex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ImportGLBbyURL: Failed to load image {i}: {ex.Message}");
                }
            }
        }

        if (gltf.scenes == null || gltf.scenes.Length == 0)
        {
            if (gltf.meshes == null || gltf.meshes.Length == 0) throw new Exception("No meshes in GLB");
            CreateMeshObject(bin, gltf, gltf.meshes[0], imageIndexToTexture);
        }
        else
        {
            int sceneIndex = gltf.scene;
            if (sceneIndex < 0 || sceneIndex >= gltf.scenes.Length) sceneIndex = 0;
            GltfScene scene = gltf.scenes[sceneIndex];
            if (scene.nodes != null)
            {
                foreach (int nodeIndex in scene.nodes)
                {
                    if (nodeIndex >= 0 && nodeIndex < gltf.nodes.Length)
                    {
                        GltfNode node = gltf.nodes[nodeIndex];
                        if (node.mesh >= 0 && node.mesh < gltf.meshes.Length)
                        {
                            CreateMeshObject(bin, gltf, gltf.meshes[node.mesh], imageIndexToTexture);
                        }
                    }
                }
            }
        }
    }

    private void CreateMeshObject(byte[] bin, GltfRoot gltf, GltfMesh gltfMesh, Dictionary<int, Texture2D> imageIndexToTexture)
    {
        if (gltfMesh.primitives == null || gltfMesh.primitives.Length == 0) throw new Exception("Mesh has no primitives");
        MeshPrimitive prim = gltfMesh.primitives[0];
        if (prim.mode != 4) Debug.LogWarning("ImportGLBbyURL: Primitive mode is not TRIANGLES; attempting anyway.");

        Vector3[] vertices = ReadVec3(bin, gltf, prim.attributes.POSITION);
        Vector3[] normals = prim.attributes.NORMAL >= 0 ? ReadVec3(bin, gltf, prim.attributes.NORMAL) : null;
        Vector2[] uvs = prim.attributes.TEXCOORD_0 >= 0 ? ReadVec2(bin, gltf, prim.attributes.TEXCOORD_0, true) : null;
        int[] indices = ReadIndices(bin, gltf, prim.indices);

        Mesh mesh = new Mesh();
        mesh.name = string.IsNullOrEmpty(gltfMesh.name) ? "glb_mesh" : gltfMesh.name;
        if (vertices.Length > 65535 || (indices != null && indices.Length > 65535))
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        mesh.vertices = vertices;
        if (uvs != null && uvs.Length == vertices.Length) mesh.uv = uvs;
        if (indices != null) mesh.triangles = indices;
        if (normals != null && normals.Length == vertices.Length) mesh.normals = normals; else mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        Material mat = new Material(Shader.Find("HDRP/Lit"));
        mat.name = (gltf.materials != null && prim.material >= 0 && prim.material < gltf.materials.Length && !string.IsNullOrEmpty(gltf.materials[prim.material].name))
            ? gltf.materials[prim.material].name
            : "glb_material";

        Texture2D baseColorTex = null;
        Texture2D ormTex = null;
        Texture2D normalTex = null;
        float normalScale = 1f;
        Color baseColor = Color.white;

        if (gltf.materials != null && prim.material >= 0 && prim.material < gltf.materials.Length)
        {
            GltfMaterial m = gltf.materials[prim.material];
            if (m.pbrMetallicRoughness != null)
            {
                if (m.pbrMetallicRoughness.baseColorFactor != null && m.pbrMetallicRoughness.baseColorFactor.Length >= 4)
                {
                    baseColor = new Color(
                        m.pbrMetallicRoughness.baseColorFactor[0],
                        m.pbrMetallicRoughness.baseColorFactor[1],
                        m.pbrMetallicRoughness.baseColorFactor[2],
                        m.pbrMetallicRoughness.baseColorFactor[3]
                    );
                }
                if (m.pbrMetallicRoughness.baseColorTexture != null)
                {
                    int texIdx = m.pbrMetallicRoughness.baseColorTexture.index;
                    if (IsValidTextureIndex(texIdx, gltf))
                    {
                        int imageIdx = gltf.textures[texIdx].source;
                        baseColorTex = GetTextureByImageIndex(imageIndexToTexture, imageIdx);
                    }
                }
                if (m.pbrMetallicRoughness.metallicRoughnessTexture != null)
                {
                    int texIdx = m.pbrMetallicRoughness.metallicRoughnessTexture.index;
                    if (IsValidTextureIndex(texIdx, gltf))
                    {
                        int imageIdx = gltf.textures[texIdx].source;
                        ormTex = GetTextureByImageIndex(imageIndexToTexture, imageIdx);
                    }
                }
            }
            if (m.normalTexture != null)
            {
                normalScale = m.normalTexture.scale <= 0f ? 1f : m.normalTexture.scale;
                int texIdx = m.normalTexture.index;
                if (IsValidTextureIndex(texIdx, gltf))
                {
                    int imageIdx = gltf.textures[texIdx].source;
                    normalTex = GetTextureByImageIndex(imageIndexToTexture, imageIdx);
                }
            }
        }

        mat.SetColor("_BaseColor", baseColor);
        if (baseColorTex != null)
        {
            mat.SetTexture("_BaseColorMap", baseColorTex);
        }
        if (normalTex != null)
        {
            mat.SetTexture("_NormalMap", normalTex);
            mat.SetFloat("_NormalScale", normalScale);
        }
        if (ormTex != null)
        {
            _lastOrmTexture = ormTex;
            _lastMaterial = mat;
            Texture2D maskMap = CreateHDRPMaskMapFromORMWithPreset(ormTex, 1);
            mat.SetTexture("_MaskMap", maskMap);
            ApplyHdrpLitKeywordsAndRemap(mat, maskMap, normalTex != null);
        }
        else
        {
            ApplyHdrpLitKeywordsAndRemap(mat, null, normalTex != null);
        }

        GameObject child = new GameObject(string.IsNullOrEmpty(gltfMesh.name) ? "GLB Object" : gltfMesh.name);
        child.transform.SetParent(this.transform, false);
        var mf = child.AddComponent<MeshFilter>();
        var mr = child.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = mat;
    }

    private static bool IsValidTextureIndex(int textureIndex, GltfRoot gltf)
    {
        return gltf.textures != null && textureIndex >= 0 && textureIndex < gltf.textures.Length && gltf.textures[textureIndex] != null;
    }

    private static Texture2D GetTextureByImageIndex(Dictionary<int, Texture2D> map, int imageIndex)
    {
        if (map != null && map.TryGetValue(imageIndex, out var tex)) return tex;
        return null;
    }

    private Texture2D LoadImageFromBufferView(byte[] bin, GltfRoot gltf, GltfImage image)
    {
        if (image == null) return null;
        if (image.bufferView < 0 || image.bufferView >= gltf.bufferViews.Length) return null;
        BufferView bv = gltf.bufferViews[image.bufferView];
        if (bv.buffer != 0) throw new Exception("Only single BIN buffer (index 0) is supported in GLB");
        int start = bv.byteOffset;
        int length = bv.byteLength;
        if (start < 0 || start + length > bin.Length) throw new Exception("Image bufferView out of range");
        byte[] imgBytes = new byte[length];
        System.Buffer.BlockCopy(bin, start, imgBytes, 0, length);

        // glTF color textures are sRGB; data maps (normal/ORM) should be linear.
        bool isDataMap = false;
        if (!string.IsNullOrEmpty(image.name))
        {
            string n = image.name.ToLowerInvariant();
            if (n.Contains("normal") || n.Contains("orm") || n.Contains("mask") || n.Contains("rough") || n.Contains("metal") || n.Contains("ao"))
            {
                isDataMap = true;
            }
        }
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, isDataMap);
        bool ok = ImageConversion.LoadImage(tex, imgBytes, false);
        if (!ok)
        {
            UnityEngine.Object.Destroy(tex);
            throw new Exception("LoadImage failed");
        }
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    public void RegenerateMaskMap(int preset)
    {
        if (_lastMaterial == null || _lastOrmTexture == null)
        {
            Debug.LogWarning("ImportGLBbyURL: No previous ORM/material to regenerate mask from.");
            return;
        }
        Texture2D mask = CreateHDRPMaskMapFromORMWithPreset(_lastOrmTexture, preset);
        _lastMaterial.SetTexture("_MaskMap", mask);
        ApplyHdrpLitKeywordsAndRemap(_lastMaterial, mask, _lastMaterial.GetTexture("_NormalMap") != null);
        Debug.Log($"ImportGLBbyURL: Regenerated MaskMap using preset {preset}.");
    }

    private Texture2D CreateHDRPMaskMapFromORMWithPreset(Texture2D orm, int preset)
    {
        int w = orm.width;
        int h = orm.height;
        // HDRP MaskMap expects linear (non-sRGB) sampling
        Texture2D mask = new Texture2D(w, h, TextureFormat.RGBA32, true, true);
        Color32[] src = orm.GetPixels32();
        Color32[] dst = new Color32[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            // Default assume glTF ORM: R=AO, G=Roughness, B=Metallic
            byte ao = src[i].r;
            byte rough = src[i].g;
            byte met = src[i].b;

            byte outR = 0, outG = 0, outB = 255, outA = 0;

            switch (preset)
            {
                default:
                case 1:
                    // Correct per Unity HDRP docs: R=Metallic, G=AO, B=Detail(255), A=Smoothness(1-Rough)
                    outR = met;
                    outG = ao;
                    outB = 255; // neutral detail mask
                    outA = (byte)(255 - rough);
                    break;
                case 2:
                    // Same as preset 1 but Detail=0 to visualize B channel influence
                    outR = met;
                    outG = ao;
                    outB = 0;
                    outA = (byte)(255 - rough);
                    break;
                case 3:
                    // Variant where exporter swapped Metallic/Roughness (R=AO, G=Metallic, B=Roughness)
                    // Interpret accordingly
                    met = src[i].g;
                    rough = src[i].b;
                    outR = met;
                    outG = ao;
                    outB = 255;
                    outA = (byte)(255 - rough);
                    break;
            }

            dst[i] = new Color32(outR, outG, outB, outA);
        }
        mask.SetPixels32(dst);
        mask.Apply(true, false);
        mask.wrapMode = TextureWrapMode.Repeat;
        mask.filterMode = FilterMode.Bilinear;
        mask.name = orm.name + "_MaskMap_P" + preset;
        return mask;
    }

    private void ApplyHdrpLitKeywordsAndRemap(Material mat, Texture maskMap, bool hasNormal)
    {
        if (mat == null) return;

        // Set sensible defaults for remap sliders to force material update
        if (mat.HasProperty("_MetallicRemapMin")) mat.SetFloat("_MetallicRemapMin", 0f);
        if (mat.HasProperty("_MetallicRemapMax")) mat.SetFloat("_MetallicRemapMax", 1f);
        if (mat.HasProperty("_AORemapMin")) mat.SetFloat("_AORemapMin", 0f);
        if (mat.HasProperty("_AORemapMax")) mat.SetFloat("_AORemapMax", 1f);
        if (mat.HasProperty("_SmoothnessRemapMin")) mat.SetFloat("_SmoothnessRemapMin", 0f);
        if (mat.HasProperty("_SmoothnessRemapMax")) mat.SetFloat("_SmoothnessRemapMax", 1f);

        // Toggle keywords commonly used in HDRP/Lit to ensure correct code path
        TrySetKeyword(mat, "_MASKMAP", maskMap != null);
        TrySetKeyword(mat, "_NORMALMAP", hasNormal);
        TrySetKeyword(mat, "_NORMALMAP_TANGENT_SPACE", hasNormal);

        // Try to call HDRP's internal validation to sync all keywords/passes
        try
        {
            var hdMatType = System.Type.GetType("UnityEngine.Rendering.HighDefinition.HDMaterial, Unity.RenderPipelines.HighDefinition.Runtime");
            if (hdMatType != null)
            {
                MethodInfo validate = hdMatType.GetMethod("ValidateMaterial", BindingFlags.Public | BindingFlags.Static);
                if (validate != null)
                {
                    validate.Invoke(null, new object[] { mat });
                }
            }
        }
        catch { /* best-effort */ }
    }

    private static void TrySetKeyword(Material mat, string keyword, bool enabled)
    {
        if (string.IsNullOrEmpty(keyword)) return;
        if (enabled) mat.EnableKeyword(keyword); else mat.DisableKeyword(keyword);
    }

    private Vector3[] ReadVec3(byte[] bin, GltfRoot gltf, int accessorIndex)
    {
        Accessor a = gltf.accessors[accessorIndex];
        if (a.componentType != 5126) throw new Exception("POSITION/NORMAL accessor must be FLOAT (5126)");
        BufferView bv = gltf.bufferViews[a.bufferView];
        if (bv.buffer != 0) throw new Exception("Only single BIN buffer (index 0) is supported in GLB");
        int stride = bv.byteStride != 0 ? bv.byteStride : (3 * sizeof(float));
        int start = bv.byteOffset + a.byteOffset;
        Vector3[] arr = new Vector3[a.count];
        for (int i = 0; i < a.count; i++)
        {
            int off = start + i * stride;
            float x = BitConverter.ToSingle(bin, off + 0);
            float y = BitConverter.ToSingle(bin, off + 4);
            float z = BitConverter.ToSingle(bin, off + 8);
            arr[i] = new Vector3(x, y, z);
        }
        return arr;
    }

    private Vector2[] ReadVec2(byte[] bin, GltfRoot gltf, int accessorIndex, bool invertV)
    {
        Accessor a = gltf.accessors[accessorIndex];
        if (a.componentType != 5126) throw new Exception("TEXCOORD accessor must be FLOAT (5126)");
        BufferView bv = gltf.bufferViews[a.bufferView];
        if (bv.buffer != 0) throw new Exception("Only single BIN buffer (index 0) is supported in GLB");
        int stride = bv.byteStride != 0 ? bv.byteStride : (2 * sizeof(float));
        int start = bv.byteOffset + a.byteOffset;
        Vector2[] arr = new Vector2[a.count];
        for (int i = 0; i < a.count; i++)
        {
            int off = start + i * stride;
            float u = BitConverter.ToSingle(bin, off + 0);
            float v = BitConverter.ToSingle(bin, off + 4);
            if (invertV) v = 1f - v;
            arr[i] = new Vector2(u, v);
        }
        return arr;
    }

    private int[] ReadIndices(byte[] bin, GltfRoot gltf, int accessorIndex)
    {
        Accessor a = gltf.accessors[accessorIndex];
        BufferView bv = gltf.bufferViews[a.bufferView];
        if (bv.buffer != 0) throw new Exception("Only single BIN buffer (index 0) is supported in GLB");
        int start = bv.byteOffset + a.byteOffset;
        int[] indices = new int[a.count];

        if (a.componentType == 5123)
        {
            int stride = bv.byteStride != 0 ? bv.byteStride : sizeof(ushort);
            for (int i = 0; i < a.count; i++)
            {
                int off = start + i * stride;
                ushort v = BitConverter.ToUInt16(bin, off);
                indices[i] = v;
            }
        }
        else if (a.componentType == 5125)
        {
            int stride = bv.byteStride != 0 ? bv.byteStride : sizeof(uint);
            for (int i = 0; i < a.count; i++)
            {
                int off = start + i * stride;
                uint v = BitConverter.ToUInt32(bin, off);
                indices[i] = (int)v;
            }
        }
        else if (a.componentType == 5121)
        {
            int stride = bv.byteStride != 0 ? bv.byteStride : sizeof(byte);
            for (int i = 0; i < a.count; i++)
            {
                int off = start + i * stride;
                byte v = bin[off];
                indices[i] = v;
            }
        }
        else
        {
            throw new Exception("Unsupported indices componentType: " + a.componentType);
        }

        return indices;
    }
}
