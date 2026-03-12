/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// System for uploading, validating and managing document files in a dedicated uploads directory.
    /// Allows importing save game data and other supported file types into the game.
    /// </summary>
    public class DocumentUploadSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static DocumentUploadSystem Instance { get; private set; }

        /// <summary>
        /// The name of the uploads subdirectory inside persistentDataPath.
        /// </summary>
        public const string uploadFolder = "uploads";

        /// <summary>
        /// Maximum allowed file size in bytes (10 MB).
        /// </summary>
        public const long maxFileSize = 10 * 1024 * 1024;

        /// <summary>
        /// Supported file extensions for upload.
        /// </summary>
        public static readonly string[] supportedExtensions = new string[] { ".dat", ".json", ".txt", ".csv" };

        /// <summary>
        /// Fired when a document has been uploaded successfully. Passes the file name.
        /// </summary>
        public static event Action<string> onDocumentUploaded;

        /// <summary>
        /// Fired when a document has been removed. Passes the file name.
        /// </summary>
        public static event Action<string> onDocumentRemoved;

        /// <summary>
        /// Fired when a document import (into save system) finished. Passes success state.
        /// </summary>
        public static event Action<bool> onDocumentImported;


        //initialize references
        void Awake()
        {
            //make sure we keep one instance of this script
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);

            //set static reference
            Instance = this;

            //ensure the uploads directory exists
            EnsureUploadDirectory();
        }


        /// <summary>
        /// Returns the full path to the uploads directory.
        /// </summary>
        public static string GetUploadPath()
        {
            return Path.Combine(Application.persistentDataPath, uploadFolder);
        }


        /// <summary>
        /// Creates the uploads directory if it does not exist yet.
        /// </summary>
        public static void EnsureUploadDirectory()
        {
            string path = GetUploadPath();
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }


        /// <summary>
        /// Checks whether a file extension is in the list of supported types.
        /// </summary>
        public static bool IsSupportedExtension(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            for (int i = 0; i < supportedExtensions.Length; i++)
            {
                if (supportedExtensions[i] == ext)
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Validates a file before uploading. Checks existence, size and extension.
        /// Returns an empty string on success or an error message on failure.
        /// </summary>
        public static string ValidateFile(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
                return "File path is empty.";

            if (!File.Exists(sourcePath))
                return "File not found: " + Path.GetFileName(sourcePath);

            if (!IsSupportedExtension(sourcePath))
                return "Unsupported file type: " + Path.GetExtension(sourcePath);

            FileInfo info = new FileInfo(sourcePath);
            if (info.Length > maxFileSize)
                return "File exceeds maximum size of " + (maxFileSize / (1024 * 1024)) + " MB.";

            if (info.Length == 0)
                return "File is empty.";

            return string.Empty;
        }


        /// <summary>
        /// Upload (copy) a file from the given source path into the uploads directory.
        /// Returns true on success.
        /// </summary>
        public static bool UploadFile(string sourcePath)
        {
            string error = ValidateFile(sourcePath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning("DocumentUploadSystem: " + error);
                return false;
            }

            EnsureUploadDirectory();

            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(GetUploadPath(), fileName);

            try
            {
                File.Copy(sourcePath, destPath, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("DocumentUploadSystem: Failed to upload file - " + e.Message);
                return false;
            }

            onDocumentUploaded?.Invoke(fileName);
            return true;
        }


        /// <summary>
        /// Upload a file from raw byte content. Validates extension and size.
        /// Returns true on success.
        /// </summary>
        public static bool UploadFileFromBytes(string fileName, byte[] data)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning("DocumentUploadSystem: File name is empty.");
                return false;
            }

            if (!IsSupportedExtension(fileName))
            {
                Debug.LogWarning("DocumentUploadSystem: Unsupported file type: " + Path.GetExtension(fileName));
                return false;
            }

            if (data == null || data.Length == 0)
            {
                Debug.LogWarning("DocumentUploadSystem: File data is empty.");
                return false;
            }

            if (data.Length > maxFileSize)
            {
                Debug.LogWarning("DocumentUploadSystem: File exceeds maximum size.");
                return false;
            }

            EnsureUploadDirectory();

            string destPath = Path.Combine(GetUploadPath(), fileName);
            try
            {
                File.WriteAllBytes(destPath, data);
            }
            catch (Exception e)
            {
                Debug.LogWarning("DocumentUploadSystem: Failed to write file - " + e.Message);
                return false;
            }

            onDocumentUploaded?.Invoke(fileName);
            return true;
        }


        /// <summary>
        /// Remove an uploaded document from the uploads directory.
        /// Returns true on success.
        /// </summary>
        public static bool RemoveFile(string fileName)
        {
            string filePath = Path.Combine(GetUploadPath(), fileName);
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("DocumentUploadSystem: File does not exist: " + fileName);
                return false;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("DocumentUploadSystem: Failed to remove file - " + e.Message);
                return false;
            }

            onDocumentRemoved?.Invoke(fileName);
            return true;
        }


        /// <summary>
        /// Returns a list of all uploaded file names in the uploads directory.
        /// </summary>
        public static List<string> GetUploadedFiles()
        {
            EnsureUploadDirectory();

            List<string> files = new List<string>();
            string[] entries = Directory.GetFiles(GetUploadPath());

            for (int i = 0; i < entries.Length; i++)
            {
                string ext = Path.GetExtension(entries[i]).ToLowerInvariant();
                for (int j = 0; j < supportedExtensions.Length; j++)
                {
                    if (supportedExtensions[j] == ext)
                    {
                        files.Add(Path.GetFileName(entries[i]));
                        break;
                    }
                }
            }

            return files;
        }


        /// <summary>
        /// Returns file info (name, size in bytes, last modified) for an uploaded document.
        /// Returns null if the file does not exist.
        /// </summary>
        public static FileInfo GetFileInfo(string fileName)
        {
            string filePath = Path.Combine(GetUploadPath(), fileName);
            if (!File.Exists(filePath))
                return null;

            return new FileInfo(filePath);
        }


        /// <summary>
        /// Attempt to import an uploaded .dat save file into the game save system.
        /// Validates that the file contains valid JSON game data before importing.
        /// Returns true on success.
        /// </summary>
        public static bool ImportAsSaveFile(string fileName)
        {
            string filePath = Path.Combine(GetUploadPath(), fileName);
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("DocumentUploadSystem: File not found for import: " + fileName);
                onDocumentImported?.Invoke(false);
                return false;
            }

            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext != ".dat" && ext != ".json")
            {
                Debug.LogWarning("DocumentUploadSystem: Only .dat and .json files can be imported as save data.");
                onDocumentImported?.Invoke(false);
                return false;
            }

            try
            {
                byte[] dataAsBytes = File.ReadAllBytes(filePath);
                string dataString = Encoding.ASCII.GetString(dataAsBytes);

                //validate that it is parsable JSON
                JSONNode parsed = JSON.Parse(dataString);
                if (parsed == null)
                {
                    Debug.LogWarning("DocumentUploadSystem: File does not contain valid data.");
                    onDocumentImported?.Invoke(false);
                    return false;
                }

                //copy the uploaded file to the save location
                string savePath = Application.persistentDataPath + "/" + SaveGameSystem.fileKey + SaveGameSystem.fileExt;
                File.Copy(filePath, savePath, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("DocumentUploadSystem: Import failed - " + e.Message);
                onDocumentImported?.Invoke(false);
                return false;
            }

            onDocumentImported?.Invoke(true);
            return true;
        }


        /// <summary>
        /// Export the current save file into the uploads directory for sharing.
        /// Returns true on success.
        /// </summary>
        public static bool ExportSaveFile(string exportName = "")
        {
            string savePath = Application.persistentDataPath + "/" + SaveGameSystem.fileKey + SaveGameSystem.fileExt;
            if (!File.Exists(savePath))
            {
                Debug.LogWarning("DocumentUploadSystem: No save file found to export.");
                return false;
            }

            EnsureUploadDirectory();

            if (string.IsNullOrEmpty(exportName))
                exportName = "save_export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + SaveGameSystem.fileExt;

            string destPath = Path.Combine(GetUploadPath(), exportName);
            try
            {
                File.Copy(savePath, destPath, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("DocumentUploadSystem: Export failed - " + e.Message);
                return false;
            }

            onDocumentUploaded?.Invoke(exportName);
            return true;
        }
    }
}
