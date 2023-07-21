using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;
using System.Runtime.InteropServices;

public class GPUCompactManager : MonoBehaviour
{
    public ComputeShader computeShader;
    public Texture tex;

    GPUCompact compactor;
    // Start is called before the first frame update

    
    void Start()
    {
        compactor = new GPUCompact(computeShader, tex);

        compactor.Init();

        compactor.Read();

        if (compactor.data != null)
        {
            //Debug.Log(compactor.data.Length);

            GenerateCubes(in compactor.outputData);
        }

        
    }

    void Update()
    {
        
    }
    private void OnDestroy()
    {
        compactor.Release();
    }

    void GenerateCubes(in int4[,] inputs)
    {
        for (int x = 0; x < inputs.GetLength(0); x++)
        {
            for (int y = 0; y < inputs.GetLength(1); y++)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.transform.position = new Vector3(x, (float)inputs[x, y].x / 255.0f, y );
            }
        }
    }
}

public struct FData
{
    public Vector4[][] layer01;
    //public Vector4[][] layer02;
    //public Vector4[][] layer03;
    //public Vector4[][] layer04;
}

public class GPUCompact
{
    public int4[] data;

    public int4[,] outputData;
    
    public int4[,] ReMapping(int4[] input,int width,int height)
    {
        var output = new int4[width,height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                output[x,y] = input[y * width + x];
            }
        }
        return output;
    }

    private ComputeBuffer cb_layer01;
    //private ComputeBuffer cb_layer02;
    //private ComputeBuffer cb_layer03;
    //private ComputeBuffer cb_layer04;

    private ComputeShader m_computeShader;
    private Texture m_texture;

    static class ID
    {
        public static int tex = Shader.PropertyToID("_CTex_F01");
        public static int datas = Shader.PropertyToID("_CDatas_F01");
        public static int width_GroupCount = Shader.PropertyToID("_CDatas_F01_Width_GroupCount");

    }


    
    public GPUCompact(ComputeShader computeShader,Texture texture)
    {
        m_computeShader = computeShader;
        m_texture = texture;
    }
    public void Init()   
    {
        data = new int4[m_texture.width * m_texture.height];

        int k = m_computeShader.FindKernel("ReadTexture");

        int stride = Marshal.SizeOf<int4>();
        int widthGroupCount = m_texture.width / 32;
        int heightGroupCount = m_texture.height / 32;
        cb_layer01 = new ComputeBuffer(m_texture.width * m_texture.height, stride);
        cb_layer01.SetData(data);
        m_computeShader.SetInt(ID.width_GroupCount, widthGroupCount);
        m_computeShader.SetTexture(k, ID.tex, m_texture);
        m_computeShader.SetBuffer(k, ID.datas, cb_layer01);

        m_computeShader.Dispatch(k, widthGroupCount, heightGroupCount, 1);
        
    }
    public void Read()
    {
        cb_layer01.GetData(data);
        outputData = ReMapping(data, m_texture.width, m_texture.height);
    }
    public void Release()
    {
        cb_layer01.Release();
    }
}