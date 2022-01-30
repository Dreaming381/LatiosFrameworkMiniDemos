using Latios;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

namespace Dragons
{
    public class PlayerGameplayReadInputSystem : SubSystem
    {
        UnityEngine.Camera camera;

        protected override void OnUpdate()
        {
            if (camera == null)
            {
                camera = UnityEngine.Camera.main;
            }

            var mouse    = Mouse.current;
            var keyboard = Keyboard.current;

            Entities.WithAll<PlayerTag>().ForEach((ref DesiredActions desiredActions, in PlayerInputStats stats) =>
            {
                if (mouse != null && keyboard != null)
                {
                    UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
                    //UnityEngine.Cursor.visible    = false;
                    float2 mouseDelta  = mouse.delta.ReadValue();
                    mouseDelta        *= new float2(1920f, 1080f) / new float2(UnityEngine.Screen.width, UnityEngine.Screen.height);
                    mouseDelta        *= math.radians(camera.fieldOfView) / 1080f;  //FOV is 80
                    mouseDelta        /= Time.DeltaTime;
                    mouseDelta        *= stats.mouseDeltaFactors;

                    desiredActions.turn = mouseDelta;

                    UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
                    //UnityEngine.Cursor.visible   = false;
                    float2 move   = default;
                    bool   w      = keyboard.wKey.isPressed;
                    bool   a      = keyboard.aKey.isPressed;
                    bool   s      = keyboard.sKey.isPressed;
                    bool   d      = keyboard.dKey.isPressed;
                    bool   c      = keyboard.cKey.isPressed;
                    bool   lctrl  = keyboard.leftCtrlKey.isPressed;
                    bool   lshift = keyboard.leftShiftKey.isPressed;
                    bool   r      = keyboard.rKey.isPressed;
                    bool   space  = keyboard.spaceKey.isPressed;
                    bool   b      = keyboard.bKey.wasPressedThisFrame;
                    var    scroll = mouse.scroll.y.ReadValue();

                    move.x               += math.select(0f, 1f, d);
                    move.x               -= math.select(0f, 1f, a);
                    move.y               += math.select(0f, 1f, w);
                    move.y               -= math.select(0f, 1f, s);
                    bool leftMouseButton  = mouse.leftButton.isPressed;

                    var moveLength = math.length(move);

                    desiredActions.move   = math.select(move, move / moveLength, moveLength > 0f);
                    desiredActions.fire   = leftMouseButton;
                    desiredActions.jump   = space;
                    desiredActions.crouch = c || lctrl || lshift;
                    desiredActions.reload = r;
                }
            }).WithoutBurst().Run();
        }
    }
}

