using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AutoGIFOutput : MonoBehaviour
{
    private GIFRecorder m_GIFRecorder;
    private CameraInput cameraInput;

    public int imageWidth = 640;
    public int imageHeight = 480;
    public float frameDuration = 0.1f; // seconds
    public float times = 3f;

    private GameObject target = null;
    private Transform targetTrans = null;
    private ActionQueue actionQueue = null;
    private DestoryByTime destoryByTime = null;

    public UnityEngine.Object m_EffectDirectory;

    private void Start()
    {
        actionQueue = ActionQueue.InitOneActionQueue();
        if (m_EffectDirectory)
        {
            foreach (string assetPath in AutoOperatorOutputUtil.GetAllPrefabs(m_EffectDirectory))
            {
                GameObject assertObj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                actionQueue = actionQueue.AddAction(GIFRecording(assertObj));
            }
        }
        actionQueue.StartQueue();
    }

    #region GIF-Output

    private IEnumerator GIFRecording(GameObject obj)
    {
        Debug.Log(string.Format("<color=blue>{0}</color>", " Start GIF Recording "));
        target = GameObject.Instantiate(obj);
        targetTrans = target.transform;
        destoryByTime = targetTrans.GetComponent<DestoryByTime>();
        if (destoryByTime)
        {
            times = destoryByTime.time;
        }
        AutoOperatorOutputUtil.FocusCameraOnGameObject(Camera.main, target);
        // Start recording
        m_GIFRecorder = new GIFRecorder(imageWidth, imageHeight, frameDuration);
        cameraInput = new CameraInput(m_GIFRecorder, new RealtimeClock(), Camera.main);
        // Get a real GIF look by skipping frames
        cameraInput.frameSkip = 4;
        yield return new WaitForSeconds(times);
        // Stop the recording
        cameraInput.Dispose();
        m_GIFRecorder.FinishWriting();
        Debug.Log(string.Format("<color=yellow>{0}</color>", " Over GIF Recording "));
    }

    #endregion GIF-Output
}