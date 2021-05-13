using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AutoPNGOutput : MonoBehaviour
{
    public int imageWidth = 640;
    public int imageHeight = 480;

    private PNGRecorder m_JPGRecorder;
    private CameraInput cameraInput;
    private bool isPhoto = false;

    private float frameDuration = 0.1f; // seconds

    public float autoFocusTime = 3f;

    private GameObject target = null;
    private Transform targetTrans = null;
    private ActionQueue actionQueue = null;

    public UnityEngine.Object m_ModelDirectory;

    private void Start()
    {
        actionQueue = ActionQueue.InitOneActionQueue();
        if (m_ModelDirectory)
        {
            foreach (string assetPath in AutoOperatorOutputUtil.GetAllPrefabs(m_ModelDirectory))
            {
                GameObject assertObj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                actionQueue = actionQueue.AddAction(WaitPNGAutoFocus(assertObj)).AddAction(PNGRecording("_Front"))
                    .AddAction(WaitPNGAutoFocus(assertObj)).AddAction(PNGRecording("_Side"))
                    .AddAction(WaitPNGAutoFocus(assertObj)).AddAction(PNGRecording("_Left"))
                    .AddAction(WaitPNGAutoFocus(assertObj)).AddAction(PNGRecording("_Back"));
            }
        }
        actionQueue.StartQueue();
    }

    #region PNG-Output

    private IEnumerator PNGRecording(string extension)
    {
        if (extension.Length == 0) yield return null;//2021-5-1
        if (isPhoto == false)
        {
            isPhoto = true;
            // Start recording
            m_JPGRecorder = new PNGRecorder(imageWidth, imageHeight, target.name + extension);
            cameraInput = new CameraInput(m_JPGRecorder, new RealtimeClock(), Camera.main);
            cameraInput.frameSkip = 40;
            Debug.Log(string.Format("<color=blue>{0}</color>", " Start PNG Recording "));
        }
        yield return new WaitForSeconds(frameDuration);

        isPhoto = false;
        // Stop the recording
        cameraInput.Dispose();
        m_JPGRecorder.FinishWriting();
        Debug.Log(string.Format("<color=yellow>{0}</color>", " Over PNG Recording "));
    }

    private int pngIndex = 0;
    private Vector3 rotateAngle = Vector3.zero;

    private IEnumerator WaitPNGAutoFocus(GameObject obj)
    {
        Debug.Log(string.Format("<color=red>{0}</color>", " Start " + obj.name + " Auto Focus "));
        int reIndex = pngIndex % 4;

        switch (reIndex)
        {
            case 1:
                rotateAngle.y = 45f;
                targetTrans.eulerAngles = rotateAngle;
                break;

            case 2:

                rotateAngle.y = 90f;
                targetTrans.eulerAngles = rotateAngle;
                break;

            case 3:
                rotateAngle.y = 180f;
                targetTrans.eulerAngles = rotateAngle;
                break;

            case 0:
                if (target) Destroy(target);
                target = GameObject.Instantiate(obj);
                targetTrans = target.transform;
                rotateAngle = targetTrans.eulerAngles;
                pngIndex = 0;
                break;
        }
        AutoOperatorOutputUtil.FocusCameraOnGameObject(Camera.main, target);
        yield return new WaitForSeconds(autoFocusTime);
        Debug.Log(string.Format("<color=green>{0}</color>", " Over " + obj.name + " Auto Focus "));
        pngIndex++;
    }

    #endregion PNG-Output
}