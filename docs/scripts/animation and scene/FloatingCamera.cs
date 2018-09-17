using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;
using UnityStandardAssets.Characters.FirstPerson;


	// Modified version of first person controller from Unity Standard assets
	// provides a free-floating camera, but includes the input management from the Standard Assets.
	//findme todo: Rewrite to remove dependencies/conflicts from Unity assets
	

	[RequireComponent(typeof (CharacterController))]
	public class FloatingCamera : MonoBehaviour
	{
		// original vars based on unity First Person Controller
		[SerializeField] private bool m_IsWalking;
		[SerializeField] private float m_WalkSpeed;
		[SerializeField] private float m_RunSpeed;
		[SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
		[SerializeField] private MouseLook m_MouseLook;
		[SerializeField] private bool m_UseFovKick;
		[SerializeField] private FOVKick m_FovKick = new FOVKick();




		private Camera m_Camera;

		private float m_YRotation;
		private Vector2 m_Input;
		private Vector3 m_MoveDir = Vector3.zero;
		private CharacterController m_CharacterController;
		private CollisionFlags m_CollisionFlags;
		private Vector3 m_OriginalCameraPosition;


		// floating cam vars
		private Ray ray;

		// Use this for initialization
		private void Start()
		{
			m_CharacterController = GetComponent<CharacterController>();
			m_Camera = Camera.main;
			m_OriginalCameraPosition = m_Camera.transform.localPosition;
			m_FovKick.Setup(m_Camera);
			m_MouseLook.Init(transform , m_Camera.transform);
		}


		// Update is called once per frame
		private void Update()
		{
			RotateView();

		}




		private void FixedUpdate()
		{

			float speed;
			GetInput(out speed);
			// always move along the camera forward as it is the direction that it being aimed at

			// draw a ray from the camera view cursor position
			// findme note: note cursor lock is on therefore centered.  Therefore should work on tablet
			ray = Camera.main.ScreenPointToRay (Input.mousePosition);



//		Debug.DrawRay (ray.origin,ray.direction*100,Color.red);

		Vector3 desiredMove = ray.direction;								
			desiredMove = desiredMove * m_Input.y + transform.right * m_Input.x;		// move camera in direction of view * input


			m_MoveDir = desiredMove*speed;
		
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

			UpdateCameraPosition(speed);

			m_MouseLook.UpdateCursorLock();
		}





	private void UpdateCameraPosition(float speed)
	{
		Vector3 newCameraPosition;

		newCameraPosition = m_Camera.transform.localPosition;

		m_Camera.transform.localPosition = newCameraPosition;
	}


	private void GetInput(out float speed)
	{
		// Read input
		float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
		float vertical = CrossPlatformInputManager.GetAxis("Vertical");

		bool waswalking = m_IsWalking;

		#if !MOBILE_INPUT
		// On standalone builds, walk/run speed is modified by a key press.
		// keep track of whether or not the character is walking or running
		m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
		#endif
		// set the desired speed to be walking or running
		speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
		m_Input = new Vector2(horizontal, vertical);

		// normalize input if it exceeds 1 in combined length:
		if (m_Input.sqrMagnitude > 1)
		{
			m_Input.Normalize();
		}

		// handle speed change to give an fov kick
		// only if the player is going to a run, is running and the fovkick is to be used
		if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
		{
			StopAllCoroutines();
			StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
		}
	}


	private void RotateView()
	{
		m_MouseLook.LookRotation (transform, m_Camera.transform);
	}


	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		/*

            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);

*/
	}

}
