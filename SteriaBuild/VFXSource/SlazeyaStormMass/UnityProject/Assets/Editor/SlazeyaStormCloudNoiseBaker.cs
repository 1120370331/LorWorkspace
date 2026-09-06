using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object=UnityEngine.Object;

// Worley F1 normalization / PW packing adapted as documented in ThirdParty/SOURCES.md.
public static class SlazeyaStormCloudNoiseBaker
{
    public const string ShapePath="Assets/Textures/CloudVolume/CloudShape64.asset";
    public const string ErosionPath="Assets/Textures/CloudVolume/CloudErosion32.asset";
    public const string ComputePath="Assets/Shaders/CloudNoiseBake.compute";
    public const string CachePath="Assets/Textures/CloudVolume/CloudNoiseCache.json";
    public const string Recipe="PW-v1: Pnoise4/8/16 amplitude1/.5/.25; inverse normalized F1; shapePW,W4,W8,W16; erosionW2,W4,W8,1";
    public const int Seed=20260906;
    [Serializable] public class NoiseRecord
    {
        public string asset,channels,allMipSha256,baseVoxelSha256;
        public int size,mips;public float[] minimum,maximum;
        public float rawPeriodError,seamFunctionDifference,filteredPeriodError,filteredSeamDifference;
    }
    [Serializable] public class NoiseManifest
    {
        public string recipe=Recipe,cacheKey,unity,device,graphicsApi;
        public int seed=Seed;public double bakeMilliseconds;public NoiseRecord shape,erosion;
    }
    private static string Full(string path){return Path.GetFullPath(Path.Combine(Application.dataPath,"..",path));}
    private static void Check(bool value,string message){if(!value)throw new Exception("Noise bake: "+message);}
    private static string Digest(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","");}
    public static string CacheKey()
    {
        var bytes=new List<byte>(Encoding.UTF8.GetBytes(Recipe+Seed+Application.unityVersion+SystemInfo.graphicsDeviceName+SystemInfo.graphicsDeviceType));
        // Only offline recipe sources belong here; runtime density/material edits must not rebake noise.
        foreach(string source in new[]{ComputePath,"Assets/Shaders/CloudNoise/PeriodicPerlin.hlsl","Assets/Editor/SlazeyaStormCloudNoiseBaker.cs"})bytes.AddRange(File.ReadAllBytes(Full(source)));
        return Digest(bytes.ToArray());
    }
    public static Texture3D[] Ensure(string output)
    {
        Directory.CreateDirectory(Full("Assets/Textures/CloudVolume"));Directory.CreateDirectory(output);
        string key=CacheKey();NoiseManifest manifest=null;
        if(File.Exists(Full(CachePath)))manifest=JsonUtility.FromJson<NoiseManifest>(File.ReadAllText(Full(CachePath)));
        var shape=AssetDatabase.LoadAssetAtPath<Texture3D>(ShapePath);var erosion=AssetDatabase.LoadAssetAtPath<Texture3D>(ErosionPath);
        if(manifest!=null&&manifest.cacheKey==key&&shape!=null&&erosion!=null)
        {
            Verify(shape,manifest.shape);Verify(erosion,manifest.erosion);
            File.WriteAllText(Path.Combine(output,"noise-bake.json"),JsonUtility.ToJson(manifest,true));Debug.Log("NOISE_CACHE_REUSED "+key);return new[]{shape,erosion};
        }
        Check(SystemInfo.supportsComputeShaders,"compute shaders unavailable");
        var watch=System.Diagnostics.Stopwatch.StartNew();
        AssetDatabase.ImportAsset("Assets/Shaders/CloudNoise/PeriodicPerlin.hlsl",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ComputePath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var compute=AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);Check(compute!=null,"compute asset missing");
        manifest=new NoiseManifest{cacheKey=key,unity=Application.unityVersion,device=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString()};
        shape=Bake(compute,64,false,out manifest.shape);erosion=Bake(compute,32,true,out manifest.erosion);
        shape=Store(shape,ShapePath);erosion=Store(erosion,ErosionPath);AssetDatabase.SaveAssets();
        Verify(shape,manifest.shape);Verify(erosion,manifest.erosion);
        watch.Stop();manifest.bakeMilliseconds=watch.Elapsed.TotalMilliseconds;
        File.WriteAllText(Full(CachePath),JsonUtility.ToJson(manifest,true));AssetDatabase.ImportAsset(CachePath,ImportAssetOptions.ForceSynchronousImport);
        File.WriteAllText(Path.Combine(output,"noise-bake.json"),JsonUtility.ToJson(manifest,true));
        Debug.Log("NOISE_BAKE_NUMERICAL_PASS "+key+" ms="+manifest.bakeMilliseconds);return new[]{shape,erosion};
    }
    private static Texture3D Store(Texture3D texture,string path)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if(existing!=null)
        {
            EditorUtility.CopySerialized(texture,existing);EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(texture);return existing;
        }
        AssetDatabase.CreateAsset(texture,path);return AssetDatabase.LoadAssetAtPath<Texture3D>(path);
    }
    private static ComputeBuffer Features(int cells,int seed)
    {
        var random=new System.Random(seed);var data=new Vector3[cells*cells*cells];
        for(int z=0;z<cells;z++)for(int y=0;y<cells;y++)for(int x=0;x<cells;x++)data[x+cells*(y+z*cells)]=new Vector3(x+(float)random.NextDouble(),y+(float)random.NextDouble(),z+(float)random.NextDouble())/cells;
        var buffer=new ComputeBuffer(data.Length,12);buffer.SetData(data);return buffer;
    }
    private static Texture3D Bake(ComputeShader shader,int size,bool detail,out NoiseRecord record)
    {
        int[] cells=detail?new[]{2,4,8}:new[]{4,8,16};var points=new ComputeBuffer[3];ComputeBuffer result=null;
        try
        {
            int kernel=shader.FindKernel("CSRaw");
            for(int c=0;c<3;c++){points[c]=Features(cells[c],Seed+(detail?9001:101)+c*7919);shader.SetBuffer(kernel,"_Points"+(char)('A'+c),points[c]);}
            shader.SetInts("_Cells",cells);shader.SetInt("_Resolution",size);shader.SetVector("_PerlinSeedOffset",new Vector4(17,43,101,0));
            result=new ComputeBuffer(size*size*size,16);shader.SetBuffer(kernel,"_Output",result);shader.Dispatch(kernel,(size+3)/4,(size+3)/4,(size+3)/4);
            var raw=new Vector4[size*size*size];result.GetData(raw);
            float[] min={float.MaxValue,float.MaxValue,float.MaxValue},max={float.MinValue,float.MinValue,float.MinValue};
            foreach(var v in raw)
            {
                Check(!float.IsNaN(v.x)&&!float.IsInfinity(v.x),"nonfinite Perlin");
                for(int c=0;c<3;c++){float value=v[c+1];Check(!float.IsNaN(value)&&!float.IsInfinity(value),"nonfinite F1");min[c]=Mathf.Min(min[c],value);max[c]=Mathf.Max(max[c],value);}
            }
            for(int c=0;c<3;c++)Check(max[c]-min[c]>1e-8f,"degenerate F1 channel "+c);
            var rgba=new Color32[raw.Length];
            for(int i=0;i<rgba.Length;i++)rgba[i]=Pack(raw[i],min,max,detail);
            var texture=new Texture3D(size,size,size,GraphicsFormat.R8G8B8A8_UNorm,TextureCreationFlags.MipChain){name=detail?"CloudErosion32":"CloudShape64",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=0};
            texture.SetPixels32(rgba);texture.Apply(true,false);
            record=new NoiseRecord{asset=detail?ErosionPath:ShapePath,size=size,mips=texture.mipmapCount,channels=detail?"R=W2,G=W4,B=W8,A=1; independent seeds":"R=PerlinWorley,G=W4,B=W8,A=W16",baseVoxelSha256=Digest(Bytes(rgba)),allMipSha256=TextureHash(texture),minimum=new float[4],maximum=new float[4]};
            for(int c=0;c<4;c++){record.minimum[c]=1;foreach(Color32 col in rgba){float v=((Color)col)[c];record.minimum[c]=Mathf.Min(record.minimum[c],v);record.maximum[c]=Mathf.Max(record.maximum[c],v);}}
            Probe(shader,points,record);VerifyFilteredPeriod(texture,record);Verify(texture,record);return texture;
        }
        finally{if(result!=null)result.Release();foreach(var p in points)if(p!=null)p.Release();}
    }
    private static Color32 Pack(Vector4 raw,float[] min,float[] max,bool detail)
    {
        // Seb CSNormalize's min/max remap, applied once to each F1 channel before inversion.
        var w=new Vector3();for(int c=0;c<3;c++)w[c]=1-Mathf.Clamp01((raw[c+1]-min[c])/Mathf.Max(1e-8f,max[c]-min[c]));
        if(detail)return (Color32)new Color(w.x,w.y,w.z,1);
        float p=Mathf.Clamp01(0.5f+0.5f*raw.x),fbm=0.625f*w.x+0.25f*w.y+0.125f*w.z;
        return (Color32)new Color(Mathf.Lerp(fbm,1,p),w.x,w.y,w.z);
    }
    private static void Probe(ComputeShader shader,ComputeBuffer[] features,NoiseRecord record)
    {
        var positions=new List<Vector3>();
        for(int i=0;i<12;i++)
        {
            Vector3 p=new Vector3((i*7-19)/64f,(i*11+3)/64f,(i*13-5)/64f);positions.Add(p);positions.Add(p+new Vector3(1,-1,2));
        }
        for(int axis=0;axis<3;axis++)for(int i=0;i<8;i++)
        {var p=new Vector3(0.17f+i*0.073f,0.37f+i*0.043f,0.61f-i*0.061f);p[axis]=0.00001f;positions.Add(p);p[axis]=0.99999f;positions.Add(p);}
        var input=new ComputeBuffer(positions.Count,12);var output=new ComputeBuffer(positions.Count,16);
        try
        {
            int kernel=shader.FindKernel("CSProbe");input.SetData(positions.ToArray());shader.SetInt("_ProbeCount",positions.Count);shader.SetBuffer(kernel,"_ProbePositions",input);shader.SetBuffer(kernel,"_Output",output);
            for(int c=0;c<3;c++)shader.SetBuffer(kernel,"_Points"+(char)('A'+c),features[c]);
            shader.Dispatch(kernel,(positions.Count+31)/32,1,1);var data=new Vector4[positions.Count];output.GetData(data);
            for(int i=0;i<data.Length;i+=2){float error=(data[i]-data[i+1]).magnitude;if(i<24)record.rawPeriodError=Mathf.Max(record.rawPeriodError,error);else record.seamFunctionDifference=Mathf.Max(record.seamFunctionDifference,error);}
            Check(record.rawPeriodError<0.0002f,"raw periodic function mismatch "+record.rawPeriodError);Check(record.seamFunctionDifference<0.01f,"raw function seam discontinuity "+record.seamFunctionDifference);
        }
        finally{input.Release();output.Release();}
    }
    public static byte[] Bytes(Color32[] colors)
    {var result=new byte[colors.Length*4];for(int i=0;i<colors.Length;i++){result[i*4]=colors[i].r;result[i*4+1]=colors[i].g;result[i*4+2]=colors[i].b;result[i*4+3]=colors[i].a;}return result;}
    public static string TextureHash(Texture3D texture)
    {var bytes=new List<byte>();for(int mip=0;mip<texture.mipmapCount;mip++)bytes.AddRange(Bytes(texture.GetPixels32(mip)));return Digest(bytes.ToArray());}
    private static Color Filter(Color32[] data,int size,Vector3 uv)
    {
        Vector3 p=new Vector3((uv.x-Mathf.Floor(uv.x))*size-0.5f,(uv.y-Mathf.Floor(uv.y))*size-0.5f,(uv.z-Mathf.Floor(uv.z))*size-0.5f);
        int x=Mathf.FloorToInt(p.x),y=Mathf.FloorToInt(p.y),z=Mathf.FloorToInt(p.z);Vector3 f=new Vector3(p.x-x,p.y-y,p.z-z);Color sum=Color.clear;
        for(int dz=0;dz<2;dz++)for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++){int ix=((x+dx)%size+size)%size,iy=((y+dy)%size+size)%size,iz=((z+dz)%size+size)%size;float weight=(dx==0?1-f.x:f.x)*(dy==0?1-f.y:f.y)*(dz==0?1-f.z:f.z);sum+=(Color)data[ix+size*(iy+iz*size)]*weight;}return sum;
    }
    private static void VerifyFilteredPeriod(Texture3D texture,NoiseRecord record)
    {
        for(int mip=0;mip<texture.mipmapCount;mip++)
        {
            int size=Mathf.Max(1,texture.width>>mip);var data=texture.GetPixels32(mip);
            for(int i=0;i<16;i++)
            {
                Vector3 p=new Vector3(i*0.137f-0.41f,i*0.213f+0.07f,i*0.079f);Color a=Filter(data,size,p),b=Filter(data,size,p+new Vector3(1,-2,3));
                for(int c=0;c<4;c++)record.filteredPeriodError=Mathf.Max(record.filteredPeriodError,Mathf.Abs(a[c]-b[c]));
                for(int axis=0;axis<3;axis++){p[axis]=0.00001f;a=Filter(data,size,p);p[axis]=0.99999f;b=Filter(data,size,p);for(int c=0;c<4;c++)record.filteredSeamDifference=Mathf.Max(record.filteredSeamDifference,Mathf.Abs(a[c]-b[c]));}
            }
        }
        Check(record.filteredPeriodError<0.0001f&&record.filteredSeamDifference<0.01f,"filtered repeat/seam mismatch");
    }
    public static void Verify(Texture3D texture,NoiseRecord record)
    {
        Check(record!=null&&(record.size==64||record.size==32),"missing or invalid noise record");
        Check(texture!=null&&texture.width==record.size&&texture.height==record.size&&texture.depth==record.size,"volume dimensions");
        Check(texture.graphicsFormat==GraphicsFormat.R8G8B8A8_UNorm&&!GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat),"linear uncompressed RGBA32");
        Check(texture.wrapModeU==TextureWrapMode.Repeat&&texture.wrapModeV==TextureWrapMode.Repeat&&texture.wrapModeW==TextureWrapMode.Repeat&&texture.filterMode==FilterMode.Trilinear&&texture.mipmapCount==(record.size==64?7:6)&&record.mips==texture.mipmapCount,"Repeat/trilinear/full mip chain");
        Check(Digest(Bytes(texture.GetPixels32(0)))==record.baseVoxelSha256,"base-voxel numeric hash mismatch");
        Check(TextureHash(texture)==record.allMipSha256,"all-mip numeric hash mismatch");
    }
    public static void VerifyBundle(GameObject prefab,string output)
    {
        var manifest=JsonUtility.FromJson<NoiseManifest>(File.ReadAllText(Full(CachePath)));
        foreach(var renderer in prefab.transform.Find("AB_StormRoot/CloudMacroRoot").GetComponentsInChildren<Renderer>())
        {Verify((Texture3D)renderer.sharedMaterial.GetTexture("_CloudShapeTex"),manifest.shape);Verify((Texture3D)renderer.sharedMaterial.GetTexture("_CloudErosionTex"),manifest.erosion);}
        File.WriteAllText(Path.Combine(output,"noise-bundle-readback.json"),JsonUtility.ToJson(manifest,true));Debug.Log("NOISE_BUNDLE_READBACK_PASS");
    }
    public static void BakeOnly()
    {
        try{string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../../preview_exports/slazeya_storm_mass/round5"));Ensure(output);Debug.Log("NOISE_FOCUSED_PASS");}
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
