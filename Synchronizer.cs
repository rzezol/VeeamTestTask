namespace VeeamTestTask
{
    public class Synchronizer
    {
        private string SrcPath { get; }
        private string DstPath { get; }
        private TimeSpan SyncInterval { get; }
        private EnumerationOptions EnumerationOptions { get; }

        public Synchronizer(CommandArguments commandArguments)
        {
            SrcPath = commandArguments.SrcPath;
            DstPath = commandArguments.DstPath;
            SyncInterval = commandArguments.SyncInterval;

            EnumerationOptions = new();
            EnumerationOptions.RecurseSubdirectories = true;
            EnumerationOptions.IgnoreInaccessible = true;
            EnumerationOptions.ReturnSpecialDirectories = false;
            EnumerationOptions.AttributesToSkip = FileAttributes.None;
        }

        public void Run()
        {
            Log.Instance.AddMessage($"Synchronization started with parameters:\nSource directory:\t\t{SrcPath}\nReplica directory:\t\t{DstPath}\nLog file path:\t\t\t{Log.Instance.LogPath}\nSynchronization interval:\t{SyncInterval}");

            while (true)
            {
                Synchronize();
                Thread.Sleep(SyncInterval);
            }
        }

        private void Synchronize()
        {
            if (!PathsExists())
            {
                // Directories not exists; will retry synchronization in next cycle
                return;
            }

            // Synchronize directories first to prepare folder hierarchy for files - this way we can copy files from Source to Destination without worrying about non existing directories.
            // Since we expect an 'identical copy,' I assumed that empty directories must also be synchronized.
            try
            {
                SynchronizeDirectories();
            }
            catch (Exception)
            {
                // Can not synchronize directories; will retry full synchronization in next cycle
                // We can not try to synchronize files since we didn't correctly synchronized directores - that's why we returning early
                return; 
            }

            try
            {
                SynchronizeFiles();
            }
            catch (Exception)
            {
                // Can not synchronize files; will retry full synchronization in next cycle
                return;
            }
        }

        private void SynchronizeDirectories()
        {
            CreateMissingDirectories();
            DeleteNonExistingDirectories();
        }

        private void SynchronizeFiles()
        {
            CopyOrUpdateFiles();
            DeleteNonExistingFiles();            
        }

        private void CreateMissingDirectories()
        {
            string[] srcFullDirPaths;
            try
            {
                srcFullDirPaths = Directory.GetDirectories(SrcPath, "*", EnumerationOptions);
            }
            catch (IOException)
            {
                // Can not access directories
                throw;
            }

            string[] srcDirPaths = ConvertFullPathsToRelativePaths(srcFullDirPaths, SrcPath);
            for (int i = 0; i < srcDirPaths.Length; i++)
            {
                string srcDirPath = srcDirPaths[i];
                string dstFullDirPath = Path.GetFullPath(srcDirPath, DstPath);

                if (!Directory.Exists(dstFullDirPath))
                {
                    try
                    {
                        // Create directory existing in Source but non existing in Destination
                        Directory.CreateDirectory(dstFullDirPath);
                        Log.Instance.AddMessage($"Created REPLICA directory '{srcDirPath}'");
                    }
                    catch (IOException)
                    {
                        // Can not create directory
                        throw;
                    }
                }
            }
        }

        private void DeleteNonExistingDirectories()
        {
            string[] dstFullDirPaths;
            try
            {
                dstFullDirPaths = Directory.GetDirectories(DstPath, "*", EnumerationOptions);
            }
            catch (IOException)
            {
                // Can not access directories
                throw;
            }

            string[] dstDirPaths = ConvertFullPathsToRelativePaths(dstFullDirPaths, DstPath);
            for (int i = 0; i < dstDirPaths.Length; i++)
            {
                string dstDirPath = dstDirPaths[i];
                string srcFullDirPath = Path.GetFullPath(dstDirPath, SrcPath);

                if (!Directory.Exists(srcFullDirPath))
                {
                    string dstFullDirPath = dstFullDirPaths[i];
                    // Check if still exists first because we might deleted it already while deleting some directory "above" in hierarchy
                    if (Directory.Exists(dstFullDirPath))
                    {
                        try
                        {
                            // Delete directory existing in Destination but non existing in Source
                            Directory.Delete(dstFullDirPath, true);
                            Log.Instance.AddMessage($"Deleted REPLICA directory '{dstDirPath}'");
                        }
                        catch (IOException)
                        {
                            // Can not delete directory
                            throw;
                        }
                    }
                }
            }
        }

        private void CopyOrUpdateFiles()
        {
            string[] srcFullFilePaths;
            try
            {
                srcFullFilePaths = Directory.GetFiles(SrcPath, "*", EnumerationOptions);
            }
            catch (IOException)
            {
                // Can not access Source files
                throw;
            }

            string[] srcFilePaths = ConvertFullPathsToRelativePaths(srcFullFilePaths, SrcPath);
            for (int i = 0; i < srcFilePaths.Length; i++)
            {
                string srcFilePath = srcFilePaths[i];
                string dstFullFilePath = Path.GetFullPath(srcFilePath, DstPath);

                // Check if file exists in Destination directory
                if (Path.Exists(dstFullFilePath))
                {
                    FileComparer fileComparer = new(srcFilePath, SrcPath, DstPath);

                    // Check if file has changed
                    if (!fileComparer.FilesEqual())
                    {
                        try
                        {
                            // Update changed file in Destination directory
                            string srcFullFilePath = srcFullFilePaths[i];
                            File.Copy(srcFullFilePath, dstFullFilePath, true);
                            Log.Instance.AddMessage($"Updated REPLICA file '{srcFilePath}'");
                        }
                        catch (IOException)
                        {
                            // Can not access file
                            // No throwing - skip this file and retry in next synchronization cycle
                        }
                    }
                }
                else
                {
                    // File exists only in Source directory 
                    try
                    {
                        // Copy it to Destination directory
                        string srcFullFilePath = srcFullFilePaths[i];
                        File.Copy(srcFullFilePath, dstFullFilePath);
                        Log.Instance.AddMessage($"Created REPLICA file '{srcFilePath}'");
                    }
                    catch (IOException)
                    {
                        // Can not create file
                        // No throwing - skip this file and retry in next synchronization cycle
                    }
                }
            }
        }

        private void DeleteNonExistingFiles()
        {
            string[] dstFullFilePaths;
            try
            {
                dstFullFilePaths = Directory.GetFiles(DstPath, "*", EnumerationOptions);
            }
            catch (IOException)
            {
                // Can not access Destination files
                throw;
            }

            string[] dstFilePaths = ConvertFullPathsToRelativePaths(dstFullFilePaths, DstPath);
            for (int i = 0; i < dstFilePaths.Length; i++)
            {
                string dstFilePath = dstFilePaths[i];
                string srcFullFilePath = Path.GetFullPath(dstFilePath, SrcPath);

                if (!Path.Exists(srcFullFilePath))
                {
                    // File exists in Destination directory but it was removed from Source directory
                    // Delete it from Destination directory
                    try
                    {
                        string dstFullFilePath = dstFullFilePaths[i];
                        File.Delete(dstFullFilePath);
                        Log.Instance.AddMessage($"Deleted REPLICA file '{dstFilePath}'");
                    }
                    catch (IOException)
                    {
                        // Can not access file
                        // No throwing - skip this file and retry in next synchronization cycle
                    }
                }
            }
        }

        private bool PathsExists()
        {
            return Directory.Exists(SrcPath) && Directory.Exists(DstPath);
        }

        private string[] ConvertFullPathsToRelativePaths(string[] fullPaths, string relativeTo)
        {
            string[] relativePaths = new string[fullPaths.Length];
            for (int i = 0; i < fullPaths.Length; i++)
            {
                relativePaths[i] = Path.GetRelativePath(relativeTo, fullPaths[i]);
            }

            return relativePaths;
        }
    }
}
