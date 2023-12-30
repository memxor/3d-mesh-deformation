using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;


[System.Serializable]
public class VectorArrayWrapper
{
    public int[] x;
    public int[] y;
    public int[] z;
}

public class MeshController : MonoBehaviour
{
    [Range(0, 5)] public float radius;
    [Range(0, 5)] public float deformationStrength;

    private Mesh mesh;
    private Vector3[] verts;
    private Vector3[] modifiedVerts;

    private void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        verts = mesh.vertices;
        modifiedVerts = mesh.vertices;
    }

    private void RecalculateMesh()
    {
        mesh.vertices = modifiedVerts;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        mesh.RecalculateNormals();
    }

    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        { 
            for (int i = 0; i < modifiedVerts.Length; i++)
            {
                Vector3 distance = modifiedVerts[i] - hit.point;

                float smoothingFactor = 2f;
                float force = deformationStrength / (1f + hit.point.sqrMagnitude);

                if (distance.sqrMagnitude < radius)
                {
                    if (Input.GetMouseButton(0))
                    {
                        modifiedVerts[i] = modifiedVerts[i] + Vector3.up * force / smoothingFactor;
                    }
                    else if (Input.GetMouseButton(1))
                    {
                        modifiedVerts[i] = modifiedVerts[i] + Vector3.down * force / smoothingFactor;
                    }
                }
            }
        }

        RecalculateMesh();
    }

    public void GetVertexData()
    {
        var compressedData = GetCompressedData(modifiedVerts);
        Debug.Log(compressedData);
        Debug.Log($"Size Compressed: {Encoding.Unicode.GetByteCount(compressedData)}");
        Vector3[] decompressedData = GetDecompressedData(compressedData);
        modifiedVerts = decompressedData;
        RecalculateMesh();
    }

    private string GetCompressedData(Vector3[] array)
    {
        VectorArrayWrapper wrapper = new()
        {
            x = new int[array.Length],
            y = new int[array.Length],
            z = new int[array.Length],
        };

        for (int i = 0; i < array.Length; i++)
        {
            wrapper.x[i] = Mathf.RoundToInt(array[i].x * 100);
            wrapper.y[i] = Mathf.RoundToInt(array[i].y * 100);
            wrapper.z[i] = Mathf.RoundToInt(array[i].z * 100);
        }

        string jsonData = JsonUtility.ToJson(wrapper);
        Debug.Log(jsonData);
        byte[] byteCompression = CompressString(jsonData);
        string base64String = System.Convert.ToBase64String(byteCompression);

        return base64String;
    }

    private Vector3[] GetDecompressedData(string data)
    {
        byte[] byteCompression = System.Convert.FromBase64String(data);
        string decompressedString = DecompressString(byteCompression);
        VectorArrayWrapper wrapper = JsonUtility.FromJson<VectorArrayWrapper>(decompressedString);

        Vector3[] vectors = new Vector3[wrapper.x.Length];
        for (int i = 0; i < vectors.Length; i++)
        {
            vectors[i].x = wrapper.x[i] / 100f;
            vectors[i].y = wrapper.y[i] / 100f;
            vectors[i].z = wrapper.z[i] / 100f;
        }

        return vectors;
    }

    private byte[] CompressString(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        using MemoryStream memoryStream = new();
        using (GZipStream gzipStream = new(memoryStream, CompressionMode.Compress, true))
        {
            gzipStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }

    private string DecompressString(byte[] compressedData)
    {
        using MemoryStream memoryStream = new(compressedData);
        using GZipStream gzipStream = new(memoryStream, CompressionMode.Decompress);
        using StreamReader reader = new(gzipStream);
        return reader.ReadToEnd();
    }
}