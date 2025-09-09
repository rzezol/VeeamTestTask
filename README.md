# VeeamTestTask
Program that synchronizes two folders: source and replica.

**VeeamTestTask** is a simple folder synchronizer written in C#. It creates an identical copy of a source folder (`source`) in a destination folder (`replica`) and keeps them in sync periodically.

## Features

- One-way synchronization (`source` → `replica`)
- Handles nested directories
- Updates changed files, copies new files, deletes removed files/folders
- Compares files by size and content
- Logs all operations to console and log file

## Usage
```bash
VeeamTestTask.exe --src_path="C:\Source" --dst_path="C:\Replica" --log_path="C:\log.txt" --sync_interval=10

--src_path – source folder path
--dst_path – destination (replica) folder path
--log_path – log file path
--sync_interval – sync interval in seconds
```
## Example
After running the program, the Replica folder will mirror the Source folder. Changes in Source are reflected in Replica in each sync cycle.
