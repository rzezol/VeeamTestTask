using System;
using System.Collections.Generic;
using System.Linq;
namespace VeeamTestTask
{
    public class FileComparer
    {
        private string FilePath { get; }
        private string SrcFullFilePath {  get; }
        private string DstFullFilePath { get; }

        public FileComparer(string filePath, string srcPath, string dstPath)
        {
            FilePath = filePath;
            SrcFullFilePath = Path.GetFullPath(filePath, srcPath);
            DstFullFilePath = Path.GetFullPath(filePath, dstPath);
        }

        public bool FilesEqual()
        {
            try
            {
                // Fast and simple check of size first, advanced content comparison later to save time
                return EqualBySize() && EqualByContent();
            }
            catch (IOException)
            {
                // Can not access file - will retry in next synchronization cycle");
                return true;
            }
        }

        private bool EqualBySize()
        {
            FileInfo srcFileInfo = new FileInfo(SrcFullFilePath);
            FileInfo dstFileInfo = new FileInfo(DstFullFilePath);

            if (srcFileInfo.Length != dstFileInfo.Length)
            {
                return false;
            }

            return true;
        }
        
        // Compare bytes of files from Source and Destination directory.
        private bool EqualByContent()
        {
            using FileStream srcFileStream = File.OpenRead(SrcFullFilePath);
            using FileStream dstFileStream = File.OpenRead(DstFullFilePath);

            // 1MB buffer to speed-up comparison process
            const int bufferSize = 1 * 1024 * 1024;
            byte[] srcBuffer = new byte[bufferSize];
            byte[] dstBuffer = new byte[bufferSize];

            
            while (true)
            {
                int srcBytesRead = srcFileStream.Read(srcBuffer);
                int dstBytesRead = dstFileStream.Read(dstBuffer);

                if (srcBytesRead != dstBytesRead)
                    return false;

                if (srcBytesRead == 0)
                    return true;

                // Using Span to minimize allocations
                if (!srcBuffer.AsSpan(0, srcBytesRead).SequenceEqual(dstBuffer.AsSpan(0, dstBytesRead)))
                    return false;
            }
        }
    }
}
