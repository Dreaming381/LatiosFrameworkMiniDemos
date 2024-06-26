using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.InputSystem;

// Post-Jam Notes:
// Input capture is a little weird for mouselook. We add the mouse delta to the center of the screen
// and get the camera ray at that location. We transform the direction of that ray into the camera's local
// space, and treat that as the target look direction. I don't think we really tested controller at all.

namespace BB
{
    [RequireMatchingQueriesForUpdate]
    public partial class ReadPlayerInputSystem : SubSystem
    {
        const float kMouseSensitivity      = 1f;
        const float kControllerSensitivity = 100f;

        InputSystem_Actions m_actions;

        protected override void OnCreate()
        {
            m_actions = new InputSystem_Actions();
            m_actions.Enable();
        }

        protected override void OnDestroy()
        {
            m_actions.Dispose();
        }

        protected override void OnUpdate()
        {
            var actions = m_actions.Player;

            float2 look = actions.Look.ReadValue<UnityEngine.Vector2>();
            if (!look.Equals(float2.zero))
            {
                if (actions.Look.activeControl.device is Mouse)
                {
                    UnityEngine.Cursor.lockState  = UnityEngine.CursorLockMode.Locked;
                    UnityEngine.Cursor.visible    = false;
                    var    camera                 = UnityEngine.Camera.main;
                    float3 screenPoint            = camera.ViewportToScreenPoint(new float3(0.5f, 0.5f, 1f));
                    screenPoint.xy               += look * kMouseSensitivity;
                    var    rayWorldSpace          = UnityEngine.Camera.main.ScreenPointToRay(screenPoint);
                    float3 rayCameraSpace         = camera.transform.InverseTransformDirection(rayWorldSpace.direction).normalized;
                    look                          = rayCameraSpace.xy;
                }
                else
                {
                    look *= kControllerSensitivity * SystemAPI.Time.DeltaTime;
                }
            }

            float2 move    = actions.Move.ReadValue<UnityEngine.Vector2>();
            bool   jump    = actions.Jump.IsPressed();
            bool   fire    = actions.Fire.WasPressedThisFrame();
            bool   reload  = actions.Reload.WasPressedThisFrame();
            bool   restart = actions.Restart.WasPressedThisFrame();

            var fpda = new FirstPersonDesiredActions
            {
                lookDirectionFromForward = look,
                move                     = move,
                jump                     = jump,
                fire                     = fire,
                reload                   = reload,
                restart                  = restart,
            };

            foreach (var entityActions in SystemAPI.Query<RefRW<FirstPersonDesiredActions> >().WithAll<PlayerTag>())
            {
                entityActions.ValueRW = fpda;
            }
        }
    }
}

