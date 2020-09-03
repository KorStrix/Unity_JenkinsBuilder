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

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Jenkins
{
    /// <summary>
    /// 에디터 플레이어 빌드 전 세팅 (빌드 후 되돌리기용)
    /// </summary>
    public class PlayerSetting_Backup
    {
        public string strDefineSymbol { get; private set; }
        public string strProductName { get; private set; }

        public BuildTargetGroup eBuildTargetGroup { get; private set; }

        public PlayerSetting_Backup(BuildTargetGroup eBuildTargetGroup, string strDefineSymbol, string strProductName)
        {
            this.eBuildTargetGroup = eBuildTargetGroup;
            this.strDefineSymbol = strDefineSymbol;
            this.strProductName = strProductName;
        }

        public void Back_To_Origin()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(eBuildTargetGroup, strDefineSymbol);
            PlayerSettings.productName = strProductName;
        }
    }
    
    /// <summary>
    /// 젠킨스 빌드를 위한 스크립트입니다.
    /// </summary>
    public partial class Builder
    {
        const string const_strPrefix_ForDebugLog = "!@#$";

        public enum ECommandLineList
        {
            /// <summary>
            /// 결과물이 나오는 파일 명
            /// </summary>
            filename,

            /// <summary>
            /// 컨피그 파일이 있는 절대 경로
            /// </summary>
            config_path,

            /// <summary>
            /// 결과물이 나오는 절대 경로
            /// </summary>
            output_path,

            /// <summary>
            /// PlayerSetting.android.BundleVersionCode
            /// </summary>
            android_bundle_versioncode,

            /// <summary>
            /// PlayerSetting.android.Version
            /// </summary>
            android_version,

            ios_version,
        }

        public static readonly Dictionary<ECommandLineList, string> const_mapCommandLine =
            new Dictionary<ECommandLineList, string>()
            {
                {ECommandLineList.filename, "-filename"},
                {ECommandLineList.config_path, "-config_path"},
                {ECommandLineList.output_path, "-output_path"},
                {ECommandLineList.android_bundle_versioncode, "-android_versioncode"},
                {ECommandLineList.android_version, "-android_version"},
                {ECommandLineList.ios_version, "-ios_version"}
            };
        
        private static BuildConfig g_pLastConfig;
        
        #region public
        
        [MenuItem("Tools/Build/Create BuildConfig Example File")]
        public static void Create_BuildConfig_Dummy()
        {
            string strContent = JsonUtility.ToJson(new BuildConfig(), true);
            File.WriteAllText(Application.dataPath + "/" + nameof(BuildConfig) + "_Example.json", strContent);
            AssetDatabase.Refresh();
        }

        public static string GetCommandLineArg(string strName)
        {
            string[] arrArgument = Environment.GetCommandLineArgs();
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
            string strPath = GetCommandLineArg(strCommandLine);
            Exception pException = DoTryParsing_JsonFile(strPath, out pOutFile);
            if (pException != null)
            {
                Debug.LogErrorFormat(const_strPrefix_ForDebugLog + " Error - FilePath : {0}, FilePath : {1}\n" +
                                     " Error : {2}", strCommandLine, strPath, pException);
                return false;
            }

            return true;
        }

        public static bool GetFile_From_CommandLine_SO<T>(string strCommandLine, out T pOutFile)
            where T : ScriptableObject
        {
            string strPath = GetCommandLineArg(strCommandLine);
            Exception pException = DoTryParsing_JsonFile_SO(strPath, out pOutFile);
            if (pException != null)
            {
                Debug.LogErrorFormat(const_strPrefix_ForDebugLog + " Error - FilePath : {0}, FilePath : {1}\n" +
                                " Error : {2}", strCommandLine, strPath, pException);
                return false;
            }

            return true;
        }

        public static void GetAppFilePath_FromConfig(BuildConfig pConfig, out string strBuildOutputFolderPath,
            out string strFileName)
        {
            string strBuildOutputFolderPath_CommandLine = GetCommandLineArg(const_mapCommandLine[ECommandLineList.output_path]);
            string strFileName_CommandLine = GetCommandLineArg(const_mapCommandLine[ECommandLineList.filename]);

            strBuildOutputFolderPath = string.IsNullOrEmpty(strBuildOutputFolderPath_CommandLine)
                ? pConfig.strAbsolute_BuildOutputFolderPath
                : strBuildOutputFolderPath_CommandLine;

            if (string.IsNullOrEmpty(strFileName_CommandLine))
            {
                strFileName = pConfig.strFileName;
            }
            else
            {
                strFileName = strFileName_CommandLine;
                pConfig.bUse_DateTime_Suffix = false;
            }
        }


        public static Exception DoTryParsing_JsonFile<T>(string strJsonFilePath, out T pOutFile)
            where T : new()
        {
            pOutFile = default(T);

            try
            {
                string strConfigJson = File.ReadAllText(strJsonFilePath);
                pOutFile = JsonUtility.FromJson<T>(strConfigJson);
            }
            catch (Exception pException)
            {
                return pException;
            }

            return null;
        }

        public static Exception DoTryParsing_JsonFile_SO<T>(string strJsonFilePath, out T pOutFile)
            where T : ScriptableObject
        {
            pOutFile = ScriptableObject.CreateInstance<T>();

            try
            {
                string strConfigJson = File.ReadAllText(strJsonFilePath);
                JsonUtility.FromJsonOverwrite(strConfigJson, pOutFile);
            }
            catch (Exception pException)
            {
                return pException;
            }

            return null;
        }
        
        public static string[] GetEnabled_EditorScenes()
        {
            return EditorBuildSettings.scenes.
                Where(p => p.enabled).
                Select(p => p.path.Replace(".unity", "")).
                ToArray();
        }


        public static void DoBuild(BuildConfig pBuildConfig, string strAbsolute_BuildOutputFolderPath,
            string strFileName, BuildTarget eBuildTarget)
        {
            g_pLastConfig = pBuildConfig;
            
            Process_PreBuild(pBuildConfig, strAbsolute_BuildOutputFolderPath, strFileName, eBuildTarget, out var eBuildTargetGroup, out var strBuildPath);

            BuildPlayerOptions sBuildPlayerOptions = Generate_BuildPlayerOption(pBuildConfig, eBuildTarget, strBuildPath);
            PlayerSetting_Backup pEditorSetting_Backup = SettingBuildConfig_To_EditorSetting(pBuildConfig, eBuildTargetGroup);

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
                BuildReport pReport = BuildPipeline.BuildPlayer(sBuildPlayerOptions);
                PrintBuildResult(strBuildPath, pReport, pReport.summary);
            }
            catch (Exception e)
            {
                Debug.Log(const_strPrefix_ForDebugLog + " Error - " + e);
                throw;
            }

            pEditorSetting_Backup.Back_To_Origin();
            Process_PostBuild(pBuildConfig, eBuildTarget, strAbsolute_BuildOutputFolderPath);

            Debug.LogFormat(const_strPrefix_ForDebugLog + " After Build DefineSymbol Current {0}",
                PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup));
        }
        
        /// <summary>
        /// Command Line으로 실행할 때 partial class file에 있으면 못찾음..
        /// </summary>
        public static void Build_Android()
        {
            if (GetFile_From_CommandLine_SO(const_mapCommandLine[ECommandLineList.config_path], out BuildConfig pConfig))
            {
                GetAppFilePath_FromConfig(pConfig, out string strBuildOutputFolderPath, out string strFileName);
                DoBuild(pConfig, strBuildOutputFolderPath, strFileName, BuildTarget.Android);
            }
        }

        /// <summary>
        /// Command Line으로 실행할 때 partial class file에 있으면 못찾음..
        /// </summary>
        public static void Build_IOS()
        {
            if (GetFile_From_CommandLine_SO(const_mapCommandLine[ECommandLineList.config_path], out BuildConfig pConfig))
            {
                GetAppFilePath_FromConfig(pConfig, out string strBuildOutputFolderPath, out string strFileName);
                DoBuild(pConfig, strBuildOutputFolderPath, strFileName, BuildTarget.iOS);
            }
        }
        
        #endregion public
        
        // ==============================================================================================


#region private
        private static BuildPlayerOptions Generate_BuildPlayerOption(BuildConfig pBuildConfig, BuildTarget eBuildTarget,
            string strBuildPath)
        {
            BuildPlayerOptions sBuildPlayerOptions = new BuildPlayerOptions
            {
                scenes = pBuildConfig.arrBuildSceneNames.Select(p => p += ".unity").ToArray(),
                locationPathName = strBuildPath,
                target = eBuildTarget,
                options = BuildOptions.None
            };

            return sBuildPlayerOptions;
        }


        private static PlayerSetting_Backup SettingBuildConfig_To_EditorSetting(BuildConfig pBuildConfig,
            BuildTargetGroup eBuildTargetGroup)
        {
            string strDefineSymbol_Backup = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(eBuildTargetGroup, pBuildConfig.strDefineSymbol);

            string strProductName_Backup = PlayerSettings.productName;
            PlayerSettings.productName = pBuildConfig.strProductName;

            return new PlayerSetting_Backup(eBuildTargetGroup, strDefineSymbol_Backup, strProductName_Backup);
        }

        private static void Process_PreBuild(BuildConfig pBuildConfig, string strAbsolute_BuildOutputFolderPath,
            string strFileName, BuildTarget eBuildTarget, out BuildTargetGroup eBuildTargetGroup,
            out string strBuildPath)
        {
            eBuildTargetGroup = GetBuildTargetGroup(eBuildTarget);
            strBuildPath = Create_BuildPath(pBuildConfig.bUse_DateTime_Suffix, strAbsolute_BuildOutputFolderPath,
                strFileName);

            switch (eBuildTarget)
            {
                case BuildTarget.Android:
                    BuildSetting_Android(pBuildConfig.pAndroidSetting);
                    strBuildPath += ".apk";
                    break;
                
                case BuildTarget.iOS:
                    BuildSetting_IOS(pBuildConfig.pIOSSetting);
                    break;
            }
        }

        private static string Create_BuildPath(bool bUse_DateTime_Suffix, string strFolderName, string strFileName)
        {
            Debug.LogFormat(const_strPrefix_ForDebugLog + " FolderName : {0}, FileName : {1}", strFolderName,
                strFileName);

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
                string strDateTime = DateTime.Now.ToString("MMdd_HHmm");
                strBuildPath = strBuildPath + "_" + strDateTime;
            }

            return strBuildPath;
        }


        private static void Process_PostBuild(BuildConfig pBuildConfig, BuildTarget eBuildTarget,
            string strAbsolute_BuildOutputFolderPath)
        {
            switch (eBuildTarget)
            {
                case BuildTarget.Android:

                    try
                    {
                        // Mac OS에서 구동 시 Directory.GetFiles함수는 Error가 나기 때문에
                        // DirectoryInfo.GetFiles를 통해 체크
                        DirectoryInfo pDirectory = new DirectoryInfo(strAbsolute_BuildOutputFolderPath);
                        foreach (var pFile in pDirectory.GetFiles())
                        {
                            // IL2CPP 파일로 빌드 시 자동으로 생inf기는 파일, 삭제해도 무방
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


        private static void PrintBuildResult(string strPath, BuildReport pReport, BuildSummary pSummary)
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
                    foreach (var pMessage in pStep.messages)
                    {
                        if (pMessage.type == LogType.Error || pMessage.type == LogType.Exception)
                        {
                            Debug.LogFormat(const_strPrefix_ForDebugLog + " Build Fail Log[{0}] : type : {1}\n" +
                                            " content : {2}", ++iErrorIndex, pMessage.type, pMessage.content);
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
                case BuildTarget.iOS: return BuildTargetGroup.iOS;
            }

            return BuildTargetGroup.Standalone;
        }
        
        #endregion private
    }
}

#endif
