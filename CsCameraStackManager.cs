using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine;
public class CsCameraStackManager
{
	private static CsCameraStackManager s_instance;

	public static CsCameraStackManager GetInstance()
	{
		if (s_instance == null)
		{
			s_instance = new CsCameraStackManager();
		}
		return s_instance;
	}


	const int c_nDefalutIndex = -1;
	const float c_flDefalutPostProsessingStack = -100;

	//스택용 카메라
	List<CsCameraStacking> m_listOverlayCamera = new List<CsCameraStacking>();
	//메인카메라(base카메라가 여러개 나올때가 있음)
	List<CsCameraStacking> m_listUniversalAdditionalCameraData = new List<CsCameraStacking>();
	//현재 렌더링 중인 메인카메라
	UniversalAdditionalCameraData m_universalAdditionalCameraDataMain;
	//base카메라가 없을 때 overlay카메라중 하나를 base카메라로 바꿔서 사용함
	CsCameraStacking m_csCameraStackingSub = null;

	float m_flRenderScale = 1.0f;

	public float RenderScale { set { m_flRenderScale = value; } }

	List<int> m_listNull = new List<int>();

	//---------------------------------------------------------------------------------------------------
	public void AddCamera(CsCameraStacking csCameraStacking)
	{
		if (csCameraStacking.GetUniversalAdditionalCameraData.renderType == CameraRenderType.Base)
		{
			if (csCameraStacking.GetCamera.enabled)
			{
				if (m_csCameraStackingSub != null)
				{
					m_csCameraStackingSub.GetUniversalAdditionalCameraData.renderType = CameraRenderType.Overlay;
					m_csCameraStackingSub = null;
				}
			}

			m_listUniversalAdditionalCameraData.Add(csCameraStacking.GetComponent<CsCameraStacking>());

			if (csCameraStacking.GetCamera.enabled)
				SetBaseCamera(csCameraStacking);

		}
		else
		{
			if (!m_listOverlayCamera.Find(a => a == csCameraStacking))
			{
				m_listOverlayCamera.Add(csCameraStacking);
				SortStack();

				//메인 카메라가 존재 하지 않음(오버레이만 있는 상태)
				if (m_universalAdditionalCameraDataMain == null)
				{
					//메인 대신 사용 중인 오버레이 카메라가 존재하는데 뎁스가 더 낮은 카메라가 들어옴
					if (m_csCameraStackingSub != null && csCameraStacking.GetDepth < m_csCameraStackingSub.GetDepth)
					{
						m_csCameraStackingSub.GetUniversalAdditionalCameraData.renderType = CameraRenderType.Overlay;
						SetSubCamera(csCameraStacking);
					}
					//메인 카메라가 없고 대신 사용 중인 것도 없음
					else if (m_csCameraStackingSub == null)
					{
						SetSubCamera(csCameraStacking);
					}
					//메인 대신 사용중인 카메라가 있고 그 카메라보다 뎁스가 높음 - 메인 카메라가 있을때랑 같은 방식으로 동작
					else
					{
						int nIndex = m_listOverlayCamera.FindIndex(a => a == csCameraStacking);
						AddBaseCameraStack(nIndex, csCameraStacking);
					}
				}
				else
				{
					int nIndex = m_listOverlayCamera.FindIndex(a => a == csCameraStacking);
					AddBaseCameraStack(nIndex, csCameraStacking);
				}
			}
		}
	}

	//---------------------------------------------------------------------------------------------------
	public void DeleteCamera(CsCameraStacking csCameraStacking)
	{
		if (csCameraStacking.GetUniversalAdditionalCameraData.renderType == CameraRenderType.Overlay)
		{
			m_listOverlayCamera.Remove(csCameraStacking);

			if (m_universalAdditionalCameraDataMain != null)
				m_universalAdditionalCameraDataMain.cameraStack.Remove(csCameraStacking.GetCamera);

			if (m_csCameraStackingSub != null && m_csCameraStackingSub.GetUniversalAdditionalCameraData.cameraStack != null)
			{
				m_csCameraStackingSub.GetUniversalAdditionalCameraData.cameraStack.Remove(csCameraStacking.GetCamera);
			}
		}
		else
		{
			m_listUniversalAdditionalCameraData.Remove(csCameraStacking.GetComponent<CsCameraStacking>());
		}
	}

	//---------------------------------------------------------------------------------------------------
	public void EnableCamera(CsCameraStacking csCameraStacking)
	{
		if (csCameraStacking.GetUniversalAdditionalCameraData.renderType == CameraRenderType.Base)
		{
			if (m_universalAdditionalCameraDataMain != null && csCameraStacking.IsBaseCamera)
			{
				if (m_csCameraStackingSub != null)
				{
					m_csCameraStackingSub.GetUniversalAdditionalCameraData.renderType = CameraRenderType.Overlay;
					m_csCameraStackingSub = null;
				}

				SetBaseCamera(csCameraStacking);
			}
		}
		else
		{
			if (m_csCameraStackingSub != null)
			{
				if (csCameraStacking.GetDepth < m_csCameraStackingSub.GetDepth)
				{
					m_csCameraStackingSub.GetUniversalAdditionalCameraData.renderType = CameraRenderType.Overlay;
					SetSubCamera(csCameraStacking);
				}
			}
		}
	}

	//---------------------------------------------------------------------------------------------------
	public void DisableCamera(CsCameraStacking csCameraStacking)
	{
		if (csCameraStacking.GetUniversalAdditionalCameraData.renderType == CameraRenderType.Base)
		{
			if (m_csCameraStackingSub != null)
			{
				if (csCameraStacking == m_csCameraStackingSub)
				{
					SetSubCamera(csCameraStacking);
					csCameraStacking.GetUniversalAdditionalCameraData.renderType = CameraRenderType.Overlay;
				}
			}
			else
			{
				int nBaseCameraIndex = BaseCameraStack(csCameraStacking.GetUniversalAdditionalCameraData);
				if (nBaseCameraIndex != c_nDefalutIndex)
				{
					SetBaseCamera(m_listUniversalAdditionalCameraData[nBaseCameraIndex]);
					return;
				}

				if (m_universalAdditionalCameraDataMain == csCameraStacking.GetUniversalAdditionalCameraData)
				{
					SetSubCamera(csCameraStacking);
				}
			}
		}
	}

	//---------------------------------------------------------------------------------------------------
	public void ChangeDepth()
	{
		//뎁스가 중간에 바뀔 경우 리스트를 재정렬해줘야함
		if (m_universalAdditionalCameraDataMain != null)
		{
			SortStack();
			m_universalAdditionalCameraDataMain.cameraStack.Clear();
			AddCameraStackList(m_universalAdditionalCameraDataMain);
		}
	}

	//---------------------------------------------------------------------------------------------------
	void SetBaseCamera(CsCameraStacking csCameraStacking)
	{
		if (m_universalAdditionalCameraDataMain != null)
		{
			m_universalAdditionalCameraDataMain.cameraStack.Clear();
		}
		m_universalAdditionalCameraDataMain = csCameraStacking.GetUniversalAdditionalCameraData;
		SortStack();

		m_universalAdditionalCameraDataMain.cameraStack.Clear();

		AddCameraStackList(m_universalAdditionalCameraDataMain);

		//renderScale을 그래픽 세팅값으로 바꿔준다 
		var rpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
		var urpAsset = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)rpAsset;
		urpAsset.renderScale = m_flRenderScale;
	}

	//---------------------------------------------------------------------------------------------------
	int BaseCameraStack(UniversalAdditionalCameraData universalAdditionalCameraData = null)
	{
		int nIndex = -1;

		for (int i = 0; i < m_listUniversalAdditionalCameraData.Count; ++i)
		{
			if (universalAdditionalCameraData != null && universalAdditionalCameraData == m_listUniversalAdditionalCameraData[i].GetUniversalAdditionalCameraData)
			{
				continue;
			}

			if (m_listUniversalAdditionalCameraData[i].gameObject.activeInHierarchy && m_listUniversalAdditionalCameraData[i].GetCamera.enabled)
			{
				nIndex = i;
				break;
			}
		}

		return nIndex;
	}

	//---------------------------------------------------------------------------------------------------
	void AddCameraStackList(UniversalAdditionalCameraData universalAdditionalCameraData)
	{

		for (int i = 0; i < m_listOverlayCamera.Count; ++i)
		{
			if (m_listOverlayCamera[i] == null)
			{
				m_listNull.Add(i);
				continue;
			}

			if (universalAdditionalCameraData == m_listOverlayCamera[i].GetUniversalAdditionalCameraData)
				continue;

			universalAdditionalCameraData.cameraStack.Add(m_listOverlayCamera[i].GetCamera);
		}

		m_listNull.Sort((a, b) => b.CompareTo(a));

		for (int i = 0; i < m_listNull.Count; ++i)
		{
			m_listOverlayCamera.RemoveAt(m_listNull[i]);
		}

		m_listNull.Clear();
	}

	//---------------------------------------------------------------------------------------------------
	void AddBaseCameraStack(int nIndex, CsCameraStacking csCameraStacking)
	{

		if (m_csCameraStackingSub != null)
		{
			m_csCameraStackingSub.GetUniversalAdditionalCameraData.cameraStack.Clear();
			AddCameraStackList(m_csCameraStackingSub.GetUniversalAdditionalCameraData);
			return;
		}

		// main camera enable
		if (m_universalAdditionalCameraDataMain != null)
		{
			m_universalAdditionalCameraDataMain.cameraStack.Clear();
			AddCameraStackList(m_universalAdditionalCameraDataMain);
		}
	}

	//---------------------------------------------------------------------------------------------------
	void SetSubCamera(CsCameraStacking csCameraStacking)
	{
		if (m_listOverlayCamera.Count != 0)
		{
			int nIndex = EnableOverlayCamera();
			m_listOverlayCamera[nIndex].GetUniversalAdditionalCameraData.renderType = CameraRenderType.Base;
			m_csCameraStackingSub = m_listOverlayCamera[nIndex];
			m_csCameraStackingSub.GetUniversalAdditionalCameraData.cameraStack.Clear();
			AddCameraStackList(m_csCameraStackingSub.GetUniversalAdditionalCameraData);
			//서브 카메라가 메인 카메라가 되는 경우 보통 전체 화면의 UI가 덮을 때
			//이 때는 renderScale을 1.0f으로 바꿔줘야한다.
			var rpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
			var urpAsset = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)rpAsset;
			urpAsset.renderScale = 1.0f;
		}
	}

	//---------------------------------------------------------------------------------------------------
	int EnableOverlayCamera()
	{
		int nIndex = 0;
		for (int i = 0; i < m_listOverlayCamera.Count; ++i)
		{
			if (m_listOverlayCamera[i] == null) continue;
			if (m_listOverlayCamera[i].gameObject.activeInHierarchy &&
				m_listOverlayCamera[i].GetCamera.enabled)
			{
				nIndex = i;
				break;
			}
		}
		return nIndex;
	}

	//---------------------------------------------------------------------------------------------------
	void SortStack()
	{
		m_listOverlayCamera.Sort((CsCameraStacking a, CsCameraStacking b) => {

			if (a.GetDepth < b.GetDepth)
			{
				return -1;
			}
			else if (a.GetDepth > b.GetDepth)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
		);
	}
}
