/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEditor;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Our about/help/support editor window.
    /// </summary>
    public class AboutEditor : EditorWindow
    {
        [MenuItem("Window/Store Simulator/About")]
        static void Init()
        {
            AboutEditor aboutWindow = (AboutEditor)EditorWindow.GetWindowWithRect
                    (typeof(AboutEditor), new Rect(0, 0, 300, 320), false, "About");
            aboutWindow.Show();
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(70);
            GUILayout.Label("Store Simulator", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(70);
            GUILayout.Label("by FLOBUK");
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.Label("Info", EditorStyles.boldLabel);
            if (GUILayout.Button("Homepage"))
            {
                Help.BrowseURL("https://flobuk.com");
            }
            GUILayout.Space(5);

            GUILayout.Label("Support", EditorStyles.boldLabel);
            if (GUILayout.Button("Online Documentation"))
            {
                Help.BrowseURL("https://flobuk.gitlab.io/assets/docs/unity/storesim/");
            }
            if (GUILayout.Button("Scripting Reference"))
            {
                Help.BrowseURL("https://flobuk.gitlab.io/assets/docs/unity/storesim/api/");
            }
            if (GUILayout.Button("Support Forum"))
            {
                Help.BrowseURL("https://discussions.unity.com/t/1592952");
            }
            if (GUILayout.Button("Discord"))
            {
                Help.BrowseURL("https://discord.gg/E8DPRe25MS");
            }
            GUILayout.Space(5);

            GUILayout.Label("Support me!", EditorStyles.boldLabel);
            if (GUILayout.Button("Review Asset"))
            {
                Help.BrowseURL("https://assetstore.unity.com/packages/slug/309463?aid=1011lGiF&pubref=editor_storesim");
            }
            if (GUILayout.Button("Donation"))
            {
                Help.BrowseURL("https://flobuk.com");
            }
        }
    }
}