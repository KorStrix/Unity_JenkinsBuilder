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

        public string[] arrBuildSceneNames = Builder.FindEnabled_EditorScenes();

        [System.Serializable]
        public class AndroidSetting
        {
            public string strFullPackageName = PlayerSettings.applicationIdentifier;

            public string strKeyalias_Name;
            public string strKeyalias_Password;

            public string strKeystore_RelativePath;
            public string strKeystore_Password;
        }

        public AndroidSetting pAndroidSetting = new AndroidSetting();
    }

    /// <summary>
    /// 젠킨스 빌드를 위한 스크립트입니다.
    /// </summary>
    class Builder
    {
        const string const_strPrefix_ForDebugLog = "!@#$";

        [MenuItem("Build/Create BuildConfig File")]
        static public void Create_BuildConfig()
        {
            string strContent = JsonUtility.ToJson(new BuildConfig(), true);
            File.WriteAllText(Application.dataPath + "/" + typeof(BuildConfig).Name + ".json", strContent); 
            AssetDatabase.Refresh();
        }

        [MenuItem("Build/Android Build Test", priority = 10000)]
        static public void Build_Android_Test()
        {
            BuildConfig pConfig = new BuildConfig();
            BuildTargetGroup eBuildTargetGroup = GetBuildTargetGroup(BuildTarget.Android);
            pConfig.strDefineSymbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);

            Build(pConfig, BuildTarget.Android);
        }


        static public void Build_Android()
        {
            BuildConfig pConfig;
            if(GetFile_From_CommandLine("-config_path", out pConfig))
                Build(pConfig, BuildTarget.Android);
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

        public static bool GetFile_From_CommandLine<T>(string strCommandLine, out T pOutFile)
            where T : new()
        {
            pOutFile = new T();
            string strPath = GetCommandLineArg(strCommandLine);

            try
            {
                string strConfigJson = File.ReadAllText(strPath);
                pOutFile = JsonUtility.FromJson<T>(strConfigJson);
            }
            catch (Exception e)
            {
                Debug.LogFormat(const_strPrefix_ForDebugLog + " Error - CommandLine : {0}, FilePath : {1}\n" +
                                " Error : {2}", strCommandLine, strPath, e);

                return false;
            }

            return true;
        }

        // ==============================================================================================

        private static void Build(BuildConfig pBuildConfig, BuildTarget eBuildTarget)
        {
            BuildTargetGroup eBuildTargetGroup;
            string strBuildPath;
            Process_PreBuild(pBuildConfig, eBuildTarget, out eBuildTargetGroup, out strBuildPath);

            BuildPlayerOptions sBuildPlayerOptions = new BuildPlayerOptions();
            sBuildPlayerOptions.scenes = pBuildConfig.arrBuildSceneNames;
            sBuildPlayerOptions.locationPathName = strBuildPath;
            sBuildPlayerOptions.target = eBuildTarget;
            sBuildPlayerOptions.options = BuildOptions.None;

            string strDefineSymbol_Backup = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(eBuildTargetGroup, pBuildConfig.strDefineSymbol);

            Debug.LogFormat(const_strPrefix_ForDebugLog + " Before Build DefineSymbol TargetGroup : {0}\n" +
                "Origin Symbol : {1}\n " +
                "Config : {2} \n" +
                "Current : {3}",
                eBuildTargetGroup, strDefineSymbol_Backup, pBuildConfig.strDefineSymbol, PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup));

            try
            {
                var pReport = BuildPipeline.BuildPlayer(sBuildPlayerOptions);
                PrintLog(strBuildPath, pReport, pReport.summary);
            }
            catch (Exception e)
            {
                Debug.Log(const_strPrefix_ForDebugLog + " Error - " + e);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(eBuildTargetGroup, strDefineSymbol_Backup);
            Process_PostBuild(pBuildConfig, eBuildTarget);

            Debug.LogFormat(const_strPrefix_ForDebugLog + " After Build DefineSymbol Current {0}", PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup));
        }

        private static void Process_PreBuild(BuildConfig pBuildConfig, BuildTarget eBuildTarget, out BuildTargetGroup eBuildTargetGroup, out string strBuildPath)
        {
            PlayerSettings.productName = pBuildConfig.strProductName;

            eBuildTargetGroup = GetBuildTargetGroup(eBuildTarget);
            strBuildPath = Create_BuildPath(pBuildConfig.bUse_DateTime_Suffix, pBuildConfig.strAbsolute_BuildOutputFolderPath, pBuildConfig.strFileName);

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
                        foreach(var pFile in pDirectory.GetFiles())
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
            if(string.IsNullOrEmpty(pAndroidSetting.strFullPackageName) == false)
                PlayerSettings.applicationIdentifier = pAndroidSetting.strFullPackageName;

            PlayerSettings.Android.keyaliasName = pAndroidSetting.strKeyalias_Name;
            PlayerSettings.Android.keyaliasPass = pAndroidSetting.strKeyalias_Password;

            PlayerSettings.Android.keystoreName = Application.dataPath + pAndroidSetting.strKeystore_RelativePath;
            PlayerSettings.Android.keystorePass = pAndroidSetting.strKeystore_Password;

            Debug.LogFormat(const_strPrefix_ForDebugLog + " Build Setting [Android]\n" +
                "strPackageName : {0}\n" +
                "keyaliasName : {1}, keyaliasPass : {2}\n" +
                "keystoreName : {3}, keystorePass : {4}\n",
                PlayerSettings.applicationIdentifier,
                PlayerSettings.Android.keyaliasName, PlayerSettings.Android.keyaliasPass,
                PlayerSettings.Android.keystoreName, PlayerSettings.Android.keystorePass);
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
                        if(pStep.messages[i].type == LogType.Error || pStep.messages[i].type == LogType.Exception)
                        {
                            Debug.LogFormat(const_strPrefix_ForDebugLog + " Build Fail Log[{0}] : type : {1}\n" +
                                " content : {2}", ++iErrorIndex, pStep.messages[i].type, pStep.messages[i].content);
                        }
                    }
                }
            }
        }

        public static string[] FindEnabled_EditorScenes()
        {
            List<string> listEditorScenes = new List<string>();
            foreach (EditorBuildSettingsScene pScene in EditorBuildSettings.scenes)
            {
                if (!pScene.enabled)
                    continue;
                listEditorScenes.Add(pScene.path);
            }

            return listEditorScenes.ToArray();
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