#region Header
/*	============================================
 *	Aurthor 			    : Strix
 *	Initial Creation Date 	: 2020-04-10
 *	Summary 		        : 
 *  Template 		        : For Unity Editor V1
   ============================================ */
#endregion Header

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

#if ASSET_BUNDLE_BROWSER
using AssetBundleBrowser;
#endif


/// <summary>
/// 
/// </summary>
public class AssetBundleBrowserWrapper
{
	/* const & readonly declaration             */

	/* enum & struct declaration                */

	/* public - Field declaration               */


	/* protected & private - Field declaration  */

#if ASSET_BUNDLE_BROWSER
	AssetBundleBrowserMain _pBrowser;
#endif
	
	System.Type _pBrowserType;

	FieldInfo _pField_BuildTab;
	FieldInfo _pField_BuildTab_UserData;
	FieldInfo _pField_BuildTab_UserData_BuildTarget;

	private object _pInstance_BuildTab;
	private object _pInstance_UserData;

	MethodInfo _pMethod_Build;

	// ========================================================================== //

	/* public - [Do~Somthing] Function 	        */

	public AssetBundleBrowserWrapper()
	{
#if ASSET_BUNDLE_BROWSER
		_pBrowser = AssetBundleBrowserMain.GetWindow<AssetBundleBrowserMain>();
		_pBrowserType = _pBrowser.GetType();

		_pField_BuildTab = _pBrowserType.GetField("m_BuildTab", BindingFlags.NonPublic | BindingFlags.Instance);
		_pInstance_BuildTab = _pField_BuildTab.GetValue(_pBrowser);
		
		_pField_BuildTab_UserData =  _pField_BuildTab.FieldType.GetField("m_UserData", BindingFlags.NonPublic | BindingFlags.Instance);
		_pInstance_UserData = _pField_BuildTab_UserData.GetValue(_pInstance_BuildTab);

		_pField_BuildTab_UserData_BuildTarget = _pField_BuildTab_UserData.FieldType.GetField("m_BuildTarget", BindingFlags.NonPublic | BindingFlags.Instance);
		
		_pMethod_Build = _pField_BuildTab.FieldType.GetMethod("ExecuteBuild", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
	}

	public void DoBuildBundle(BuildTarget eBuildTarget)
	{
		_pField_BuildTab_UserData_BuildTarget.SetValue(_pInstance_UserData, (int)eBuildTarget);

		UnityEngine.Debug.Log($"!@#$ Start Build Bundle \n" +
		                      $"Current Platform : {Application.platform} Target : {_pField_BuildTab_UserData_BuildTarget.GetValue(_pInstance_UserData)} \n" +
		                      $"Symbol : {PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)} \n");
		
#if ASSET_BUNDLE_BROWSER
		_pMethod_Build?.Invoke(_pField_BuildTab.GetValue(_pBrowser), null);
		_pBrowser.Close();
#else
		UnityEngine.Debug.Log("!@#$ Use Define Symbol ASSET_BUNDLE_BROWSER");
#endif
		UnityEngine.Debug.Log("!@#$ Finish Build");
	}

	// ========================================================================== //

	/* protected - [Override & Unity API]       */


	/* protected - [abstract & virtual]         */


	// ========================================================================== //

	#region Private

	#endregion Private
}