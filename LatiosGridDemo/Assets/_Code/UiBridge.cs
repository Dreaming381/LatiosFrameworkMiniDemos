using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    public class UiBridge : MonoBehaviour
    {
        public bool receivedAction;
        public bool attack;
        public bool move;
        public int2 direction;

        public void SetAttack()
        {
            receivedAction = true;
            attack         = true;
        }

        public void SetMove()
        {
            receivedAction = true;
            move           = true;
        }

        public void SetDirectionX(int x)
        {
            direction.x = x;
        }

        public void SetDirectionY(int y)
        {
            direction.y = y;
        }
    }
}

