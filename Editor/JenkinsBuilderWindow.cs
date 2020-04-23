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
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Jenkins
{
    [Serializable]
    public class BuildConfig
    {
        /// <summary>
        /// 출력할 파일 명, 젠킨스에서 -filename (filename:string) 로 설정가능
        /// </summary>
        public string strFileName = "Build";

        public string strDefineSymbol;

        /// <summary>
        /// 설치한 디바이스에 표기될 이름
        /// </summary>
        public string strProductName = PlayerSettings.productName;

        /// <summary>
        /// 빌드에 포함할 씬들
        /// </summary>
        public string[] arrBuildSceneNames = Builder.GetEnabled_EditorScenes();

        [Serializable]
        public class AndroidSetting
        {
            // 예시) com.CompanyName.ProductName 
            public string strFullPackageName = PlayerSettings.applicationIdentifier;

            public string strKeyalias_Name;
            public string strKeyalias_Password;

            public string strKeystore_RelativePath;
            public string strKeystore_Password;

            /// <summary>
            /// CPP 빌드를 할지 체크, CPP빌드는 오래 걸리므로 Test빌드가 아닌 Alpha 빌드부터 하는걸 권장
            /// </summary>
            public bool bUse_IL_TO_CPP_Build = false;
        }

        public AndroidSetting pAndroidSetting = new AndroidSetting();


        /// <summary>
        /// 유니티 -> XCode Export -> .ipa 에 필요한 모든 설정
        /// </summary>
        [Serializable]
        public class IOSSetting
        {
            // 애플 개발자 사이트에서 조회 가능, 숫자랑 영어로 된거
            public string strAppleTeamID;

            public string strProvisioningProfileName;
            public string strAppleProvisioningProfileID;

            public string strEntitlementsFileName_Without_ExtensionName;
            
            /// <summary>
            /// 유니티 Asset 경로에서 XCode Project로 카피할 파일 목록, 확장자까지 작성해야 합니다
            /// </summary>
            public string[] arrCopy_AssetFilePath_To_XCodeProjectPath;

            /// <summary>
            /// XCode 프로젝트에 추가할 Framework, 확장자까지 작성해야 합니다
            /// </summary>
            public string[] arrXCode_Framework_Add;

            public string[] arrXCode_OTHER_LDFLAGS_Add;
            public string[] arrXCode_OTHER_LDFLAGS_Remove;

        }

        public IOSSetting pIOSSetting = new IOSSetting();


        // 출력할 폴더 및 파일은 Jenkins에서 처리할 예정
        [Obsolete("Jenkins에서 CommandLine으로 처리할 예정")]
        public string strAbsolute_BuildOutputFolderPath = Application.dataPath.Replace("/Assets", "") + "/Build";

        [Obsolete("Jenkins에서 CommandLine으로 처리할 예정")]
        public bool bUse_DateTime_Suffix = true;
    }

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
                Exception pException = Builder.DoTryParsing_JsonFile(_strConfigPath, out _pBuildConfig);
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
            if (GUILayout.Button("Build !") && bConfigIsNotNull)
            {
                Builder.DoBuild(_pBuildConfig, _strBuildOutputPath, _pBuildConfig.strFileName, _eBuildTarget);
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

    #endregion BuilderWindow

    /// <summary>
    /// 젠킨스 빌드를 위한 스크립트입니다.
    /// </summary>
    public class Builder
    {
        const string const_CommandLine_FileName = "-filename";
        const string const_CommandLine_ConfigFilePath = "-config_path";
        const string const_CommandLine_OutputPath = "-output_path";
        const string const_strPrefix_ForDebugLog = "!@#$";

        private static BuildConfig g_pLastConfig;
        
        #region public
        
        [MenuItem("Tools/Build/Create BuildConfig Example File")]
        public static void Create_BuildConfig_Dummy()
        {
            string strContent = JsonUtility.ToJson(new BuildConfig(), true);
            File.WriteAllText(Application.dataPath + "/" + nameof(BuildConfig) + "_Example.json", strContent);
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Build/Build Test - Android")]
        public static void Build_Test_Android()
        {
            BuildConfig pConfig = new BuildConfig();
            BuildTargetGroup eBuildTargetGroup = GetBuildTargetGroup(BuildTarget.Android);
            pConfig.strDefineSymbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);

            DoBuild(pConfig, pConfig.strAbsolute_BuildOutputFolderPath, pConfig.strFileName, BuildTarget.Android);
        }

        [MenuItem("Tools/Build/Build Test - IOS")]
        public static void Build_Test_IOS()
        {
            BuildConfig pConfig = new BuildConfig();
            BuildTargetGroup eBuildTargetGroup = GetBuildTargetGroup(BuildTarget.iOS);
            pConfig.strDefineSymbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);

            DoBuild(pConfig, pConfig.strAbsolute_BuildOutputFolderPath, pConfig.strFileName, BuildTarget.iOS);
        }

        public static void Build_Android()
        {
            if (GetFile_From_CommandLine(const_CommandLine_ConfigFilePath, out BuildConfig pConfig))
            {
                GetPath_FromConfig(pConfig, out string strBuildOutputFolderPath, out string strFileName);
                DoBuild(pConfig, strBuildOutputFolderPath, strFileName, BuildTarget.Android);
            }
        }

        public static void Build_IOS()
        {
            if (GetFile_From_CommandLine(const_CommandLine_ConfigFilePath, out BuildConfig pConfig))
            {
                GetPath_FromConfig(pConfig, out string strBuildOutputFolderPath, out string strFileName);
                DoBuild(pConfig, strBuildOutputFolderPath, strFileName, BuildTarget.iOS);
            }
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
                Debug.LogFormat(const_strPrefix_ForDebugLog + " Error - FilePath : {0}, FilePath : {1}\n" +
                                " Error : {2}", strCommandLine, strPath, pException);
                return false;
            }

            return true;
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

        public static string[] GetEnabled_EditorScenes()
        {
            return EditorBuildSettings.scenes.Where(p => p.enabled).Select(p => p.path).ToArray();
        }


        public static void DoBuild(BuildConfig pBuildConfig, string strAbsolute_BuildOutputFolderPath,
            string strFileName, BuildTarget eBuildTarget)
        {
            g_pLastConfig = pBuildConfig;
            
            Process_PreBuild(pBuildConfig, strAbsolute_BuildOutputFolderPath, strFileName, eBuildTarget,
                out var eBuildTargetGroup, out var strBuildPath);

            BuildPlayerOptions sBuildPlayerOptions =
                Generate_BuildPlayerOption(pBuildConfig, eBuildTarget, strBuildPath);
            PlayerSetting_Backup pEditorSetting_Backup =
                SettingBuildConfig_To_EditorSetting(pBuildConfig, eBuildTargetGroup);

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
            }

            pEditorSetting_Backup.Back_To_Origin();
            Process_PostBuild(pBuildConfig, eBuildTarget, strAbsolute_BuildOutputFolderPath);

            Debug.LogFormat(const_strPrefix_ForDebugLog + " After Build DefineSymbol Current {0}",
                PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup));
        }
        #endregion public
        
        // ==============================================================================================

        [PostProcessBuild(999999)]
        public static void OnPostprocessBuild(BuildTarget eBuildTarget, string strPath)
        {
            Debug.Log($"{const_strPrefix_ForDebugLog} OnPostprocessBuild - BuildTarget : {eBuildTarget} strPath : {strPath}");
            if (eBuildTarget != BuildTarget.iOS)
                return;

            Init_XCodeProject(strPath);
            SetupPlist(strPath);
        }
        
        // ==============================================================================================


#region private
        /// <summary>
        /// IOS용 XCode Initialize
        /// </summary>
        /// <param name="strXCodeProjectPath"></param>
        private static void Init_XCodeProject(string strXCodeProjectPath)
        {
            BuildConfig.IOSSetting pIOSSetting = g_pLastConfig.pIOSSetting;
            var projectPath = strXCodeProjectPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
            
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Init_XCodeProject)} Start - {nameof(strXCodeProjectPath)} : {strXCodeProjectPath}\n" +
                      $"projectPath : {projectPath}");
            
#if UNITY_IOS
            PBXProject pPBXProject = new PBXProject();
            pPBXProject.ReadFromFile(projectPath);
            
            string strTargetGuid = pPBXProject.TargetGuidByName("Unity-iPhone");
 
            
            // Set Apple Team ID
            pPBXProject.SetTeamId(strTargetGuid, pIOSSetting.strAppleTeamID);
            
            
            // Copy File Asset To XCode Project
            foreach(var strFilePath in pIOSSetting.arrCopy_AssetFilePath_To_XCodeProjectPath)
                CopyFile_Asset_To_XCode(strXCodeProjectPath, strFilePath);

            
            // Add XCode Framework
            foreach (string strFramework in pIOSSetting.arrXCode_Framework_Add)
            {
                pPBXProject.AddFrameworkToProject(strTargetGuid, strFramework, false);
                Debug.Log($"{const_strPrefix_ForDebugLog} Add Framework \"{strFramework}\" to XCode Project");
            }
            

            // Set XCode OTHER_LDFLAGS
            pPBXProject.UpdateBuildProperty(strTargetGuid, "OTHER_LDFLAGS", pIOSSetting.arrXCode_OTHER_LDFLAGS_Add, pIOSSetting.arrXCode_OTHER_LDFLAGS_Remove);
            
            #region Sample
            // // Sample of setting build property
            // project.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
            
            // Sample of setting compile flags
            // var guid = pbxProject.FindFileGuidByProjectPath("Classes/UI/Keyboard.mm");
            // var flags = pbxProject.GetCompileFlagsForFile(targetGuid, guid);
            // flags.Add("-fno-objc-arc");
            // pbxProject.SetCompileFlagsForFile(targetGuid, guid, flags);
            #endregion Sample

            string strTargetEntitlementsFilePath = strXCodeProjectPath + "/" + pIOSSetting.strEntitlementsFileName_Without_ExtensionName + ".entitlements";
            pPBXProject.AddCapability(strTargetGuid, PBXCapabilityType.PushNotifications, strTargetEntitlementsFilePath, true);
            pPBXProject.AddCapability(strTargetGuid, PBXCapabilityType.InAppPurchase, null, true);
            pPBXProject.AddCapability(strTargetGuid, PBXCapabilityType.GameCenter, null, true);

            pPBXProject.AddBuildProperty(strTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)");
            
            SetIOS_AuthToken(pPBXProject, strTargetGuid);

            // Apply settings
            File.WriteAllText(projectPath, pPBXProject.WriteToString());
#else
            string a = "", b = null;
            if (a.Equals(b))
            {
            }

            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Init_XCodeProject)} - Not Define Symbol is Not IOS");
#endif       
        }

#if UNITY_IOS
        private static void SetIOS_AuthToken(PBXProject project, string strTargetGuid)
        {
            var token = project.GetBuildPropertyForAnyConfig(strTargetGuid, "USYM_UPLOAD_AUTH_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                token = "FakeToken";
            }

            project.SetBuildProperty(strTargetGuid, "USYM_UPLOAD_AUTH_TOKEN", token);
        }
#endif

        private static void CopyFile_Asset_To_XCode(string strXCodeProjectPath, string strFilePath)
        {
            string strFileName = strFilePath;
            int iLastIndex = strFilePath.LastIndexOf("/");
            if(iLastIndex != -1)
                strFileName = strFilePath.Substring(iLastIndex + 1);
            
            string strFilePath_Origin = Application.dataPath + "/" + strFilePath;
            string strFilePath_Dest = strXCodeProjectPath + "/" + strFileName;

            try
            {
                FileUtil.DeleteFileOrDirectory(strFilePath_Dest);
                
                
                FileUtil.CopyFileOrDirectory(strFilePath_Origin, strFilePath_Dest);
            }
            catch (Exception e)
            {
                Debug.Log($"{const_strPrefix_ForDebugLog} Copy File Error FileName : \"{strFileName}\" // \"{strFilePath_Origin}\" to \"{strFilePath_Dest}\"" + e);
            }

            Debug.Log($"{const_strPrefix_ForDebugLog} Copy File FileName : \"{strFileName}\" // \"{strFilePath_Origin}\" to \"{strFilePath_Dest}\"");
        }

        private static void SetupPlist(string strXCodeProjectPath)
        {
            Debug.Log($"{nameof(SetupPlist)} Start - {nameof(strXCodeProjectPath)} : {strXCodeProjectPath}");
            
            // Property List(.plist) Default Name
            const string strInfoPlistName = "Info.plist";

#if UNITY_IOS
            var str_plistPath = Path.Combine(strXCodeProjectPath, strInfoPlistName);
            var p_plistDocument = new PlistDocument();
            p_plistDocument.ReadFromFile(str_plistPath);

            // Add URL Scheme
            // var array = plist.root.CreateArray("CFBundleURLTypes");
            // var urlDict = array.AddDict();
            // urlDict.SetString("CFBundleURLName", "hogehogeName");
            // var urlInnerArray = urlDict.CreateArray("CFBundleURLSchemes");
            // urlInnerArray.AddString("hogehogeValue");

            // Apply editing settings to Info.plist
            p_plistDocument.WriteToFile(str_plistPath);
#else
            Debug.Log($"{nameof(SetupPlist)} - Not Define Symbol is Not IOS");
#endif
        }

        private static BuildPlayerOptions Generate_BuildPlayerOption(BuildConfig pBuildConfig, BuildTarget eBuildTarget,
            string strBuildPath)
        {
            BuildPlayerOptions sBuildPlayerOptions = new BuildPlayerOptions
            {
                scenes = pBuildConfig.arrBuildSceneNames,
                locationPathName = strBuildPath,
                target = eBuildTarget,
                options = BuildOptions.None
            };

            return sBuildPlayerOptions;
        }


        private static void GetPath_FromConfig(BuildConfig pConfig, out string strBuildOutputFolderPath,
            out string strFileName)
        {
            string strBuildOutputFolderPath_CommandLine = GetCommandLineArg(const_CommandLine_OutputPath);
            string strFileName_CommandLine = GetCommandLineArg(const_CommandLine_FileName);

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
                DateTime sDateTimeNow = DateTime.Now;
                string strDateTime =
                    $"{sDateTimeNow.Month.ToString("D2") + sDateTimeNow.Day.ToString("D2")}_{sDateTimeNow.Hour.ToString("D2") + sDateTimeNow.Minute.ToString("D2")}";

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

        /// <summary>
        /// 안드로이드 세팅
        /// </summary>
        private static void BuildSetting_Android(BuildConfig.AndroidSetting pAndroidSetting)
        {
            if (string.IsNullOrEmpty(pAndroidSetting.strFullPackageName) == false)
                PlayerSettings.applicationIdentifier = pAndroidSetting.strFullPackageName;

            PlayerSettings.Android.keyaliasName = pAndroidSetting.strKeyalias_Name;
            PlayerSettings.Android.keyaliasPass = pAndroidSetting.strKeyalias_Password;

            PlayerSettings.Android.keystoreName = Application.dataPath + pAndroidSetting.strKeystore_RelativePath;
            PlayerSettings.Android.keystorePass = pAndroidSetting.strKeystore_Password;

            // if (pAndroidSetting.bUse_IL_TO_CPP_Build)
            //     PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            // else
            //     PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);

            Debug.LogFormat(const_strPrefix_ForDebugLog + " Build Setting [Android]\n" +
                            "strPackageName : {0}\n" +
                            "keyaliasName : {1}, keyaliasPass : {2}\n" +
                            "keystoreName : {3}, keystorePass : {4}\n" +
                            "bUse_IL_TO_CPP_Build : {5}",
                PlayerSettings.applicationIdentifier,
                PlayerSettings.Android.keyaliasName, PlayerSettings.Android.keyaliasPass,
                PlayerSettings.Android.keystoreName, PlayerSettings.Android.keystorePass,
                pAndroidSetting.bUse_IL_TO_CPP_Build);
            ;
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