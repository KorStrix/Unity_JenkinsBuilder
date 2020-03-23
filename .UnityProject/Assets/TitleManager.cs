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

/// <summary>
/// 
/// </summary>
public class TitleManager : MonoBehaviour
{
	/* const & readonly declaration             */

	/* enum & struct declaration                */

	/* public - Field declaration               */

	public BundleLoader.EBundleLoadLogic eBundleLoadLogic = BundleLoader.EBundleLoadLogic.Editor;

	/* protected & private - Field declaration  */

	BundleLoader _pBundleLoader;

	// ========================================================================== //

	/* public - [Do~Somthing] Function 	        */

	public void ClickVibrate_1()
	{
		ApplicationWrapper.VibrateDevice();
	}

	public void ClickVibrate_2()
	{
		ApplicationWrapper.VibrateDevice(100, 3);
	}

	// ========================================================================== //

	/* protected - [Override & Unity API]       */

	private void Awake()
	{
		_pBundleLoader = GetComponent<BundleLoader>();
	}

	private void OnEnable()
	{
		_pBundleLoader.DoInit(eBundleLoadLogic);

		StopCoroutine(nameof(LoadBundleCoroutine));
		StartCoroutine(nameof(LoadBundleCoroutine));
	}

	/* protected - [abstract & virtual]         */


	// ========================================================================== //

	#region Private

	IEnumerator LoadBundleCoroutine()
	{
		yield return _pBundleLoader.DoPreLoad_Bundle("Scene", OnLoadBundle);
		yield return _pBundleLoader.DoPreLoad_Bundle("Prefab", OnLoadBundle);

		yield break;
	}

	public void OnLoadBundle(string strBundleName, bool bIsSuccess)
	{
		Debug.Log($"{nameof(OnLoadBundle)} - {strBundleName} - bIsSuccess : {bIsSuccess}", this);
	}

	#endregion Private
}