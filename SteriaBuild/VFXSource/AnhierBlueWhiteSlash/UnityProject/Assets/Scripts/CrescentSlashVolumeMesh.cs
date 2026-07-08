using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CrescentSlashVolumeMesh : MonoBehaviour
{
    [Header("Overall Shape")]
    [Tooltip("参考半径，现在更像刀光中心参考线半径")]
    public float outerRadius = 2.0f;

    [Tooltip("整体横向拉伸")]
    public float horizontalScale = 1.35f;

    [Tooltip("整体纵向拉伸")]
    public float verticalScale = 1.15f;

    [Header("Blade Width Profile")]
    [Tooltip("两端宽度，越小越尖")]
    public float tipBladeWidth = 0.03f;

    [Tooltip("中间最大宽度")]
    public float middleBladeWidth = 0.90f;

    [Tooltip("中间鼓起程度，越大越像两端尖、中间胖")]
    [Range(0.3f, 5f)]
    public float middleBulgePower = 1.4f;

    [Tooltip("宽度变化速度。1是原速，1.875表示在原来1.5基础上再快25%")]
    [Range(0.5f, 3f)]
    public float widthChangeRate = 1.875f;

    [Tooltip("宽度向外扩张比例。0.3表示30%向外，70%向内")]
    [Range(0f, 1f)]
    public float outwardWidthRatio = 0.3f;

    [Header("Volume Thickness")]
    [Tooltip("整体厚度，Z方向")]
    public float depth = 0.12f;

    [Tooltip("表面轻微起伏，避免像完全平板")]
    public float ridgeAmplitude = 0.025f;

    [Header("Arc Range")]
    [Tooltip("起始角度。原来145，上方减少60度后为85")]
    public float startAngle = 85f;

    [Tooltip("结束角度。原来-145，下方减少30度后为-115")]
    public float endAngle = -115f;

    [Tooltip("让两端向左拉长，形成尖端感")]
    public float tipLength = 0.45f;

    [Header("Edge Detail")]
    [Tooltip("边缘噪声，控制毛刺感")]
    public float edgeNoise = 0.03f;

    [Tooltip("噪声种子")]
    public float noiseSeed = 3.17f;

    [Header("Mesh Quality")]
    [Range(8, 256)]
    public int lengthSegments = 96;

    [Range(2, 32)]
    public int widthSegments = 10;

    private Mesh mesh;

    private void Awake()
    {
        Generate();
    }

    private void OnEnable()
    {
        Generate();
    }

    private void OnValidate()
    {
        outerRadius = Mathf.Max(0.01f, outerRadius);
        horizontalScale = Mathf.Max(0.01f, horizontalScale);
        verticalScale = Mathf.Max(0.01f, verticalScale);

        tipBladeWidth = Mathf.Max(0.001f, tipBladeWidth);
        middleBladeWidth = Mathf.Max(tipBladeWidth + 0.001f, middleBladeWidth);

        middleBulgePower = Mathf.Max(0.1f, middleBulgePower);
        widthChangeRate = Mathf.Max(0.1f, widthChangeRate);

        depth = Mathf.Max(0.001f, depth);

        lengthSegments = Mathf.Max(8, lengthSegments);
        widthSegments = Mathf.Max(2, widthSegments);

        Generate();
    }

    [ContextMenu("Generate Crescent Slash Mesh")]
    public void Generate()
    {
        MeshFilter mf = GetComponent<MeshFilter>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Crescent Slash Volume Mesh";
        }
        else
        {
            mesh.Clear();
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> controlUvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        int rowCount = lengthSegments + 1;
        int colCount = widthSegments + 1;

        int frontOffset = 0;
        int backOffset = rowCount * colCount;

        // =========================
        // Front vertices
        // =========================
        for (int i = 0; i <= lengthSegments; i++)
        {
            float t = i / (float)lengthSegments;

            for (int j = 0; j <= widthSegments; j++)
            {
                float u = j / (float)widthSegments;

                Vector3 p = GetPoint(t, u);
                float halfZ = GetHalfDepth(t, u);

                vertices.Add(new Vector3(p.x, p.y, halfZ));
                uvs.Add(new Vector2(t, u));
                controlUvs.Add(new Vector2(t, u));
            }
        }

        // =========================
        // Back vertices
        // =========================
        for (int i = 0; i <= lengthSegments; i++)
        {
            float t = i / (float)lengthSegments;

            for (int j = 0; j <= widthSegments; j++)
            {
                float u = j / (float)widthSegments;

                Vector3 p = GetPoint(t, u);
                float halfZ = GetHalfDepth(t, u);

                vertices.Add(new Vector3(p.x, p.y, -halfZ));
                uvs.Add(new Vector2(t, u));
                controlUvs.Add(new Vector2(t, u));
            }
        }

        // =========================
        // Front surface
        // =========================
        for (int i = 0; i < lengthSegments; i++)
        {
            for (int j = 0; j < widthSegments; j++)
            {
                int a = frontOffset + Index(i, j);
                int b = frontOffset + Index(i + 1, j);
                int c = frontOffset + Index(i + 1, j + 1);
                int d = frontOffset + Index(i, j + 1);

                AddQuad(triangles, a, d, c, b);
            }
        }

        // =========================
        // Back surface
        // =========================
        for (int i = 0; i < lengthSegments; i++)
        {
            for (int j = 0; j < widthSegments; j++)
            {
                int a = backOffset + Index(i, j);
                int b = backOffset + Index(i + 1, j);
                int c = backOffset + Index(i + 1, j + 1);
                int d = backOffset + Index(i, j + 1);

                AddQuad(triangles, a, b, c, d);
            }
        }

        // =========================
        // Outer side wall, u = 0
        // =========================
        for (int i = 0; i < lengthSegments; i++)
        {
            int f0 = frontOffset + Index(i, 0);
            int f1 = frontOffset + Index(i + 1, 0);
            int b1 = backOffset + Index(i + 1, 0);
            int b0 = backOffset + Index(i, 0);

            AddQuad(triangles, f0, f1, b1, b0);
        }

        // =========================
        // Inner side wall, u = widthSegments
        // =========================
        for (int i = 0; i < lengthSegments; i++)
        {
            int f0 = frontOffset + Index(i, widthSegments);
            int f1 = frontOffset + Index(i + 1, widthSegments);
            int b1 = backOffset + Index(i + 1, widthSegments);
            int b0 = backOffset + Index(i, widthSegments);

            AddQuad(triangles, f0, b0, b1, f1);
        }

        // =========================
        // Start cap
        // =========================
        for (int j = 0; j < widthSegments; j++)
        {
            int f0 = frontOffset + Index(0, j);
            int f1 = frontOffset + Index(0, j + 1);
            int b1 = backOffset + Index(0, j + 1);
            int b0 = backOffset + Index(0, j);

            AddQuad(triangles, f0, b0, b1, f1);
        }

        // =========================
        // End cap
        // =========================
        for (int j = 0; j < widthSegments; j++)
        {
            int f0 = frontOffset + Index(lengthSegments, j);
            int f1 = frontOffset + Index(lengthSegments, j + 1);
            int b1 = backOffset + Index(lengthSegments, j + 1);
            int b0 = backOffset + Index(lengthSegments, j);

            AddQuad(triangles, f0, f1, b1, b0);
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, controlUvs);
        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
    }

    private int Index(int i, int j)
    {
        return i * (widthSegments + 1) + j;
    }

    /// <summary>
    /// t: 沿月牙长度方向，0是一端，1是另一端，0.5是中间
    /// u: 从外缘到内缘，0是外缘，1是内缘
    /// </summary>
    private Vector3 GetPoint(float t, float u)
    {
        float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;

        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        // 基础宽度曲线：两端为0，中间为1
        float baseProfile = Mathf.Sin(Mathf.PI * t);

        // 中间宽度变化率更快：
        // 原来是 1.5，现在再快25%，变为 1.875
        float widthProfile = Mathf.Pow(baseProfile, middleBulgePower * widthChangeRate);

        float bladeWidth = Mathf.Lerp(tipBladeWidth, middleBladeWidth, widthProfile);
        bladeWidth = Mathf.Min(bladeWidth, outerRadius * 0.95f);

        // 宽度不是全部向内扩，而是30%向外、70%向内
        float outwardWidth = bladeWidth * outwardWidthRatio;
        float inwardWidth = bladeWidth * (1f - outwardWidthRatio);

        float outerR = outerRadius + outwardWidth;
        float innerR = outerRadius - inwardWidth;

        Vector2 outer = new Vector2(
            cos * outerR * horizontalScale,
            sin * outerR * verticalScale
        );

        Vector2 inner = new Vector2(
            cos * innerR * horizontalScale,
            sin * innerR * verticalScale
        );

        // 两端拉尖
        float tipFactor = Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 2.5f);

        outer.x -= tipLength * tipFactor;
        inner.x -= tipLength * tipFactor * 0.65f;

        // 从外缘插值到内缘
        float easedU = Smooth01(u);
        Vector2 p = Vector2.Lerp(outer, inner, easedU);

        // 边缘毛刺，不加颜色，只改变几何轮廓
        float edgeFactor = Mathf.Max(1f - u, u);
        edgeFactor = Mathf.Pow(edgeFactor, 3f);

        float n1 = Mathf.PerlinNoise(noiseSeed + t * 13.7f, u * 4.1f) - 0.5f;
        float n2 = Mathf.PerlinNoise(noiseSeed + 10.0f + t * 27.3f, u * 2.9f) - 0.5f;

        Vector2 radial = new Vector2(cos, sin).normalized;
        Vector2 tangent = new Vector2(-sin, cos).normalized;

        p += radial * n1 * edgeNoise * edgeFactor;
        p += tangent * n2 * edgeNoise * edgeFactor;

        return new Vector3(p.x, p.y, 0f);
    }

    /// <summary>
    /// 获取Z方向半厚度：
    /// 两端薄，中间厚；
    /// 边缘薄，中线厚。
    /// </summary>
    private float GetHalfDepth(float t, float u)
    {
        float baseProfile = Mathf.Sin(Mathf.PI * t);

        // 厚度也跟随中间宽度变化率
        float lengthProfile = Mathf.Pow(baseProfile, middleBulgePower * widthChangeRate);

        float widthProfile = Mathf.Sin(Mathf.PI * u);
        widthProfile = Mathf.Pow(widthProfile, 0.55f);

        float thicknessProfile = (0.25f + 0.75f * widthProfile) * lengthProfile;

        float ridge =
            Mathf.Sin(t * Mathf.PI * 10f + u * 2.3f) *
            Mathf.Sin(u * Mathf.PI) *
            ridgeAmplitude;

        float halfDepth = depth * thicknessProfile + ridge;

        return Mathf.Max(0.003f, halfDepth);
    }

    private float Smooth01(float x)
    {
        return x * x * (3f - 2f * x);
    }

    private void AddQuad(List<int> tris, int a, int b, int c, int d)
    {
        tris.Add(a);
        tris.Add(b);
        tris.Add(c);

        tris.Add(a);
        tris.Add(c);
        tris.Add(d);
    }
}
