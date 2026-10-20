using PawPath.Core;
using UnityEngine;

namespace PawPath.Levels
{
    /// <summary>Suspends only mob physics while the player prepares or pauses a level.</summary>
    public sealed class MobSimulationGate : MonoBehaviour
    {
        GameFlow flow;
        Rigidbody2D body;
        bool suspended;
        bool wasSimulated;

        public static bool GameplayActive => GameFlow.Instance != null &&
            GameFlow.Instance.State == GameFlowState.Gameplay;

        void OnEnable()
        {
            body = GetComponent<Rigidbody2D>();
            flow = GameFlow.Instance;
            if (flow == null) return;
            flow.StateChanged += OnStateChanged;
            OnStateChanged(flow.State);
        }

        void OnStateChanged(GameFlowState state)
        {
            if (body == null) return;
            if (state != GameFlowState.Gameplay)
            {
                if (!suspended)
                {
                    wasSimulated = body.simulated;
                    suspended = true;
                }
                body.simulated = false;
            }
            else RestoreSimulation();
        }

        void RestoreSimulation()
        {
            if (!suspended) return;
            if (body != null) body.simulated = wasSimulated;
            suspended = false;
        }

        void OnDisable()
        {
            if (flow != null) flow.StateChanged -= OnStateChanged;
            RestoreSimulation();
            flow = null;
        }
    }
}
