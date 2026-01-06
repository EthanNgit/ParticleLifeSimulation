using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class Particle
{
    public int particleType { get; set; }
    public Vector2 position { get; set; }
    public Vector2 velocity { get; set; }

    public Particle(int particleType, Vector2 position)
    {
        this.particleType = particleType;
        this.position = position;
    }
}

public class SimulationManager : MonoBehaviour
{
    [Header("Config")]
    public Material circleMaterial;
    public int ParticleCount = 100;
    public int ParticleTypeCount = 1;
    public float ForceMultiplier = 1.0f;
    public Vector2 EnvSize = new Vector2(10, 5);
    public float RMax = 0.5f;
    public float Beta = 0.5f;

    private Mesh circleMesh;
    private Matrix4x4[] matrices;
    private MaterialPropertyBlock propertyBlock;

    private Particle[] particles;
    private Vector4[] particleColors;
    private float[,] interactionMatrix;
    private readonly Quaternion defaultRot = Quaternion.Euler(0, 180, 0);

    private readonly float frictionHalfLife = 0.04f;

    private float minX;
    private float maxX;
    private float minY;
    private float maxY;

    private Dictionary<Vector2Int, List<int>> grid;

    void Start()
    {
        minX = -EnvSize.x;
        maxX = EnvSize.x;
        minY = -EnvSize.y;
        maxY = EnvSize.y;

        circleMesh = CreateCircleMesh(32, 0.015f);
        matrices = new Matrix4x4[ParticleCount];
        propertyBlock = new MaterialPropertyBlock();

        grid = new Dictionary<Vector2Int, List<int>>();

        InitMatrix();
        InitParticles();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float frictionFactor = (float)Math.Pow(0.5f, dt / frictionHalfLife);

        grid.Clear();
        for (int i = 0; i < ParticleCount; i++)
        {
            Vector2Int cellKey = GetGridCell(particles[i].position);
            if (!grid.ContainsKey(cellKey))
                grid[cellKey] = new List<int>();
            grid[cellKey].Add(i);
        }

        Parallel.For(0, ParticleCount, i =>
        {
            float totalForceX = 0;
            float totalForceY = 0;

            Vector2Int cellKey = GetGridCell(particles[i].position);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int neighborKey = cellKey + new Vector2Int(dx, dy);

                    if (grid.ContainsKey(neighborKey))
                    {
                        foreach (int j in grid[neighborKey])
                        {
                            if (i == j) continue;

                            float rx = particles[j].position.x - particles[i].position.x;
                            float ry = particles[j].position.y - particles[i].position.y;
                            float r = Mathf.Sqrt(rx * rx + ry * ry);
                            if (r > 0 && r < RMax)
                            {
                                float f = ForceFunction(r / RMax, interactionMatrix[particles[i].particleType, particles[j].particleType]);
                                totalForceX += rx / r * f;
                                totalForceY += ry / r * f;
                            }
                        }
                    }
                }
            }

            totalForceX *= RMax * ForceMultiplier;
            totalForceY *= RMax * ForceMultiplier;

            float newVelocityX = (particles[i].velocity.x * frictionFactor) + totalForceX * dt;
            float newVelocityY = (particles[i].velocity.y * frictionFactor) + totalForceY * dt;
            particles[i].velocity = new Vector2(newVelocityX, newVelocityY);
        });

        for (int i = 0; i < ParticleCount; i++)
        {
            float newPosX = particles[i].position.x + particles[i].velocity.x * dt;
            float newPosY = particles[i].position.y + particles[i].velocity.y * dt;

            if (newPosX < minX) newPosX = maxX - 0.1f;
            else if (newPosX > maxX) newPosX = minX + 0.1f;

            if (newPosY < minY) newPosY = maxY - 0.1f;
            else if (newPosY > maxY) newPosY = minY + 0.1f;

            particles[i].position = new Vector2(newPosX, newPosY);

            matrices[i] = Matrix4x4.TRS(particles[i].position, defaultRot, Vector3.one);
        }

        Graphics.DrawMeshInstanced(circleMesh, 0, circleMaterial, matrices, ParticleCount, propertyBlock);
    }

    Vector2Int GetGridCell(Vector2 position)
    {
        int x = Mathf.FloorToInt((position.x - minX) / RMax);
        int y = Mathf.FloorToInt((position.y - minY) / RMax);
        return new Vector2Int(x, y);
    }

    float ForceFunction(float r, float a)
    {
        if (r < Beta)
        {
            return (r / Beta - 1);
        }
        else if (Beta < r && r < 1)
        {
            return a * (1 - Math.Abs(2 * r - 1 - Beta) / (1 - Beta));
        }
        else
        {
            return 0;
        }
    }

    void InitMatrix()
    {
        interactionMatrix = new float[ParticleTypeCount, ParticleTypeCount];

        for (int x = 0; x < ParticleTypeCount; x++)
        {
            for (int y = 0; y < ParticleTypeCount; y++)
            {
                float randForce = Random.Range(-1.0f * ForceMultiplier, 1.0f * ForceMultiplier);
                interactionMatrix[x, y] = randForce;
                Debug.Log(randForce);
            }
        }
    }
    void InitParticles()
    {
        Color[] particleTypeColors = new Color[ParticleTypeCount];
        float hueStep = 1f / ParticleTypeCount;
        for (int i = 0; i < ParticleTypeCount; i++)
        {
            float hue = (i * hueStep + Random.Range(0f, hueStep)) % 1f;
            float saturation = Random.Range(0.6f, 1f);
            float value = Random.Range(0.8f, 1f);
            particleTypeColors[i] = Color.HSVToRGB(hue, saturation, value);
        }

        particles = new Particle[ParticleCount];
        particleColors = new Vector4[ParticleCount];
        for (int i = 0; i < ParticleCount; i++)
        {
            Vector2 startPos = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            int randParticleType = Random.Range(0, ParticleTypeCount);
            Particle particle = new Particle(randParticleType, startPos);
            particles[i] = particle;
            particleColors[i] = particleTypeColors[randParticleType];
        }

        propertyBlock.SetVectorArray("_BaseColor", System.Array.ConvertAll(particleColors, c => c));
    }

    Mesh CreateCircleMesh(int segments, float radius)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 2) > segments ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        Debug.Log("Created a mesh");

        return mesh;
    }
}
