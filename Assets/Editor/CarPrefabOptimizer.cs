// Assets/Editor/CarPrefabOptimizer.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class CarPrefabOptimizer
{
    [MenuItem("Tools/Optimize/Combine Car Prefab By Material")]  
    public static void OptimizeSelectedCarPrefab()
    {
        // 1. Select and duplicate
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("Please select a Car Prefab in the Hierarchy or Project window.");
            return;
        }
        GameObject instance = Object.Instantiate(selected);
        instance.name = selected.name + "_Optimized";
        Undo.RegisterCreatedObjectUndo(instance, "Create Optimized Prefab");

        // 2. Gather CombineInstances per material
        var groups = new Dictionary<Material, List<CombineInstance>>();
        foreach (var mr in instance.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            Mesh mesh = mf.sharedMesh;
            for (int i = 0; i < mr.sharedMaterials.Length; i++)
            {
                Material mat = mr.sharedMaterials[i];
                if (!groups.ContainsKey(mat))
                    groups[mat] = new List<CombineInstance>();
                CombineInstance ci = new CombineInstance();
                ci.mesh = mesh;
                ci.subMeshIndex = i;
                ci.transform = mf.transform.localToWorldMatrix;
                groups[mat].Add(ci);
            }
        }

        // 3. Clear original children
        var children = new List<GameObject>();
        foreach (Transform t in instance.transform)
            children.Add(t.gameObject);
        foreach (var go in children)
            Undo.DestroyObjectImmediate(go);

        // 4. Create combined mesh for each material
        foreach (var kvp in groups)
        {
            Material mat = kvp.Key;
            var combineList = kvp.Value;
            if (combineList.Count == 0) continue;

            GameObject newGO = new GameObject("CombinedMesh_" + mat.name);
            newGO.transform.SetParent(instance.transform, false);

            var mf = newGO.AddComponent<MeshFilter>();
            Mesh combined = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            combined.CombineMeshes(combineList.ToArray(), true, true);
            mf.sharedMesh = combined;

            var mrNew = newGO.AddComponent<MeshRenderer>();
            mrNew.sharedMaterial = mat;
        }

        Debug.LogFormat("Optimized prefab '{0}' created with {1} material groups.", instance.name, groups.Count);
    }
}
