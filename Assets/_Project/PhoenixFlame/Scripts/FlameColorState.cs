using UnityEngine;

namespace SoftGames.PhoenixFlame
{
    /// <summary>
    /// Sits on every colour state and names it to the controller as the state is entered. This is
    /// what puts the cycle in the controller graph instead of in C#: the states, their order and
    /// their names decide what the fire turns into next.
    /// </summary>
    public class FlameColorState : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent(out FlameColorController controller))
            {
                controller.BeginFadeTo(stateInfo.shortNameHash);
            }
        }
    }
}
