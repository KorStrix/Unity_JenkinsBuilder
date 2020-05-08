#region Header

/*	============================================
 *	작성자 : Strix
 *	작성일 : 2019-10-21 오후 6:42:02
 *	개요 : 
 *	
 *	에디터 폴더에 있어야 정상 동작합니다.
 *	
 *	참고한 원본 코드 링크
 *	https://slway000.tistory.com/74
 *	https://smilejsu.tistory.com/1528
   ============================================ */

#endregion Header

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor.Callbacks;

#if UNITY_EDITOR
using UnityEditor;

namespace Jenkins
{
    /// <summary>
    /// 젠킨스 빌더를 유니티 에디터에서 제어하하는 스크립트입니다.
    /// </summary>
    public class JenkinsBuilderWindow : EditorWindow
    {
        BuildConfig _pBuildConfig;

        string _strConfigPath;
        string _strBuildOutputPath;

        [MenuItem("Tools/Build/Show Jenkins Builder Window", priority = -10000)]
        public static void DoShow_Jenkins_Builder_Window()
        {
            // Get existing open window or if none, make a new one:
            JenkinsBuilderWindow pWindow = (JenkinsBuilderWindow) GetWindow(typeof(JenkinsBuilderWindow), false);

            pWindow.minSize = new Vector2(400, 300);
            pWindow.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10f);

            EditorGUI.BeginChangeCheck();
            DrawPath_File("Config", ref _strConfigPath);
            if (EditorGUI.EndChangeCheck())
            {
                Exception pException = Builder.DoTryParsing_JsonFile_SO(_strConfigPath, out _pBuildConfig);
                if (pException != null)
                {
                    _strConfigPath = "!! Error !!" + _strConfigPath;
                    Debug.LogError($"Json Parsing Fail Path : {_strConfigPath}\n {pException}", this);
                }
            }

            DrawPath_Folder("Build Output", ref _strBuildOutputPath);
            GUILayout.Space(10f);

            bool bConfigIsNotNull = _pBuildConfig != null;
            GUI.enabled = bConfigIsNotNull;
            
            if (GUILayout.Button("Android Build !") && bConfigIsNotNull)
            {
                Builder.DoBuild(_pBuildConfig, _strBuildOutputPath, _pBuildConfig.strFileName, BuildTarget.Android);
            }

            if (GUILayout.Button("IOS Build !") && bConfigIsNotNull)
            {
                Builder.DoBuild(_pBuildConfig, _strBuildOutputPath, _pBuildConfig.strFileName, BuildTarget.iOS);
            }
            
            GUILayout.Space(30f);
        }

        private string DrawPath_Folder(string strExplainName, ref string strFolderPath)
        {
            return DrawPath(strExplainName, ref strFolderPath, true);
        }

        private string DrawPath_File(string strExplainName, ref string strFilePath)
        {
            return DrawPath(strExplainName, ref strFilePath, false);
        }

        private string DrawPath(string strExplainName, ref string strEditPath, bool bIsFolder)
        {
            GUILayout.BeginHorizontal();

            if (bIsFolder)
                GUILayout.Label($"{strExplainName} Folder Path : ", GUILayout.Width(150f));
            else
                GUILayout.Label($"{strExplainName} File Path : ", GUILayout.Width(150f));

            GUILayout.Label(strEditPath);

            if (GUILayout.Button($"Edit {strExplainName}", GUILayout.Width(150f)))
            {
                string strPath = "";
                if (bIsFolder)
                    strPath = EditorUtility.OpenFolderPanel("Root Folder", "", "");
                else
                    strPath = EditorUtility.OpenFilePanel("File Path", "", "");

                strEditPath = strPath;
            }

            GUILayout.EndHorizontal();

            return strEditPath;
        }
    }
}

#endif