using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VHLineRenderer : MonoBehaviour
{
    public int vertexCount = 2;
    [SerializeField] private float width = 0.1f;
    [SerializeField] private Material material;
    private List<Vector3> positions = new List<Vector3>();
    private List<GameObject> cylinders = new List<GameObject>();
    
    public void SetVertexCount(int count)
    {
        vertexCount = count;
        positions.Clear();
        for (int i = 0; i < vertexCount; i++)
        {
            positions.Add(Vector3.zero);
        }
        UpdateCylinders();
    }
    
    public void SetPosition(int index, Vector3 position)
    {
        if (index < 0 || index >= positions.Count) return;
        positions[index] = position;
        UpdateCylinders();
    }
    
    private void UpdateCylinders()
    {
        // Clear existing cylinders
        foreach (var cylinder in cylinders)
        {
            Destroy(cylinder);
        }
        cylinders.Clear();
        
        // Create new cylinders
        for (int i = 0; i < positions.Count - 1; i++)
        {
            CreateCylinderBetweenPoints(positions[i], positions[i + 1]);
        }
    }
    
    private void CreateCylinderBetweenPoints(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        Vector3 midPoint = (start + end) / 2;
        
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.position = midPoint;
        cylinder.transform.up = direction.normalized;
        cylinder.transform.localScale = new Vector3(width, distance / 2, width);
        
        if (material)
        {
            cylinder.GetComponent<Renderer>().material = material;
        }
        
        cylinder.transform.parent = transform;
        cylinders.Add(cylinder);
    }
}