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
using UnityEditor.Build.Reporting;

namespace Jenkins
{
    public partial class BuildConfig
    {
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
            /// 아직 미지원
            /// </summary>
            public bool bUse_IL_TO_CPP_Build;

            public int iBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
        }

        public AndroidSetting pAndroidSetting = new AndroidSetting();
    }
    
    public partial class Builder
    {
        [MenuItem("Tools/Build/Build Test - Android")]
        public static void Build_Test_Android()
        {
            BuildConfig pConfig = new BuildConfig();
            BuildTargetGroup eBuildTargetGroup = GetBuildTargetGroup(BuildTarget.Android);
            pConfig.strDefineSymbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(eBuildTargetGroup);

            DoBuild(pConfig, pConfig.strAbsolute_BuildOutputFolderPath, pConfig.strFileName, BuildTarget.Android);
        }

        // ==============================================================================================


        /// <summary>
        /// 안드로이드 세팅
        /// </summary>
        private static void BuildSetting_Android(BuildConfig.AndroidSetting pSetting)
        {
            if (string.IsNullOrEmpty(pSetting.strFullPackageName) == false)
                PlayerSettings.applicationIdentifier = pSetting.strFullPackageName;

            PlayerSettings.Android.keyaliasName = pSetting.strKeyalias_Name;
            PlayerSettings.Android.keyaliasPass = pSetting.strKeyalias_Password;

            PlayerSettings.Android.keystoreName = Application.dataPath + pSetting.strKeystore_RelativePath;
            PlayerSettings.Android.keystorePass = pSetting.strKeystore_Password;

            string strBundleVersionCode_FromCommandLine = GetCommandLineArg(const_mapCommandLine[ECommandLineList.android_bundle_versioncode]);
            if(int.TryParse(strBundleVersionCode_FromCommandLine, out int iBundleVersionCode))
                PlayerSettings.Android.bundleVersionCode = iBundleVersionCode;
            else
                PlayerSettings.Android.bundleVersionCode = pSetting.iBundleVersionCode;

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
                pSetting.bUse_IL_TO_CPP_Build);
            ;
        }
    }
}

#endif
