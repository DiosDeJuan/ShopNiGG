/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI panel for browsing, uploading and managing documents in the uploads directory.
    /// Provides a scrollable list of uploaded files with info and action buttons.
    /// </summary>
    public class UIDocumentUpload : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static UIDocumentUpload Instance { get; private set; }

        /// <summary>
        /// Container where document list items are spawned.
        /// </summary>
        public Transform contentParent;

        /// <summary>
        /// Prefab for a single document list entry. Expected to contain TMP_Text and Button children.
        /// </summary>
        public GameObject documentItemPrefab;

        /// <summary>
        /// Label showing the number of uploaded documents.
        /// </summary>
        public TMP_Text documentCountLabel;

        /// <summary>
        /// Label showing the path to the uploads directory.
        /// </summary>
        public TMP_Text uploadPathLabel;

        /// <summary>
        /// Button for refreshing the document list.
        /// </summary>
        public Button refreshButton;

        /// <summary>
        /// Button for exporting the current save file.
        /// </summary>
        public Button exportButton;

        /// <summary>
        /// Label for showing status or error messages.
        /// </summary>
        public TMP_Text statusLabel;

        /// <summary>
        /// Clip to play when a document action succeeds, or none if not set.
        /// </summary>
        public AudioClip successClip;

        /// <summary>
        /// Clip to play when a document action fails, or none if not set.
        /// </summary>
        public AudioClip failClip;


        //initialize references
        void Awake()
        {
            Instance = this;
        }


        //initialize variables and event subscriptions
        void Start()
        {
            if (refreshButton) refreshButton.onClick.AddListener(RefreshDocumentList);
            if (exportButton) exportButton.onClick.AddListener(ExportCurrentSave);

            DocumentUploadSystem.onDocumentUploaded += OnDocumentChanged;
            DocumentUploadSystem.onDocumentRemoved += OnDocumentChanged;
            DocumentUploadSystem.onDocumentImported += OnDocumentImported;

            RefreshDocumentList();
        }


        /// <summary>
        /// Clear and rebuild the list of uploaded documents.
        /// </summary>
        public void RefreshDocumentList()
        {
            //clear existing items
            if (contentParent != null)
            {
                for (int i = contentParent.childCount - 1; i >= 0; i--)
                    Destroy(contentParent.GetChild(i).gameObject);
            }

            List<string> files = DocumentUploadSystem.GetUploadedFiles();

            if (documentCountLabel)
                documentCountLabel.text = files.Count + " document" + (files.Count != 1 ? "s" : "");

            if (uploadPathLabel)
                uploadPathLabel.text = DocumentUploadSystem.GetUploadPath();

            for (int i = 0; i < files.Count; i++)
            {
                CreateDocumentEntry(files[i]);
            }

            SetStatus(files.Count > 0 ? "" : "No documents uploaded yet. Place files in the uploads folder.");
        }


        //create a single document list entry in the UI
        private void CreateDocumentEntry(string fileName)
        {
            if (documentItemPrefab == null || contentParent == null)
                return;

            GameObject item = Instantiate(documentItemPrefab, contentParent);
            item.name = fileName;

            //set document info text
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                FileInfo info = DocumentUploadSystem.GetFileInfo(fileName);
                string sizeText = info != null ? FormatFileSize(info.Length) : "unknown";
                label.text = fileName + "  (" + sizeText + ")";
            }

            //set up import button (first button found)
            Button[] buttons = item.GetComponentsInChildren<Button>();
            if (buttons.Length > 0)
            {
                buttons[0].onClick.AddListener(() => ImportDocument(fileName));
            }

            //set up remove button (second button found)
            if (buttons.Length > 1)
            {
                buttons[1].onClick.AddListener(() => RemoveDocument(fileName));
            }
        }


        /// <summary>
        /// Attempt to import a document as save data.
        /// </summary>
        public void ImportDocument(string fileName)
        {
            bool success = DocumentUploadSystem.ImportAsSaveFile(fileName);

            if (success)
            {
                SetStatus("Imported: " + fileName);
                AudioSystem.Play2D(successClip);
            }
            else
            {
                SetStatus("Failed to import: " + fileName);
                AudioSystem.Play2D(failClip);
            }
        }


        /// <summary>
        /// Remove a document from the uploads directory.
        /// </summary>
        public void RemoveDocument(string fileName)
        {
            bool success = DocumentUploadSystem.RemoveFile(fileName);

            if (success)
            {
                SetStatus("Removed: " + fileName);
                AudioSystem.Play2D(successClip);
            }
            else
            {
                SetStatus("Failed to remove: " + fileName);
                AudioSystem.Play2D(failClip);
            }
        }


        /// <summary>
        /// Export the current save file to the uploads directory.
        /// </summary>
        public void ExportCurrentSave()
        {
            bool success = DocumentUploadSystem.ExportSaveFile();

            if (success)
            {
                SetStatus("Save file exported successfully.");
                AudioSystem.Play2D(successClip);
                RefreshDocumentList();
            }
            else
            {
                SetStatus("Failed to export save file.");
                AudioSystem.Play2D(failClip);
            }
        }


        //set status text
        private void SetStatus(string message)
        {
            if (statusLabel)
                statusLabel.text = message;
        }


        //format byte count to human-readable string
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("F1") + " KB";
            return (bytes / (1024f * 1024f)).ToString("F1") + " MB";
        }


        //event handler for document upload or removal
        private void OnDocumentChanged(string fileName)
        {
            RefreshDocumentList();
        }


        //event handler for document import result
        private void OnDocumentImported(bool success)
        {
            if (success)
                SetStatus("Document imported successfully. Restart the game to apply changes.");
        }


        //unsubscribe from events
        void OnDestroy()
        {
            DocumentUploadSystem.onDocumentUploaded -= OnDocumentChanged;
            DocumentUploadSystem.onDocumentRemoved -= OnDocumentChanged;
            DocumentUploadSystem.onDocumentImported -= OnDocumentImported;
        }
    }
}
