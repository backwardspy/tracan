using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    struct Sphere
    {
        public Vector3 Position;
        public float Radius;
        public Vector3 Albedo;
        public Vector3 Specular;
    }

    public ComputeShader RayTracingShader;
    private int _kernel;

    public Texture SkyboxTexture;

    public Light DirectionalLight;

    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint MaxSpheres = 100;
    public float SpherePlacementRadius = 60.0f;

    private ComputeBuffer _sphereBuffer;

    private RenderTexture _target;
    private Camera _camera;

    private uint _currentSample = 0;
    private Material _addMaterial;

    private void SetupScene()
    {
        List<Sphere> spheres = new List<Sphere>();
        for (int i = 0; i < MaxSpheres; i++)
        {
            Sphere sphere = new Sphere();

            bool valid = false;
            for (int attempt = 0; attempt < 10; ++attempt)
            {
                sphere.Radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
                Vector2 pos = Random.insideUnitCircle * SpherePlacementRadius;
                sphere.Position = new Vector3(pos.x, sphere.Radius, pos.y);

                bool collided = false;
                foreach (Sphere other in spheres)
                {
                    float minDist = sphere.Radius + other.Radius;
                    if (Vector3.SqrMagnitude(sphere.Position - other.Position) < minDist * minDist)
                    {
                        collided = true;
                        break;
                    }
                }
                if (!collided)
                {
                    valid = true;
                    break;
                }
            }
            if (!valid) continue;

            Vector3 colour = (Vector3)(Vector4)Random.ColorHSV();   // yep
            bool metal = Random.value < 0.5f;
            sphere.Albedo = metal ? Vector3.zero : colour;
            sphere.Specular = metal ? colour : Vector3.one * 0.04f;
            spheres.Add(sphere);
        }

        _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _sphereBuffer.SetData(spheres);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        _kernel = RayTracingShader.FindKernel("CSMain");
    }

    private void OnEnable()
    {
        _currentSample = 0;
        SetupScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null) _sphereBuffer.Release();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        SetShaderParameters();
        Render(dst);
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(_kernel, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));

        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));

        RayTracingShader.SetBuffer(_kernel, "_Spheres", _sphereBuffer);
    }

    private void Render(RenderTexture dst)
    {
        EnsureTarget();
        RayTracingShader.SetTexture(_kernel, "_Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(_kernel, threadGroupsX, threadGroupsY, 1);

        if (_addMaterial == null) _addMaterial = new Material(Shader.Find("Hidden/AddShader"));

        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, dst, _addMaterial);
        _currentSample++;
    }

    private void EnsureTarget()
    {
        if (_target == null ||
            _target.width != Screen.width ||
            _target.height != Screen.height)
        {
            if (_target != null) _target.Release();
            _target = new RenderTexture(
                Screen.width,
                Screen.height,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
            _currentSample = 0;
        }
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }

        if (DirectionalLight.transform.hasChanged)
        {
            _currentSample = 0;
            DirectionalLight.transform.hasChanged = false;
        }
    }
}
