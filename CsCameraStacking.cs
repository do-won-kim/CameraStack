using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CsCameraStacking : MonoBehaviour
{
	const int c_nDefaultDepth = -1;

	Camera m_camera;
	UniversalAdditionalCameraData m_universalAdditionalCameraData;
	bool m_bFirst = false;

	[SerializeField]
	float m_flDepth = c_nDefaultDepth;
	[SerializeField]
	bool m_bIsBaseCamera = false;

	public Camera GetCamera { get { return m_camera; }  }
	public UniversalAdditionalCameraData GetUniversalAdditionalCameraData { get { return m_universalAdditionalCameraData; } }
	public bool IsBaseCamera { get { return m_bIsBaseCamera; } set { m_bIsBaseCamera = value; } }
	public float GetDepth
	{
		get { return m_flDepth; }
		set
		{
			m_flDepth = value;
			CsCameraStackManager.GetInstance().ChangeDepth();
		}
	}

	//---------------------------------------------------------------------------------------------------
	void Awake()
	{
		m_camera = GetComponent<Camera>();
		m_universalAdditionalCameraData = m_camera.GetUniversalAdditionalCameraData();

		if (m_flDepth == c_nDefaultDepth)
		{
			m_flDepth = m_camera.depth;
		}

		CsCameraStackManager.GetInstance().AddCamera(this);


	}
	
	//---------------------------------------------------------------------------------------------------
	void OnDestroy()
	{
		CsCameraStackManager.GetInstance().DeleteCamera(this);
	}

	//---------------------------------------------------------------------------------------------------
	void OnEnable()
	{
		if (!m_bFirst)
		{
			m_bFirst = true;
			return;
		}
		CsCameraStackManager.GetInstance().EnableCamera(this);
	}

	//---------------------------------------------------------------------------------------------------
	void OnDisable()
	{
		CsCameraStackManager.GetInstance().DisableCamera(this);
	}

	//---------------------------------------------------------------------------------------------------
	//오브젝트가 아니라 컴포넌트가 꺼지고 켜질경우 수동으로 해줘야함
	//---------------------------------------------------------------------------------------------------
	public void EnableCameraComponent()
	{
		if (!m_bFirst)
		{
			m_bFirst = true;
			return;
		}
		CsCameraStackManager.GetInstance().EnableCamera(this);
	}

	//---------------------------------------------------------------------------------------------------
	public void DisableCameraComponent()
	{
		CsCameraStackManager.GetInstance().DisableCamera(this);
	}
}
