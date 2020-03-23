#region Header
/*	============================================
 *	Aurthor 			    : Strix
 *	Initial Creation Date 	: #CREATIONDATE#
 *	Summary 		        : 
 *  Template 		        : For Unity Editor V1
   ============================================ */
#endregion Header

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using AssetBundleBrowser;
using System.Reflection;
using UnityEditor;

/// <summary>
/// 
/// </summary>
public class AssetBundleBrowserWrapper
{
	/* const & readonly declaration             */

	/* enum & struct declaration                */

	/* public - Field declaration               */


	/* protected & private - Field declaration  */

	AssetBundleBrowserMain _pBrowser;
	System.Type _pBrowserType;
	System.Type _pAssetBundleBuildTab;
	FieldInfo _pField_BuildTab;
	MethodInfo _pMethod_Build;

	// ========================================================================== //

	/* public - [Do~Somthing] Function 	        */

	public AssetBundleBrowserWrapper()
	{
		_pBrowser = AssetBundleBrowserMain.GetWindow<AssetBundleBrowserMain>();
		_pBrowserType = _pBrowser.GetType();

		_pField_BuildTab = _pBrowserType.GetField("m_BuildTab", BindingFlags.NonPublic | BindingFlags.Instance);
		_pAssetBundleBuildTab = _pField_BuildTab.FieldType;

		_pMethod_Build = _pAssetBundleBuildTab.GetMethod("ExecuteBuild", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	public void DoBuildBundle()
	{
		_pMethod_Build.Invoke(_pField_BuildTab.GetValue(_pBrowser), null);
	}

	// ========================================================================== //

	/* protected - [Override & Unity API]       */


	/* protected - [abstract & virtual]         */


	// ========================================================================== //

	#region Private

	#endregion Private
}