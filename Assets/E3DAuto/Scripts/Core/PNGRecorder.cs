/*
*   NatCorder
*   Copyright (c) 2020 Yusuf Olokoba.
*/

namespace NatSuite.Recorders
{
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Internal;

    /// <summary>
    /// JPG image sequence recorder.
    /// This recorder is currently supported on macOS and Windows.
    /// </summary>
    [Doc(@"PNGRecorder")]
    public sealed class PNGRecorder : IMediaRecorder
    {
        #region --Client API--

        /// <summary>
        /// Image size.
        /// </summary>
        [Doc(@"FrameSize")]
        public (int width, int height) frameSize => (framebuffer.width, framebuffer.height);

        /// <summary>
        /// Create a JPG recorder.
        /// </summary>
        /// <param name="imageWidth">Image width.</param>
        /// <param name="imageHeight">Image height.</param>
        [Doc(@"PNGRecorderCtor")]
        public PNGRecorder(int imageWidth, int imageHeight, string extension)
        {
            // Create buffers
            this.framebuffer = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false, false);
            this.writeQueue = new Queue<byte[]>();
            // Setup output
            string recordingPath = Path.Combine(Directory.GetCurrentDirectory(), $"recording_PNG");
            if (!string.IsNullOrEmpty(recordingPath))
            {
                if (!Directory.Exists(recordingPath))
                {
                    Directory.CreateDirectory(recordingPath);
                }
            }
            // Start writer thread
            this.recordingTask = Task.Run(() =>
            {
                for (; ; )
                {
                    // Dequeue
                    byte[] frameData;
                    lock ((writeQueue as ICollection).SyncRoot)
                        if (writeQueue.Count > 0)
                            frameData = writeQueue.Dequeue();
                        else
                            continue;
                    // Write out
                    if (frameData != null)
                        File.WriteAllBytes(Path.Combine(recordingPath, extension + ".png"), frameData);
                    else
                        break;
                }
                return recordingPath;
            });
        }

        /// <summary>
        /// Commit a video pixel buffer for encoding.
        /// The pixel buffer MUST have RGBA32 pixel layout.
        /// </summary>
        /// <param name="pixelBuffer">Pixel buffer containing video frame to commit.</param>
        /// <param name="timestamp">Not used.</param>
        [Doc(@"CommitFrame")]
        public void CommitFrame<T>(T[] pixelBuffer, long timestamp) where T : struct
        {
            var handle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
            CommitFrame(handle.AddrOfPinnedObject(), timestamp);
            handle.Free();
        }

        /// <summary>
        /// Commit a video pixel buffer for encoding.
        /// The pixel buffer MUST have RGBA32 pixel layout.
        /// </summary>
        /// <param name="nativeBuffer">Pixel buffer in native memory to commit.</param>
        /// <param name="timestamp">Not used.</param>
        [Doc(@"CommitFrame")]
        public void CommitFrame(IntPtr nativeBuffer, long timestamp)
        {
            // Encode immediately
            framebuffer.LoadRawTextureData(nativeBuffer, framebuffer.width * framebuffer.height * 4);
            var frameData = ImageConversion.EncodeToJPG(framebuffer);
            // Write out on a worker thread
            lock ((writeQueue as ICollection).SyncRoot)
                writeQueue.Enqueue(frameData);
        }

        /// <summary>
        /// This recorder does not support committing audio samples.
        /// </summary>
        [Doc(@"CommitSamplesNotSupported")]
        public void CommitSamples(float[] sampleBuffer, long timestamp) { }

        /// <summary>
        /// Finish writing and return the path to the recorded media file.
        /// </summary>
        [Doc(@"FinishWriting", @"FinishWritingDiscussion")]
        public Task<string> FinishWriting()
        {
            lock ((writeQueue as ICollection).SyncRoot)
                writeQueue.Enqueue(null); // EOS
            Texture2D.Destroy(framebuffer);
            return recordingTask;
        }

        #endregion --Client API--

        #region --Operations--

        private readonly Texture2D framebuffer;
        private readonly Queue<byte[]> writeQueue;
        private readonly Task<string> recordingTask;

        #endregion --Operations--
    }
}