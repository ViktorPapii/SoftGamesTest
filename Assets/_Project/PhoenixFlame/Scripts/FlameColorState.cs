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
            // A StateMachineBehaviour has no scene reference to serialize, so this is the one
            // lookup that has to happen at run time. Silently doing nothing would leave the fire
            // stuck on its first colour with no clue why.
            if (!animator.TryGetComponent(out FlameColorController controller))
            {
                Debug.LogError($"No {nameof(FlameColorController)} on {animator.name}; " +
                               "the colour cycle cannot run.", animator);
                return;
            }

            controller.BeginFadeTo(stateInfo.shortNameHash);
        }
    }
}
