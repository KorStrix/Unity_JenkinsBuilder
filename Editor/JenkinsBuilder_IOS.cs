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

// #define UNITY_IOS // IOS Code Compile Test

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Jenkins
{
    public partial class Builder
    {
        [MenuItem("Tools/Build/Build Test - IOS")]
        public static void Build_Test_IOS()
        {
            BuildConfig pConfig = new BuildConfig();
            BuildTargetGroup eBuildTargetGroup = GetBuildTargetGroup(BuildTarget.iOS);
            pConfig.strDefineSymbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);

            DoBuild(pConfig, pConfig.strAbsolute_BuildOutputFolderPath, pConfig.strFileName, BuildTarget.iOS);
        }

        public static void Build_IOS()
        {
            if (GetFile_From_CommandLine(const_CommandLine_ConfigFilePath, out BuildConfig pConfig))
            {
                GetPath_FromConfig(pConfig, out string strBuildOutputFolderPath, out string strFileName);
                DoBuild(pConfig, strBuildOutputFolderPath, strFileName, BuildTarget.iOS);
            }
        }

        [PostProcessBuild(999999)]
        public static void OnPostprocessBuild(BuildTarget eBuildTarget, string strPath)
        {
            Debug.Log($"{const_strPrefix_ForDebugLog} OnPostprocessBuild - BuildTarget : {eBuildTarget} strPath : {strPath}");
            if (eBuildTarget != BuildTarget.iOS)
                return;

            Init_XCodeProject(strPath);
            Setup_XCodePlist(strPath);
        }

        // ==============================================================================================

        /// <summary>
        /// IOS용 XCode Initialize
        /// </summary>
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

        private static void Setup_XCodePlist(string strXCodeProjectPath)
        {
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Setup_XCodePlist)} Start - {nameof(strXCodeProjectPath)} : {strXCodeProjectPath}");
            
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
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Setup_XCodePlist)} - WriteToFile {str_plistPath}");
#else
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Setup_XCodePlist)} - Not Define Symbol is Not IOS");
#endif
        }

        private static void BuildSetting_IOS(BuildConfig.IOSSetting pSetting)
        {
            if (string.IsNullOrEmpty(pSetting.strBundle_Identifier) == false)
                PlayerSettings.applicationIdentifier = pSetting.strBundle_Identifier;
            
            Debug.LogFormat(const_strPrefix_ForDebugLog + " Build Setting [IOS]\n" +
                            "strPackageName : {0}\n",
                PlayerSettings.applicationIdentifier
            );
        }

    }
}

#endif