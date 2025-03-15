using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace Controls
{
    /// <summary>
    /// Contains player input information.
    /// Usage of a raw composite allows to unite
    /// and handle mouse/pointer and touch inputs
    /// equally.
    /// </summary>
    public struct PointerInput
    {
        /// <summary>
        /// Some form of contact with the screen.
        /// Can be a mouse button or a finger.
        /// </summary>
        public bool Contact;
        /// <summary>
        /// InputId to differentiate the input control.
        /// </summary>
        public int InputId;
        /// <summary>
        /// Current position in screen coordinates.
        /// </summary>
        public Vector2 Position;
    }

    /// <summary>
    /// Custom composite for the new InputSystem that allows combined
    /// input of some contact (i.e. mouse button/finger) and its
    /// position to be consumed in the even handlers.
    /// </summary>
    /// <remarks>
    /// Automatically registered in the Editor.
    /// </remarks>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PointerInputComposite : InputBindingComposite<PointerInput>
    {
        /// <summary>
        /// Required in Editor v6
        /// </summary>
        public int Mode;

        [InputControl(layout = "Button")]
        public int Contact;

        [InputControl(layout = "Vector2")]
        public int Position;

        [InputControl(layout = "Integer")]
        public int Id;

        public override PointerInput ReadValue(ref InputBindingCompositeContext context)
        {
            return new PointerInput
            {
                Contact = context.ReadValueAsButton(Contact),
                InputId = context.ReadValue<int>(Id),
                Position = context.ReadValue<Vector2, Vector2MagnitudeComparer>(Position)
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            InputSystem.RegisterBindingComposite<PointerInputComposite>();
        }

#if UNITY_EDITOR
        static PointerInputComposite()
        {
            Register();
        }
#endif
    }

}