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

// #define UNITY_IOS // IOS Code Compile Test

using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;



#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

namespace Jenkins
{
    public partial class BuildConfig
    {
        /// <summary>
        /// 유니티 -> XCode Export -> XCode ArchiveBuildConfig -> .ipa 에 필요한 모든 설정
        /// </summary>
        [Serializable]
        public class IOSSetting
        {
            /// <summary>
            /// 애플 개발자 사이트에서 조회 가능, 숫자랑 영어로 된거
            /// </summary>
            public string strAppleTeamID;

            /// <summary>
            /// Apple에서 세팅된 net.Company.Product 형식의 string
            /// </summary>
            public string strBundle_Identifier;
            public string strEntitlementsFileName_Without_ExtensionName;
            
            /// <summary>
            /// 유니티 Asset 경로에서 XCode Project로 카피할 파일 목록, 확장자까지 작성해야 합니다
            /// <para>UnityProject/Assets/ 기준</para>
            /// </summary>
            public string[] arrCopy_AssetFilePath_To_XCodeProjectPath;

            /// <summary>
            /// XCode 프로젝트에 추가할 Framework, 확장자까지 작성해야 합니다
            /// </summary>
            public string[] arrXCode_Framework_Add;

            /// <summary>
            /// XCode 프로젝트에 제거할 Framework, 확장자까지 작성해야 합니다
            /// </summary>
            public string[] arrXCode_Framework_Remove;

            public string[] arrXCode_OTHER_LDFLAGS_Add;
            public string[] arrXCode_OTHER_LDFLAGS_Remove;

            /// <summary>
            /// HTTP 주소 IOS는 기본적으로 HTTP를 허용 안하기 때문에 예외에 추가해야 합니다. http://는 제외할것
            /// <para>예시) http://www.naver.com = www.naver.com</para>
            /// </summary>
            public string[] arrHTTPAddress;

            /// <summary>
            /// 출시할 빌드 버전
            /// <para>이미 앱스토어에 올렸으면 그 다음 항상 숫자를 올려야 합니다. 안그럼 앱스토어에서 안받음</para>
            /// </summary>
            public string strBuildVersion;

            /// <summary>
            /// 빌드 번호
            /// <para>이미 앱스토어에 올렸으면 그 다음 항상 1씩 올려야 합니다. 안그럼 앱스토어에서 안받음</para>
            /// </summary>
            public string strBuildNumber;

            [Serializable]
            public class PLIST_ADD
            {
                public string strKey;
                public string strValue;

                public PLIST_ADD(string strKey, string strValue)
                {
                    this.strKey = strKey;
                    this.strValue = strValue;
                }
            }

            public PLIST_ADD[] arrAddPlist = new PLIST_ADD[] {new PLIST_ADD("ExampleKey", "ExampleValue")};
            public string[] arrRemovePlistKey = new string[] { "ExampleKey", "ExampleValue" };

            /// <summary>
            /// <para>ScriptableObject 생성시 생성자에 PlayerSettings에서 Get할경우 Unity Exception이 남</para>
            /// </summary>
            /// <returns></returns>
            public static IOSSetting CreateSetting()
            {
                IOSSetting pNewSetting = new IOSSetting();

                return pNewSetting;
            }
        }

        public IOSSetting pIOSSetting;
    }
    
    public partial class Builder
    {
        public const int const_iPostBuildCallbackOrder = 777;

        [MenuItem(const_strPrefix_EditorContextMenu + "Build Test - IOS")]
        // ReSharper disable once UnusedMember.Global
        public static void Build_Test_IOS()
        {
            BuildConfig pConfig = new BuildConfig();
            BuildTargetGroup eBuildTargetGroup = GetBuildTargetGroup(BuildTarget.iOS);
            pConfig.strDefineSymbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);

            DoBuild(pConfig, pConfig.strAbsolute_BuildOutputFolderPath, pConfig.strFileName, BuildTarget.iOS);
        }

        [PostProcessBuild(const_iPostBuildCallbackOrder)]
        public static void OnPostProcessBuild(BuildTarget eBuildTarget, string strPath)
        {
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(OnPostProcessBuild)} - BuildTarget : {eBuildTarget} strPath : {strPath}");
            if (eBuildTarget != BuildTarget.iOS)
                return;

            Init_XCodeProject(strPath);
            Setup_XCodePlist(strPath);
        }

        // ==============================================================================================

        /// <summary>
        /// IOS용 XCode Initialize
        /// </summary>
        // ReSharper disable once UnusedParameter.Local
        private static void Init_XCodeProject(string strXCodeProjectPath)
        {
#if UNITY_IOS
            BuildConfig.IOSSetting pIOSSetting = g_pLastConfig.pIOSSetting;
            var projectPath = strXCodeProjectPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
            
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Init_XCodeProject)} Start - {nameof(strXCodeProjectPath)} : {strXCodeProjectPath}\n" +
                      $"projectPath : {projectPath}");

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
            
            
            // Remove XCode Framework
            foreach (string strFramework in pIOSSetting.arrXCode_Framework_Remove)
            {
                pPBXProject.RemoveFrameworkFromProject(strTargetGuid, strFramework);
                Debug.Log($"{const_strPrefix_ForDebugLog} Remove Framework \"{strFramework}\" to XCode Project");
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
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Init_XCodeProject)} - Define Symbol is Not IOS");
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

        // ReSharper disable once UnusedParameter.Local
        private static void Setup_XCodePlist(string strXCodeProjectPath)
        {
#if UNITY_IOS
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Setup_XCodePlist)} Start - {nameof(strXCodeProjectPath)} : {strXCodeProjectPath}");
            BuildConfig.IOSSetting pIOSSetting = g_pLastConfig.pIOSSetting;

            // Property List(.plist) Default Name
            const string strInfoPlistName = "Info.plist";

            var str_plistPath = Path.Combine(strXCodeProjectPath, strInfoPlistName);
            var p_plistDocument = new PlistDocument();
            p_plistDocument.ReadFromFile(str_plistPath);



            // 해당 키는 IOS 업로드 시 해당 키는 지원하지 않는다는 에러 발생으로 인해 제거
            const string strExitsOnSuspendKey = "UIApplicationExitsOnSuspend";
            var arrRootValues = p_plistDocument.root.values;
            if(arrRootValues.ContainsKey(strExitsOnSuspendKey))
                arrRootValues.Remove(strExitsOnSuspendKey);

            foreach (var pProperty in pIOSSetting.arrAddPlist)
            {
                Debug.Log($"{const_strPrefix_ForDebugLog} Add Property - Key : \"{pProperty.strKey}\" Value : {pProperty.strValue}");

                if (arrRootValues.ContainsKey(pProperty.strKey))
                    arrRootValues[pProperty.strKey] = new PlistElementString(pProperty.strValue);
                else
                    arrRootValues.Add(pProperty.strKey, new PlistElementString(pProperty.strValue));
            }

            foreach (string strRemovePropertyKey in pIOSSetting.arrRemovePlistKey)
            {
                if (arrRootValues.ContainsKey(strRemovePropertyKey))
                {
                    arrRootValues.Remove(strRemovePropertyKey);
                    Debug.Log($"{const_strPrefix_ForDebugLog} Contains & Removed Key.Length : {strRemovePropertyKey}");
                }
            }

            Debug.Log($"{const_strPrefix_ForDebugLog} pIOSSetting.arrHTTPAddress.Length : \"{pIOSSetting.arrHTTPAddress.Length}\"");

            // HTTP 주소는 Plist에 추가해야 접근 가능..
            if (pIOSSetting.arrHTTPAddress.Length != 0)
            {
                PlistElementDict pTransportDict = Get_Or_Add_PlistDict(arrRootValues, "NSAppTransportSecurity");
                PlistElementDict pExceptionDomainsDict = Get_Or_Add_PlistDict(pTransportDict.values, "NSExceptionDomains");

                foreach (string strAddress in pIOSSetting.arrHTTPAddress)
                {
                    Debug.Log($"{const_strPrefix_ForDebugLog} Add HTTPAddress : \"{strAddress}\"");

                    PlistElementDict pTransportDomain = Get_Or_Add_PlistDict(pExceptionDomainsDict.values, strAddress);
                    const string strNSAllowInsecureHTTPLoadsKey = "NSExceptionAllowsInsecureHTTPLoads";
                    const string strNSIncludesSubdomains = "NSIncludesSubdomains";


                    if (pTransportDomain.values.ContainsKey(strNSAllowInsecureHTTPLoadsKey) == false)
                        pTransportDomain.values.Add(strNSAllowInsecureHTTPLoadsKey, new PlistElementBoolean(true));

                    if (pTransportDomain.values.ContainsKey(strNSIncludesSubdomains) == false)
                        pTransportDomain.values.Add(strNSIncludesSubdomains, new PlistElementBoolean(true));
                }
            }

            // Apply editing settings to Info.plist
            p_plistDocument.WriteToFile(str_plistPath);
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Setup_XCodePlist)} - WriteToFile {str_plistPath}");
#else
            Debug.Log($"{const_strPrefix_ForDebugLog} {nameof(Setup_XCodePlist)} - Not Define Symbol is Not IOS");
#endif
        }

#if UNITY_IOS
        private static PlistElementDict Get_Or_Add_PlistDict(IDictionary<string, PlistElement> arrRootValues, string strKey)
        {
            if (arrRootValues.ContainsKey(strKey) == false)
                arrRootValues.Add(strKey, new PlistElementDict());

            return arrRootValues[strKey].AsDict();
        }
#endif

        private static void BuildSetting_IOS(BuildConfig.IOSSetting pSetting)
        {
            if (string.IsNullOrEmpty(pSetting.strBundle_Identifier) == false)
                PlayerSettings.applicationIdentifier = pSetting.strBundle_Identifier;

            string strVersion_FromCommandLine = GetCommandLineArg(mapCommandLine[ECommandLineList.ios_version]);
            if (string.IsNullOrEmpty(strVersion_FromCommandLine) == false)
                PlayerSettings.bundleVersion = strVersion_FromCommandLine;
            else if(string.IsNullOrEmpty(pSetting.strBuildVersion) == false)
                PlayerSettings.bundleVersion = pSetting.strBuildVersion;


            string strBuildNumber_FromCommandLine = GetCommandLineArg(mapCommandLine[ECommandLineList.ios_buildnumber]);
            if(string.IsNullOrEmpty(strBuildNumber_FromCommandLine) == false)
                PlayerSettings.iOS.buildNumber = strBuildNumber_FromCommandLine;
            else if(string.IsNullOrEmpty(pSetting.strBuildNumber) == false)
                PlayerSettings.iOS.buildNumber = pSetting.strBuildNumber;


            Debug.Log(const_strPrefix_ForDebugLog +
                      $" Build Setting [IOS]\n" +
                      $"applicationIdentifier : {PlayerSettings.applicationIdentifier}\n" +
                      $"PlayerSettings.bundleVersion : {PlayerSettings.bundleVersion}\n" +
                      $"buildNumber : {PlayerSettings.iOS.buildNumber}"
                      );
        }

    }
}

#endif
