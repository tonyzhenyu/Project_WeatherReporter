using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;
using System.Runtime.InteropServices;
using System.Drawing;
public enum LandType
{
    River = 0,//河
    Sand = 1,//沙子
    Dirty = 2,//土
    Snowfield = 3,//雪地
}
public struct MapData
{
    public int type;//土地类型
    public int height;//高度
    public int temperature;//温度
    public int pressure;//气压
}
public struct MapGroupData
{
    public MapData[,] layer0;
    public MapData[,] layer1;
    public MapData[,] layer2;
    public MapData[,] layer3;
}
public class GPUCompactManager : MonoBehaviour
{
    public ComputeShader computeShader;
    public Texture tex;
    public int size;
    GPUCompact compactor;
    // Start is called before the first frame update
    MapGroupData[] mapGroupDatas;
    private void Awake()
    {
        mapGroupDatas = new MapGroupData[size];
    }
    void Start()
    {
        compactor = new GPUCompact(computeShader, tex);
        compactor.Init(mapGroupDatas);
        GenerateCubes(in mapGroupDatas);
    }
    void Update()
    {
        
    }
    private void OnDestroy()
    {
        compactor.Release();
    }

    void GenerateCubes(in MapGroupData[] inputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            MapData[,] mapGroupData = inputs[i].layer0;
            int offset = i * mapGroupData.GetLength(0);
            for (int x = 0; x < mapGroupData.GetLength(0); x++)
            {
                for (int y = 0; y < mapGroupData.GetLength(1); y++)
                {
                    GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    g.transform.position = new Vector3(offset+x, (float)mapGroupData[x, y].height / 255.0f, y);
                }
            }
        }
    }
}
public class GPUCompact
{
    private ComputeBuffer computeBuffer;
    private ComputeShader m_computeShader;
    private Texture m_texture;

    public static int idTex = Shader.PropertyToID("_CTex_F01");
    public static int idDatas = Shader.PropertyToID("_CDatas_F01");
    public static int idWidthGroupCount = Shader.PropertyToID("_CDatas_F01_Width_GroupCount");
    public GPUCompact(ComputeShader computeShader,Texture texture)
    {
        m_computeShader = computeShader;
        m_texture = texture;
    }
    public void Init(MapGroupData[] mapGroupDatas)   
    {
        int4[] data = new int4[m_texture.width * m_texture.height];
        int4[,] outputData;
        int handel = m_computeShader.FindKernel("ReadTexture");
        int stride = Marshal.SizeOf<int4>();
        int widthGroupCount = m_texture.width / 32;
        int heightGroupCount = m_texture.height / 32;
        computeBuffer = new ComputeBuffer(m_texture.width * m_texture.height, stride);
        computeBuffer.SetData(data);
        m_computeShader.SetInt(idWidthGroupCount, widthGroupCount);
        m_computeShader.SetTexture(handel, idTex, m_texture);
        m_computeShader.SetBuffer(handel, idDatas, computeBuffer);
        m_computeShader.Dispatch(handel, widthGroupCount, heightGroupCount, 1);
        computeBuffer.GetData(data);
        outputData = ReMapping(data, m_texture.width, m_texture.height);
        for (int i = 0; i < mapGroupDatas.Length; i++)
        {
            mapGroupDatas[i].layer0 = new MapData[m_texture.width, m_texture.height];
            mapGroupDatas[i].layer1 = new MapData[m_texture.width, m_texture.height];
            mapGroupDatas[i].layer2 = new MapData[m_texture.width, m_texture.height];
            mapGroupDatas[i].layer3 = new MapData[m_texture.width, m_texture.height];
            for (int j = 0; j < outputData.GetLength(0); j++)
            {
                for (int k = 0; k < outputData.GetLength(1); k++)
                {
                    mapGroupDatas[i].layer0[j, k].type = outputData[j, k].x;
                    mapGroupDatas[i].layer1[j, k].type = outputData[j, k].x;
                    mapGroupDatas[i].layer2[j, k].type = outputData[j, k].x;
                    mapGroupDatas[i].layer3[j, k].type = outputData[j, k].x;

                    mapGroupDatas[i].layer0[j, k].height = outputData[j, k].y;
                    mapGroupDatas[i].layer1[j, k].height = outputData[j, k].y;
                    mapGroupDatas[i].layer2[j, k].height = outputData[j, k].y;
                    mapGroupDatas[i].layer3[j, k].height = outputData[j, k].y;

                    mapGroupDatas[i].layer0[j, k].temperature = outputData[j, k].z;
                    mapGroupDatas[i].layer1[j, k].temperature = outputData[j, k].z;
                    mapGroupDatas[i].layer2[j, k].temperature = outputData[j, k].z;
                    mapGroupDatas[i].layer3[j, k].temperature = outputData[j, k].z;

                    mapGroupDatas[i].layer0[j, k].pressure = outputData[j, k].w;
                    mapGroupDatas[i].layer1[j, k].pressure = outputData[j, k].w;
                    mapGroupDatas[i].layer2[j, k].pressure = outputData[j, k].w;
                    mapGroupDatas[i].layer3[j, k].pressure = outputData[j, k].w;
                }
            }
        }
    }
    public int4[,] ReMapping(int4[] input, int width, int height)
    {
        var output = new int4[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                output[x, y] = input[y * width + x];
            }
        }
        return output;
    }
    public void Release()
    {
        computeBuffer.Release();
    }
}