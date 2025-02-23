using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Utils
{
    public static float GetAnimatorClipLength(Animator animator, string clipName)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        var clip = controller.animationClips.FirstOrDefault(c => string.Equals(c.name, clipName, StringComparison.OrdinalIgnoreCase));

        if (clip == null)
            throw new KeyNotFoundException($"Clip '{clipName}' not found in Animator '{animator.name}'");

        return clip.length;
    }
}