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
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Jenkins
{
    [System.Serializable]
    public class BuildConfig
    {
        public string strAbsolute_BuildOutputFolderPath = Application.dataPath.Replace("/Assets", "") + "/Build";
        public string strFileName = "Build";
        public string strDefineSymbol;
        public string strProductName = PlayerSettings.productName;

        public bool bUse_DateTime_Suffix = true;

        public string[] arrBuildSceneNames = Builder.GetEnabled_EditorScenes();

        [System.Serializable]
        public class AndroidSetting
        {
            public string strFullPackageName = PlayerSettings.applicationIdentifier;

            public string strKeyalias_Name;
            public string strKeyalias_Password;

            public string strKeystore_RelativePath;
            public string strKeystore_Password;

            public bool bUse_IL_TO_CPP_Build = true;
        }

        public AndroidSetting pAndroidSetting = new AndroidSetting();
    }

    public class PlayerSetting_Backup
    {
        public string strDefineSymbol { get; private set; }
        public string strProductName { get; private set; }

        public BuildTargetGroup eBuildTargetGroup { get; private set; }

        public PlayerSetting_Backup(BuildTargetGroup eBuildTargetGroup, string strDefineSymbol, string strProductName)
        {
            this.eBuildTargetGroup = eBuildTargetGroup; this.strDefineSymbol = strDefineSymbol; this.strProductName = strProductName;
        }

        public void Back_To_Origin()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(eBuildTargetGroup, strDefineSymbol);
            PlayerSettings.productName = strProductName;
        }

    }

    #region BuilderWindow

    /// <summary>
    /// 젠킨스 빌더를 유니티 에디터에서 제어하하는 스크립트입니다.
    /// </summary>
    public class JenkinsBuilderWindow : EditorWindow
    {
        BuildConfig _pBuildConfig;
        BuildTarget _eBuildTarget = BuildTarget.Android;

        string _strConfigPath;
        string _strBuildOutputPath;

        [MenuItem("Tools/Build/Show Jenkins Builder Window", priority = -10000)]
        static public void DoShow_Jenkins_Builder_Window()
        {
            // Get existing open window or if none, make a new one:
            JenkinsBuilderWindow pWindow = (JenkinsBuilderWindow)GetWindow(typeof(JenkinsBuilderWindow), false);

            pWindow.minSize = new Vector2(400, 300);
            pWindow.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10f);

            EditorGUI.BeginChangeCheck();
            DrawPath_File("Config", ref _strConfigPath);
            if(EditorGUI.EndChangeCheck())
            {
                System.Exception pException = Builder.DoTryParsing_JsonFile(_strConfigPath, out _pBuildConfig);
                if(pException != null)
                {
                    _strConfigPath = "!! Error !!" + _strConfigPath;
                    Debug.LogError($"Json Parsing Fail Path : {_strConfigPath}\n {pException}", this);
                }
            }

            DrawPath_Folder("Build Output", ref _strBuildOutputPath);
            GUILayout.Space(10f);

            GUI.enabled = _pBuildConfig != null;
            if (GUILayout.Button("Build !"))
            {
                Builder.DoBuild(_pBuildConfig, _strBuildOutputPath, _pBuildConfig.strFileName, _eBuildTarget);
            }
            
            GUILayout.Space(30f);
        }

        private string DrawPath_Folder(string strExplaneName, ref string strFolderPath)
        {
            return DrawPath(strExplaneName, ref strFolderPath, true);
        }

        private string DrawPath_File(string strExplaneName, ref string strFilePath)
        {
            return DrawPath(strExplaneName, ref strFilePath, false);
        }

        private string DrawPath(string strExplaneName, ref string strEditPath, bool bIsFolder)
        {
            GUILayout.BeginHorizontal();

            if (bIsFolder)
                GUILayout.Label($"{strExplaneName} Folder Path : ", GUILayout.Width(150f));
            else
                GUILayout.Label($"{strExplaneName} File Path : ", GUILayout.Width(150f));

            GUILayout.Label(strEditPath);

            if (GUILayout.Button($"Edit {strExplaneName}", GUILayout.Width(150f)))
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

    #endregion

    /// <summary>
    /// 젠킨스 빌드를 위한 스크립트입니다.
    /// </summary>
    public class Builder
    {
        const string const_strPrefix_ForDebugLog = "!@#$";

        [MenuItem("Tools/Build/Create BuildConfig File")]
        static public void Create_BuildConfig_Dummy()
        {
            string strContent = JsonUtility.ToJson(new BuildConfig(), true);
            File.WriteAllText(Application.dataPath + "/" + typeof(BuildConfig).Name + ".json", strContent);
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Build/Android Build Test")]
        static public void Build_Android_Project()
        {
            BuildConfig pConfig = new BuildConfig();
            BuildTargetGroup eBuildTargetGroup = GetBuildTargetGroup(BuildTarget.Android);
            pConfig.strDefineSymbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);

            DoBuild(pConfig, pConfig.strAbsolute_BuildOutputFolderPath, pConfig.strFileName, BuildTarget.Android);
        }

        static public void Build_Android()
        {
            BuildConfig pConfig;
            if (GetFile_From_CommandLine("-config_path", out pConfig))
            {
                string strBuildOutputFolderPath_CommandLine = GetCommandLineArg("-output_path");
                string strFileName_CommandLine = GetCommandLineArg("-filename");

                string strBuildOutputFolderPath_Final = string.IsNullOrEmpty(strBuildOutputFolderPath_CommandLine) ? pConfig.strAbsolute_BuildOutputFolderPath : strBuildOutputFolderPath_CommandLine;
                string strFileName_Final = string.IsNullOrEmpty(strFileName_CommandLine) ? pConfig.strFileName : strFileName_CommandLine;

                DoBuild(pConfig, strBuildOutputFolderPath_Final, strFileName_Final, BuildTarget.Android);
            }
        }

        static public string GetCommandLineArg(string strName)
        {
            string[] arrArgument = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arrArgument.Length; ++i)
            {
                if (arrArgument[i] == strName && arrArgument.Length > i + 1)
                {
                    return arrArgument[i + 1];
                }
            }

            return null;
        }

        static public bool GetFile_From_CommandLine<T>(string strCommandLine, out T pOutFile)
            where T : new()
        {
            string strPath = GetCommandLineArg(strCommandLine);
            System.Exception pException = DoTryParsing_JsonFile(strPath, out pOutFile);
            if (pException != null)
            {
                Debug.LogFormat(const_strPrefix_ForDebugLog + " Error - FilePath : {0}, FilePath : {1}\n" +
                    " Error : {2}", strCommandLine, strPath, pException);
                return false;
            }

            return true;
        }

        static public System.Exception DoTryParsing_JsonFile<T>(string strJsonFilePath, out T pOutFile)
              where T : new()
        {
            pOutFile = default(T);

            try
            {
                string strConfigJson = File.ReadAllText(strJsonFilePath);
                pOutFile = JsonUtility.FromJson<T>(strConfigJson);
            }
            catch (System.Exception pException)
            {
                return pException;
            }

            return null;
        }

        static public string[] GetEnabled_EditorScenes()
        {
            return EditorBuildSettings.scenes.Where(p => p.enabled).Select(p => p.path).ToArray();
        }


        // ==============================================================================================

        public static void DoBuild(BuildConfig pBuildConfig, string strAbsolute_BuildOutputFolderPath, string strFileName, BuildTarget eBuildTarget)
        {
            BuildTargetGroup eBuildTargetGroup;
            string strBuildPath;
            Process_PreBuild(pBuildConfig, strAbsolute_BuildOutputFolderPath, strFileName, eBuildTarget, out eBuildTargetGroup, out strBuildPath);

            BuildPlayerOptions sBuildPlayerOptions = new BuildPlayerOptions();
            sBuildPlayerOptions.scenes = pBuildConfig.arrBuildSceneNames;
            sBuildPlayerOptions.locationPathName = strBuildPath;
            sBuildPlayerOptions.target = eBuildTarget;
            sBuildPlayerOptions.options = BuildOptions.None;

            var pEditorSetting_Backup = SettingToBuildConfig_EditorSetting(pBuildConfig, eBuildTargetGroup);

            Debug.LogFormat(const_strPrefix_ForDebugLog + " Before Build DefineSymbol TargetGroup : {0}\n" +
                "Origin Symbol : {1}\n " +
                "Config : {2} \n" +
                "Current : {3} \n" +
                "strBuildPath : {4}",
                eBuildTargetGroup,
                pEditorSetting_Backup.strDefineSymbol,
                pBuildConfig.strDefineSymbol,
                PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup),
                strBuildPath);

            try
            {
                var pReport = BuildPipeline.BuildPlayer(sBuildPlayerOptions);
                PrintLog(strBuildPath, pReport, pReport.summary);
            }
            catch (Exception e)
            {
                Debug.Log(const_strPrefix_ForDebugLog + " Error - " + e);
            }

            pEditorSetting_Backup.Back_To_Origin();
            Process_PostBuild(pBuildConfig, eBuildTarget);

            Debug.LogFormat(const_strPrefix_ForDebugLog + " After Build DefineSymbol Current {0}", PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup));
        }


        private static PlayerSetting_Backup SettingToBuildConfig_EditorSetting(BuildConfig pBuildConfig, BuildTargetGroup eBuildTargetGroup)
        {
            string strDefineSymbol_Backup = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(eBuildTargetGroup, pBuildConfig.strDefineSymbol);

            string strProductName_Backup = PlayerSettings.productName;
            PlayerSettings.productName = pBuildConfig.strProductName;

            return new PlayerSetting_Backup(eBuildTargetGroup, strDefineSymbol_Backup, strProductName_Backup);
        }

        private static void Process_PreBuild(BuildConfig pBuildConfig, string strAbsolute_BuildOutputFolderPath, string strFileName, BuildTarget eBuildTarget, out BuildTargetGroup eBuildTargetGroup, out string strBuildPath)
        {
            eBuildTargetGroup = GetBuildTargetGroup(eBuildTarget);
            strBuildPath = Create_BuildPath(pBuildConfig.bUse_DateTime_Suffix, strAbsolute_BuildOutputFolderPath, strFileName);

            switch (eBuildTarget)
            {
                case BuildTarget.Android:
                    BuildSetting_Android(pBuildConfig.pAndroidSetting);
                    strBuildPath += ".apk";
                    break;
            }
        }

        private static string Create_BuildPath(bool bUse_DateTime_Suffix, string strFolderName, string strFileName)
        {
            Debug.LogFormat(const_strPrefix_ForDebugLog + " FolderName : {0}, FileName : {1}", strFolderName, strFileName);

            try
            {
                if (Directory.Exists(strFolderName) == false)
                    Directory.CreateDirectory(strFolderName);
            }
            catch (Exception e) 
            {
                Debug.Log(const_strPrefix_ForDebugLog + " Error - Create Directory - " + e);
            }

            string strBuildPath = strFolderName + "/" + strFileName;
            if (bUse_DateTime_Suffix)
            {
                DateTime sDateTimeNow = DateTime.Now;
                string strDateTime = string.Format("{0}_{1}",
                    sDateTimeNow.Month.ToString("D2") + sDateTimeNow.Day.ToString("D2"),
                    sDateTimeNow.Hour.ToString("D2") + sDateTimeNow.Minute.ToString("D2"));

                strBuildPath = strBuildPath + "_" + strDateTime;
            }

            return strBuildPath;
        }


        private static void Process_PostBuild(BuildConfig pBuildConfig, BuildTarget eBuildTarget)
        {
            switch (eBuildTarget)
            {
                case BuildTarget.Android:

                    try
                    {
                        // Mac OS에서 구동 시 Directory.GetFiles함수는 Error가 나기 때문에
                        // DirectoryInfo.GetFiles를 통해 체크
                        DirectoryInfo pDirectory = new DirectoryInfo(pBuildConfig.strAbsolute_BuildOutputFolderPath);
                        foreach (var pFile in pDirectory.GetFiles())
                        {
                            // IL2CPP 파일로 빌드 시 자동으로 생기는 파일, 삭제해도 무방
                            if (pFile.Extension == ".zip" && pFile.Name.Contains("symbols"))
                            {
                                Debug.Log(const_strPrefix_ForDebugLog + " Delete : " + pFile.Name);
                                pFile.Delete();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log(const_strPrefix_ForDebugLog + " Error - " + e);
                    }
                    break;
            }
        }

        private static void BuildSetting_Android(BuildConfig.AndroidSetting pAndroidSetting)
        {
            if (string.IsNullOrEmpty(pAndroidSetting.strFullPackageName) == false)
                PlayerSettings.applicationIdentifier = pAndroidSetting.strFullPackageName;

            PlayerSettings.Android.keyaliasName = pAndroidSetting.strKeyalias_Name;
            PlayerSettings.Android.keyaliasPass = pAndroidSetting.strKeyalias_Password;

            PlayerSettings.Android.keystoreName = Application.dataPath + pAndroidSetting.strKeystore_RelativePath;
            PlayerSettings.Android.keystorePass = pAndroidSetting.strKeystore_Password;

            if (pAndroidSetting.bUse_IL_TO_CPP_Build)
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            else
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);

            Debug.LogFormat(const_strPrefix_ForDebugLog + " Build Setting [Android]\n" +
                "strPackageName : {0}\n" +
                "keyaliasName : {1}, keyaliasPass : {2}\n" +
                "keystoreName : {3}, keystorePass : {4}\n" +
                "bUse_IL_TO_CPP_Build : {5}",
                PlayerSettings.applicationIdentifier,
                PlayerSettings.Android.keyaliasName, PlayerSettings.Android.keyaliasPass,
                PlayerSettings.Android.keystoreName, PlayerSettings.Android.keystorePass,
                pAndroidSetting.bUse_IL_TO_CPP_Build);;
        }

        private static void PrintLog(string strPath, BuildReport pReport, BuildSummary pSummary)
        {
            Debug.LogFormat(const_strPrefix_ForDebugLog + " Path : {0}, Build Result : {1}", strPath, pSummary.result);

            if (pSummary.result == BuildResult.Succeeded)
            {
                Debug.Log(const_strPrefix_ForDebugLog + " Build Succeeded!");
            }
            else if (pSummary.result == BuildResult.Failed)
            {
                int iErrorIndex = 1;
                foreach (var pStep in pReport.steps)
                {
                    for (int i = 0; i < pStep.messages.Length; i++)
                    {
                        if (pStep.messages[i].type == LogType.Error || pStep.messages[i].type == LogType.Exception)
                        {
                            Debug.LogFormat(const_strPrefix_ForDebugLog + " Build Fail Log[{0}] : type : {1}\n" +
                                " content : {2}", ++iErrorIndex, pStep.messages[i].type, pStep.messages[i].content);
                        }
                    }
                }
            }
        }

        private static BuildTargetGroup GetBuildTargetGroup(BuildTarget eBuildTarget)
        {
            switch (eBuildTarget)
            {
                case BuildTarget.Android: return BuildTargetGroup.Android;
            }

            return BuildTargetGroup.Standalone;
        }
    }
}

#endif