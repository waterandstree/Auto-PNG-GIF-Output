using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 任务执行队列
/// </summary>
public class ActionQueue : MonoBehaviour
{
    private event Action onComplete;

    private List<OneAction> actions = new List<OneAction>();

    public static ActionQueue InitOneActionQueue()
    {
        return new GameObject().AddComponent<ActionQueue>();
    }

    /// <summary>
    /// 添加一个任务到队列
    /// </summary>
    /// <param name="startAction">开始时执行的方法</param>
    /// <param name="IsCompleted">判断该节点是否完成</param>
    /// <returns></returns>
    public ActionQueue AddAction(Action startAction, Func<bool> IsCompleted)
    {
        actions.Add(new OneAction(startAction, IsCompleted));
        return this;
    }

    /// <summary>
    /// 添加一个协程方法到队列
    /// </summary>
    /// <param name="enumerator">一个协程</param>
    /// <returns></returns>
    public ActionQueue AddAction(IEnumerator enumerator)
    {
        actions.Add(new OneAction(enumerator));
        return this;
    }

    /// <summary>
    /// 添加一个任务到队列
    /// </summary>
    /// <param name="action">一个方法</param>
    /// <returns></returns>
    public ActionQueue AddAction(Action action)
    {
        actions.Add(new OneAction(action));
        return this;
    }

    /// <summary>
    /// 绑定执行完毕回调
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public ActionQueue BindCallback(Action callback)
    {
        onComplete += callback;
        return this;
    }

    /// <summary>
    /// 开始执行队列
    /// </summary>
    /// <returns></returns>
    public ActionQueue StartQueue()
    {
        StartCoroutine(StartQueueAsync());
        return this;
    }

    private IEnumerator StartQueueAsync()
    {
        if (actions.Count > 0)
        {
            if (actions[0].startAction != null)
            {
                actions[0].startAction();
            }
        }
        while (actions.Count > 0)
        {
            yield return actions[0].enumerator;
            actions.RemoveAt(0);
            if (actions.Count > 0)
            {
                if (actions[0].startAction != null)
                {
                    actions[0].startAction();
                }
            }
            else
            {
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        if (onComplete != null)
        {
            onComplete();
        }
        Destroy(gameObject);
    }

    private class OneAction
    {
        public Action startAction;
        public IEnumerator enumerator;

        public OneAction(Action startAction, Func<bool> IsCompleted)
        {
            this.startAction = startAction;
            //如果没用协程，自己创建一个协程
            enumerator = new CustomEnumerator(IsCompleted);
        }

        public OneAction(IEnumerator enumerator, Action action = null)
        {
            this.startAction = action;
            this.enumerator = enumerator;
        }

        public OneAction(Action action)
        {
            this.startAction = action;
            this.enumerator = null;
        }

        /// <summary>
        /// 自定义的协程
        /// </summary>
        private class CustomEnumerator : IEnumerator
        {
            public object Current => null;
            private Func<bool> IsCompleted;

            public CustomEnumerator(Func<bool> IsCompleted)
            {
                this.IsCompleted = IsCompleted;
            }

            public bool MoveNext()
            {
                return !IsCompleted();
            }

            public void Reset()
            {
            }
        }
    }
}

public class AutoOperatorOutputUtil
{
    public static Vector3 ExponentialDecayFilter(Vector3 lastValue, Vector3 currentValue, float tau, float deltaT)
    {
        if (tau > 0)
        {
            var a = Mathf.Exp(-deltaT / tau);
            return a * lastValue + (1.0f - a) * currentValue;
        }
        else
        {
            return currentValue;
        }
    }

    public static void CalculationMaxBound(Transform parent)
    {
        Vector3 postion = parent.position;
        Quaternion rotation = parent.rotation;
        Vector3 scale = parent.localScale;
        parent.position = Vector3.zero;
        parent.rotation = Quaternion.Euler(Vector3.zero);
        parent.localScale = Vector3.one;

        Collider[] colliders = parent.GetComponentsInChildren<Collider>();
        foreach (Collider child in colliders)
        {
            UnityEngine.Object.DestroyImmediate(child);
        }
        Vector3 center = Vector3.zero;
        Renderer[] renders = parent.GetComponentsInChildren<Renderer>();
        foreach (Renderer child in renders)
        {
            center += child.bounds.center;
        }
        center /= parent.GetComponentsInChildren<Transform>().Length;
        //center /= parent.GetComponentsInChildren().Length;
        Bounds bounds = new Bounds(center, Vector3.zero);
        foreach (Renderer child in renders)
        {
            bounds.Encapsulate(child.bounds);
        }
        BoxCollider boxCollider = parent.gameObject.AddComponent<BoxCollider>();
        boxCollider.center = bounds.center - parent.position;
        boxCollider.size = bounds.size;

        parent.position = postion;
        parent.rotation = rotation;
        parent.localScale = scale;
    }

    public static Bounds CalculateBounds(GameObject go)
    {
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        UnityEngine.Object[] rList = go.GetComponentsInChildren(typeof(Renderer));
        foreach (Renderer r in rList)
        {
            b.Encapsulate(r.bounds);
        }
        return b;
    }

    public static string[] GetAllPrefabs(UnityEngine.Object obj)
    {
        var directory = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(directory) || !directory.StartsWith("Assets"))
            throw new ArgumentException("folderPath");
        //=======================================================================//
        string[] subFolders = Directory.GetDirectories(directory);
        string[] guids = null;
        string[] assetPaths = null;
        List<string> assetPathsList = new List<string>();//2021-4-29
        int i = 0, iMax = 0;
        foreach (var folder in subFolders)
        {
            guids = AssetDatabase.FindAssets("t:Prefab", new string[] { folder });
            assetPaths = new string[guids.Length];
            for (i = 0, iMax = assetPaths.Length; i < iMax; ++i)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                //Debug.Log(assetPaths[i]);
                assetPathsList.Add(assetPaths[i]);//2021-4-29
            }
        }
        if (subFolders.Length == 0)
        {
            guids = AssetDatabase.FindAssets("t:Prefab", new string[] { directory });
            assetPaths = new string[guids.Length];
            for (i = 0, iMax = assetPaths.Length; i < iMax; ++i)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                //Debug.Log(assetPaths[i]);
                assetPathsList.Add(assetPaths[i]);//2021-4-29
            }
        }
        //=======================================================================//
        //return assetPaths;
        return assetPathsList.ToArray();//2021-4-29
    }

    public static Vector3 FocusCameraOnGameObject(Camera c, GameObject go)
    {
        Bounds b = CalculateBounds(go);
        Vector3 max = b.size;
        // Get the radius of a sphere circumscribing the bounds
        float radius = max.magnitude / 2f;
        // Get the horizontal FOV, since it may be the limiting of the two FOVs to properly encapsulate the objects
        float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(c.fieldOfView * Mathf.Deg2Rad / 2f) * c.aspect) * Mathf.Rad2Deg;
        // Use the smaller FOV as it limits what would get cut off by the frustum
        float fov = Mathf.Min(c.fieldOfView, horizontalFOV);
        float dist = radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f));
        //Debug.Log("Radius = " + radius + " dist = " + dist);
        c.transform.localPosition = new Vector3(b.center.x, b.center.y, b.center.z - dist);
        if (c.orthographic)
            c.orthographicSize = radius;

        // Frame the object hierarchy
        c.transform.LookAt(b.center);

        var pos = new Vector3(c.transform.localPosition.x, c.transform.localPosition.y, dist);
        return pos;
    }
}