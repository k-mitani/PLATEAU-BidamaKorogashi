using System.Collections;
using System.Collections.Generic;
//using PLATEAU.Geometries;
//using PLATEAU.Native;
using UnityEditor;
using UnityEngine;

public class DoWork : Editor
{
    [MenuItem("Tools/SetRoadHeight")]
    static void SetRoadHeight()
    {
        foreach (var obj in Selection.gameObjects)
        {
            Process(obj);
        }

        static void Process(GameObject obj)
        {
            // 子要素も見ていく。
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                var child = obj.transform.GetChild(i);
                Process(child.gameObject);
            }

            if (!obj.TryGetComponent<MeshFilter>(out var meshFilter)) return;
            var mesh = meshFilter.sharedMesh;

            var vertices = mesh.vertices;
            var targetLayer = 1 << LayerMask.NameToLayer("Floor");
            for (int i = 0; i < vertices.Length; i++)
            {
                Debug.Log(vertices[i]);
                var ray = new Ray(vertices[i] + Vector3.up * 1000, Vector3.down);
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * 2000, Color.red, 10);
                if (Physics.Raycast(ray, out var hit, 99999, targetLayer))
                {
                    Debug.Log(hit.collider.name);
                    vertices[i].y = hit.point.y + 0f;
                    //vertices[i].y = 0;
                }
                else
                {
                    Debug.Log("no hit");
                }
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }
    }

    [MenuItem("Tools/SetDemHeight")]
    static void SetDemHeight()
    {
        foreach (var obj in Selection.gameObjects)
        {
            Process(obj);
        }

        static void Process(GameObject obj)
        {
            // 子要素も見ていく。
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                var child = obj.transform.GetChild(i);
                Process(child.gameObject);
            }

            if (!obj.TryGetComponent<MeshFilter>(out var meshFilter)) return;
            var mesh = meshFilter.sharedMesh;
            var vertices = mesh.vertices;
            var targetLayer = 1 << LayerMask.NameToLayer("Road");
            for (int i = 0; i < vertices.Length; i++)
            {
                Debug.Log(vertices[i]);
                var ray = new Ray(vertices[i] + Vector3.up * 1000, Vector3.down);
                //Debug.DrawLine(ray.origin, ray.origin + ray.direction * 2000, Color.red, 10);
                if (Physics.Raycast(ray, out var hit, 99999, targetLayer))
                {
                    Debug.Log(hit.collider.name);
                    vertices[i].y = hit.point.y - 0.03f;
                    //vertices[i].y = 0;
                }
                else
                {
                    Debug.Log("no hit");
                }
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }
    }

    [MenuItem("Tools/Calculate")]
    static void Calculate()
    {
        //var vec = new PlateauVector3d(
        //    50565.8352578886,
        //    0,
        //    149463.247129192);
        //var geoReference = GeoReference.Create(vec, 1, CoordinateSystem.EUN, 4);


        //foreach (var l in TargetLocation.Data)
        //{
        //    //var kawara2 = new GeoCoordinate(34.33882, 134.052579999, 0);
        //    var coord = new GeoCoordinate(l.coord[1], l.coord[0], 0);
        //    var vec2 = geoReference.Project(coord);
        //    Debug.Log(vec2);
        //}
    }

    [MenuItem("Tools/DeleteDeactivatedObjects")]
    static void DeleteDeactivatedBuildings()
    {
        foreach (var obj in Selection.gameObjects)
        {
            Process(obj);
        }

        static void Process(GameObject obj)
        {
            if (!obj.activeSelf) DestroyImmediate(obj);
            else
            {
                // 子要素も見ていく。
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    var child = obj.transform.GetChild(i);
                    Process(child.gameObject);
                }
            }
        }
    }


    [MenuItem("Tools/ShowTargetPositions")]
    static void ShowTargetPositions()
    {
        foreach (var l in TargetLocation.Data)
        {
            var pos = l.position;
            Debug.DrawLine(pos, pos + Vector3.up * 100, Color.red, 10);
        }

    }
}
