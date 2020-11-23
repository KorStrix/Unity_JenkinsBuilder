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
        string _strBuildPath;
        
        [MenuItem(Builder.const_strPrefix_EditorContextMenu + "Show Jenkins Builder Window", priority = -10000)]
        public static void DoShow_Jenkins_Builder_Window()
        {
            JenkinsBuilderWindow pWindow = (JenkinsBuilderWindow) GetWindow(typeof(JenkinsBuilderWindow), false);

            pWindow.minSize = new Vector2(600, 200);
            pWindow.Show();
        }

        private void OnEnable()
        {
            _strConfigPath = EditorPrefs.GetString($"{nameof(JenkinsBuilderWindow)}_{nameof(_strConfigPath)}");
            _strBuildPath = EditorPrefs.GetString($"{nameof(JenkinsBuilderWindow)}_{nameof(_strBuildPath)}");

            CheckConfigPath();
        }

        private void OnGUI()
        {
            GUILayout.Space(10f);

            EditorGUILayout.HelpBox("이 툴은 BuildConfig를 통해 빌드하는 툴입니다.\n\n" +
                                    "사용방법\n" +
                                    "1. Edit Config Json File을 클릭하여 세팅합니다.\n" +
                                    "2. Edit Build Output Path를 눌러 빌드파일 출력 경로를 세팅합니다.\n" +
                                    "3. 플랫폼(Android or iOS) 빌드를 누릅니다.\n" +
                                    "4. 빌드가 되는지 확인 후, 빌드가 완료되면 Open BuildFolder를 눌러 빌드 파일을 확인합니다."
                ,MessageType.Info);


            if (GUILayout.Button("Github"))
            {
                System.Diagnostics.Process.Start("https://github.com/KorStrix/Unity_JenkinsBuilder");
            }
            GUILayout.Space(30f);


            EditorGUI.BeginChangeCheck();
            DrawPath_File("Config Json File", ref _strConfigPath, 
                (strPath) => EditorPrefs.SetString($"{nameof(JenkinsBuilderWindow)}_{nameof(_strConfigPath)}", strPath));
            if (EditorGUI.EndChangeCheck())
            {
                CheckConfigPath();
            }

            DrawPath_Folder("Build", ref _strBuildPath,
                (strPath) => EditorPrefs.SetString($"{nameof(JenkinsBuilderWindow)}_{nameof(_strBuildPath)}", strPath));
            GUILayout.Space(10f);

            bool bConfigIsNotNull = _pBuildConfig != null;

            GUI.enabled = bConfigIsNotNull;
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Android Build !", GUILayout.Height(40f)))
                {
                    Builder.DoBuild(_pBuildConfig, _strBuildPath, _pBuildConfig.strFileName, BuildTarget.Android);
                }

                if (GUILayout.Button("iOS Build !", GUILayout.Height(40f)))
                {
                    Builder.DoBuild(_pBuildConfig, _strBuildPath, _pBuildConfig.strFileName, BuildTarget.iOS);
                }
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            if (GUILayout.Button("Open BuildFolder Path !"))
            {
                System.Diagnostics.Process.Start(_strBuildPath);
            }
        }

        private string DrawPath_Folder(string strExplainName, ref string strFolderPath, Action<string> OnChangePath)
        {
            return DrawPath(strExplainName, ref strFolderPath, OnChangePath, true);
        }

        private string DrawPath_File(string strExplainName, ref string strFilePath, Action<string> OnChangePath)
        {
            return DrawPath(strExplainName, ref strFilePath, OnChangePath, false);
        }

        private string DrawPath(string strExplainName, ref string strEditPath, Action<string> OnChangePath, bool bIsFolder)
        {
            GUILayout.BeginHorizontal();

            if (bIsFolder)
                GUILayout.Label($"{strExplainName} Path : ", GUILayout.Width(150f));
            else
                GUILayout.Label($"{strExplainName} Path : ", GUILayout.Width(150f));

            GUI.enabled = false;
            GUILayout.TextArea(strEditPath, GUILayout.ExpandWidth(true), GUILayout.Height(40f));
            GUI.enabled = true;

            if (GUILayout.Button($"Edit {strExplainName}", GUILayout.Width(150f)))
            {
                string strPath = "";
                if (bIsFolder)
                    strPath = EditorUtility.OpenFolderPanel("Root Folder", "", "");
                else
                    strPath = EditorUtility.OpenFilePanel("File Path", "", "");

                strEditPath = strPath;
                OnChangePath?.Invoke(strPath);
            }

            GUILayout.EndHorizontal();

            return strEditPath;
        }

        private void CheckConfigPath()
        {
            if (string.IsNullOrEmpty(_strConfigPath))
                return;

            Exception pException = Builder.DoTryParsing_JsonFile_SO(_strConfigPath, out _pBuildConfig);
            if (pException != null)
            {
                _strConfigPath = "!! Error !!" + _strConfigPath;
                Debug.LogError($"Json Parsing Fail Path : {_strConfigPath}\n {pException}", this);
            }
        }

    }
}

#endif