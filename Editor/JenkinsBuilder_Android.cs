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

#if UNITY_EDITOR
using UnityEditor;

namespace Jenkins
{
    public partial class BuildConfig
    {
        [Serializable]
        public class AndroidSetting
        {
             /// <summary>
             /// 예시) com.CompanyName.ProductName  
             /// </summary>
            public string strFullPackageName;

            public string strKeyalias_Name;
            public string strKeyalias_Password;

            /// <summary>
            /// Keystore 파일의 경로입니다. `파일경로/파일명.keystore` 까지 쓰셔야 합니다.
            /// <para>UnityProject/Asset/ 기준의 상대 경로입니다. </para>
            /// <para>예를들어 UnityProject/Asset 폴더 밑에 example.keystore를 넣으셨으면 "/example.keystore" 입니다.</para>
            /// </summary>
            public string strKeystore_RelativePath;
            public string strKeystore_Password;

            /// <summary>
            /// CPP 빌드를 할지 체크, CPP빌드는 오래 걸리므로 Test빌드가 아닌 Alpha 빌드부터 하는걸 권장
            /// 아직 미지원
            /// </summary>
            public bool bUse_IL_TO_CPP_Build;

            public int iBundleVersionCode;

            public string strVersion;


            /// <summary>
            /// <para>ScriptableObject 생성시 생성자에 PlayerSettings에서 Get할경우 Unity Exception이 남</para>
            /// </summary>
            /// <returns></returns>
            public static AndroidSetting CreateSetting()
            {
                AndroidSetting pNewSetting = new AndroidSetting();

                pNewSetting.strFullPackageName = PlayerSettings.applicationIdentifier;
                pNewSetting.iBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
                pNewSetting.strVersion = PlayerSettings.bundleVersion;

                return pNewSetting;
            }
        }

        public AndroidSetting pAndroidSetting;
    }
    
    public partial class Builder
    {
        [MenuItem(const_strPrefix_EditorContextMenu + "Build Test - Android")]
        public static void Build_Test_Android()
        {
            BuildConfig pConfig = BuildConfig.CreateConfig();
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

            string strBundleVersionCode_FromCommandLine = GetCommandLineArg(mapCommandLine[ECommandLineList.android_bundle_versioncode]);
            if(int.TryParse(strBundleVersionCode_FromCommandLine, out int iBundleVersionCode))
                PlayerSettings.Android.bundleVersionCode = iBundleVersionCode;
            else
                PlayerSettings.Android.bundleVersionCode = pSetting.iBundleVersionCode;

            string strVersionCode_FromCommandLine = GetCommandLineArg(mapCommandLine[ECommandLineList.android_version]);
            if (string.IsNullOrEmpty(strVersionCode_FromCommandLine) == false)
                PlayerSettings.bundleVersion = strVersionCode_FromCommandLine;
            else
                PlayerSettings.bundleVersion = pSetting.strVersion;

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
