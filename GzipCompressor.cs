using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    public class GzipCompressor
    {
        private readonly object _readFileLock = new object();

        private readonly object _callbackLock = new object();

        private readonly FileStream _originalFileStream;

        //private readonly IList<Thread> _workingThreads = new List<Thread>();

        private readonly int _segmentsCount;

        private readonly int _bufferLength = 1024 * 1024;

        private readonly FileSegmentWriter _fileSegmentWriter;

        private volatile int _segmentNumber;

        private bool _isCallbackInvoked = false;

        private event EventHandler<SuccessEventArgs> OnSuccess;

        private event EventHandler<ErrorEventArgs> OnError;

        public static void CompressFile(string sourceFile, string destinationFile, EventHandler<SuccessEventArgs> successCallback, EventHandler<ErrorEventArgs> errorCallback)
        {
            try
            {
                var gzipCompressor = new GzipCompressor(sourceFile, destinationFile);
                gzipCompressor.OnSuccess += successCallback;
                gzipCompressor.OnError += errorCallback;
                gzipCompressor.Compress();
            }
            catch (Exception e)
            {
                errorCallback?.Invoke(null, new ErrorEventArgs(e, e.Message));
            }

        }

        public GzipCompressor(string sourceFile, string destFile)
        {
            var fileToCompress = new FileInfo(sourceFile);
            _originalFileStream = fileToCompress.OpenRead();

            OnSuccess += (sender, args) => _originalFileStream?.Dispose();
            OnError += (sender, args) =>
            {
                _originalFileStream?.Dispose();
                _fileSegmentWriter.Abort();
            };

            _segmentsCount = (int) Math.Ceiling((double) fileToCompress.Length / _bufferLength);
            _fileSegmentWriter = new FileSegmentWriter(_segmentsCount, destFile, OnSuccessInvoke, OnErrorInvoke);
        }

        private void Compress()
        {
            try
            {
                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    var coreNumber = i;
                    var worker = new Thread(() =>
                    {
                        CompressOnCore();
                    });

                    //_workingThreads.Add(worker);
                    worker.Start();
                }
            }
            catch (Exception e)
            {
                OnErrorInvoke(this, new ErrorEventArgs(e, e.Message));
            }
        }

        private void CompressOnCore()
        {
            try
            {
                //CurrentThread.ProcessorAffinity = new IntPtr(coreNumber + 1);
                //Thread.BeginThreadAffinity();
                while (_segmentNumber < _segmentsCount)
                {
                    var buffer = new byte[_bufferLength];
                    int bytesCount;
                    int currentSegment;
                    lock (_readFileLock)
                    {
                        if (_segmentNumber < _segmentsCount)
                        {
                            currentSegment = _segmentNumber;
                            ++_segmentNumber;
                            bytesCount = _originalFileStream.Read(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            break;
                        }
                    }

                    var compressedStream = new MemoryStream();
                    using (GZipStream compressionStream = new GZipStream(compressedStream,
                        CompressionMode.Compress, true))
                    {
                        compressionStream.Write(buffer, 0, bytesCount);
                    }

                    compressedStream.Seek(0, SeekOrigin.Begin);
                    //Console.WriteLine($"Push segment: {currentSegment}");
                    _fileSegmentWriter.WriteSegment(currentSegment, compressedStream, () => { compressedStream.Dispose(); });
                }
            }
            catch (Exception e)
            {
                OnErrorInvoke(this, new ErrorEventArgs(e, e.Message));
            }
        }

        private void OnSuccessInvoke(object sender, SuccessEventArgs successEventArgs)
        {
            lock (_callbackLock)
            {
                if (_isCallbackInvoked == false)
                {
                    _isCallbackInvoked = true;
                    OnSuccess?.Invoke(sender, successEventArgs);
                }
            }
        }

        private void OnErrorInvoke(object sender, ErrorEventArgs errorEventArgs)
        {
            lock (_callbackLock)
            {
                if (_isCallbackInvoked == false)
                {
                    _isCallbackInvoked = true;
                    OnError?.Invoke(sender, errorEventArgs);
                }
            }
        }

        public void Decompress(string sourceFile, string destinationFile)
        {
            var fileToDecompress = new FileInfo(sourceFile);
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                using (FileStream decompressedFileStream = File.Create(destinationFile))
                {
                    using (GZipStream decompressionStream =
                        new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        var buffer = new byte[100];
                        var bytesCount = decompressionStream.Read(buffer, 0, buffer.Length);
                        decompressedFileStream.Write(buffer, 0, bytesCount);
                    }
                }
            }
        }

        //[DllImport("kernel32.dll")]
        //public static extern int GetCurrentThreadId();

        //private ProcessThread CurrentThread
        //{
        //    get
        //    {
        //        int id = GetCurrentThreadId();
        //        return Process.GetCurrentProcess().Threads.Cast<ProcessThread>().Single(th => th.Id == id);
        //    }
        //}
    }
}