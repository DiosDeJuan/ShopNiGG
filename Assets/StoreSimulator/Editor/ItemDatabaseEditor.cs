/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.IO;
using UnityEngine;
using UnityEditor;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Editor script for updating the references on ItemDatabase automatically.
    /// </summary>
    public class ItemDatabaseEditor : EditorWindow
    {         
        [MenuItem("Window/Store Simulator/Update ItemDatabase")]
        public static void Init()
        {                     
            ItemDatabase database = FindAnyObjectByType<ItemDatabase>();

            if (database == null)
            {
                EditorUtility.DisplayDialog("ItemDatabase component not found!",
                                            "Please make sure to have an ItemDatabase component in the scene.", "Ok");
                return;
            }

            string path = EditorUtility.OpenFolderPanel("Select folder with ScriptableObjects", "", "");
            path = path.Replace(Application.dataPath, "Assets");
            string[] files = Directory.GetFiles(path, "*.asset", SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                Undo.RecordObject(database, "Update ItemDatabase");
                database.purchasables.Clear();
            }

            for(int i = 0; i < files.Length; i++)
            {
                PurchasableScriptableObject scriptable = AssetDatabase.LoadAssetAtPath<PurchasableScriptableObject>(files[i]);
                if(scriptable != null)
                    database.purchasables.Add(scriptable);
            }

            EditorUtility.SetDirty(database);
            EditorUtility.DisplayDialog("Update complete",
                                        "Your ItemDatabase has been updated.\nPlease apply the changes to your prefab if necessary.", "Ok");
        }
    }
}