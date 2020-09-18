#region Header
/*	============================================
 *	Author 			    	: Require PlayerPref Key : "Author"
 *	Initial Creation Date 	: 2020-05-08
 *	Summary 		        : 
 *  Template 		        : New Behaviour For Unity Editor V2
   ============================================ */
#endregion Header

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Jenkins
{
	[Serializable]
	public partial class BuildConfig : ScriptableObject
	{
		/// <summary>
		/// 출력할 파일 명, 젠킨스에서 -filename (filename:string) 로 설정가능
		/// </summary>
		public string strFileName = "Build";

		public string strDefineSymbol;

		/// <summary>
		/// 설치한 디바이스에 표기될 이름
		/// </summary>
		public string strProductName;

        /// <summary>
        /// 빌드에 포함할 씬들, 확장자는 안쓰셔도 됩니다.
        /// <para>예시) ["Assets/SomethingScene_1", "Assets/SomethingScene_1"]</para>
        /// </summary>
        public string[] arrBuildSceneNames; 



		/// <summary>
		/// 빌드 타겟이 IOS일 때는 Player Setting - IOS - Version
		/// 빌드 타겟이 안드로이드일 때는 Player Setting - Android - BundleVersionCode
		/// </summary>
		public string strAppVersion;


        // 출력할 폴더 및 파일은 Jenkins에서 처리할 예정이였으나,
        // IL2CPP의 경우 같은 장소에 빌드해놓으면 더 빠르다는 메뉴얼로 인해 일단 보류
        // https://docs.unity3d.com/kr/2020.2/Manual/IL2CPP-OptimizingBuildTimes.html
        public string strAbsolute_BuildOutputFolderPath;

		[Obsolete("Jenkins에서 CommandLine으로 처리할 예정")]
		public bool bUse_DateTime_Suffix;

		public void DoInit()
		{
			strProductName = PlayerSettings.productName;
			arrBuildSceneNames = Builder.GetEnabled_EditorScenes();
			
			strAbsolute_BuildOutputFolderPath = Application.dataPath.Replace("/Assets", "") + "/Build";
			bUse_DateTime_Suffix = true;
		}
	}
}
