using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InputSamples.Drawing
{
    /// <summary>
    /// Contains information for drag inputs.
    /// </summary>
    public struct PointerInput
    {
        public bool Contact;

        public int InputId;

        public Vector2 Position;
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PointerInputComposite : InputBindingComposite<PointerInput>
    {

#if UNITY_EDITOR
        static PointerInputComposite()
        {
            Register();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            InputSystem.RegisterBindingComposite<PointerInputComposite>();
        }

        /// <summary>
        /// Required in Editor v6
        /// </summary>
        public int mode;

        [InputControl(layout = "Button")]
        public int Contact;

        [InputControl(layout = "Vector2")]
        public int Position;

        [InputControl(name = "Input Id", layout = "Integer")]
        public int InputId;

        public override PointerInput ReadValue(ref InputBindingCompositeContext context)
        {
            return new PointerInput
            {
                Contact = context.ReadValueAsButton(Contact),
                InputId = context.ReadValue<int>(InputId),
                Position = context.ReadValue<Vector2, Vector2MagnitudeComparer>(Position),
            };
        }
    }
}
