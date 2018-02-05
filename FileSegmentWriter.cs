﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GZipTest
{
    public class FileSegmentWriter
    {
        private volatile int _currentSegment = 0;
        private readonly int _length;
        private readonly Dictionary<int, MemoryStream> _segments;
        private readonly Dictionary<int, Action> _callbacks;
        private readonly FileStream _writeFileStream;
        private bool _isAborted;
        public event EventHandler<SuccessEventArgs> OnSuccess;
        public event EventHandler<ErrorEventArgs> OnError;

        public FileSegmentWriter(int length, string destFile, EventHandler<SuccessEventArgs> successCallback, EventHandler<ErrorEventArgs> errorCallback)
        {
            _length = length;
            _writeFileStream = File.Create(destFile);
            _callbacks = new Dictionary<int, Action>(length);
            _segments = new Dictionary<int, MemoryStream>(length);

            OnSuccess += successCallback;
            OnSuccess += (sender, args) => _writeFileStream?.Dispose();
            OnError += errorCallback;
            OnError += (sender, args) =>
            {
                _writeFileStream?.Dispose();
                _isAborted = true;
            };

            var thread = new Thread(WriteToFile);
            thread.Start();
        }

        private void WriteToFile()
        {
            while (_currentSegment < _length && _isAborted == false)
            {
                try
                {
                    if (_segments.ContainsKey(_currentSegment))
                    {
                        const int bufferLength = 1024 * 1024; //1 Megabyte
                        var buffer = new byte[bufferLength];
                        int bytesCount;
                        var compressedStream = _segments[_currentSegment];

                        //writing
                        while ((bytesCount = compressedStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            _writeFileStream.Write(buffer, 0, bytesCount);
                        }

                        if (_callbacks.ContainsKey(_currentSegment))
                        {
                            _callbacks[_currentSegment].Invoke();
                            _callbacks.Remove(_currentSegment);
                        }

                        //Console.WriteLine($"Remove segment: {_currentSegment}");
                        _segments.Remove(_currentSegment);
                        ++_currentSegment;
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new ErrorEventArgs(e, e.Message));
                    //throw;
                }
            }

            if (_isAborted == false)
            {
                OnSuccess?.Invoke(this, new SuccessEventArgs());
            }
        }

        public void WriteSegment(int i, MemoryStream memoryStream, Action callback)
        {
            _segments.Add(i, memoryStream);
            _callbacks.Add(i, callback);
        }

        public void Abort()
        {
            _isAborted = true;
        }
    }
}
