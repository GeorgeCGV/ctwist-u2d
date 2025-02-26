using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Animator))]
public class ColorBlock : BasicBlock
{
    private static readonly int animatorColorIntParam = Animator.StringToHash("Color");

    public static readonly Color red = new Color32(255, 103, 99, 255);
    public static readonly Color blue = new Color32(109, 164, 255, 255);
    public static readonly Color white = new Color32(230, 230, 230, 255);
    public static readonly Color black = new Color32(67, 67, 67, 255);
    public static readonly Color yellow = new Color32(190, 178, 72, 255);
    public static readonly Color green = new Color32(46, 159, 99, 255);
    public static readonly Color pink = new Color32(204, 113, 184, 255);
    public static readonly Color purple = new Color32(132, 42, 195, 255);

    public enum EBlockColor
    {
        Blue,
        Red,
        Yellow,
        Green,
        White,
        Purple,
        Pink,
        Black
    }

    public static Color UnityColorFromBlockColor(EBlockColor value)
    {
        switch (value)
        {
            case EBlockColor.Red:
                return red;
            case EBlockColor.Blue:
                return blue;
            case EBlockColor.White:
                return white;
            case EBlockColor.Yellow:
                return yellow;
            case EBlockColor.Green:
                return green;
            case EBlockColor.Purple:
                return purple;
            case EBlockColor.Pink:
                return pink;
            case EBlockColor.Black:
                return black;
            default:
                throw new NotImplementedException("not supported");
        }
    }

    [SerializeField]
    public GameObject EfxOnDestroy;
    [SerializeField]
    public GameObject EfxOnAttach;
    [SerializeField]
    public AudioClip SfxAttach;

    private EBlockColor color;

    public EBlockColor ColorType
    {
        get
        {
            return color;
        }
        set
        {
            int animatorTriggerValue;
            switch (value)
            {
                case EBlockColor.Red:
                    animatorTriggerValue = 0;
                    break;
                case EBlockColor.Blue:
                    animatorTriggerValue = 1;
                    break;
                case EBlockColor.White:
                    animatorTriggerValue = 2;
                    break;
                case EBlockColor.Black:
                    animatorTriggerValue = 3;
                    break;
                case EBlockColor.Green:
                    animatorTriggerValue = 4;
                    break;
                case EBlockColor.Yellow:
                    animatorTriggerValue = 5;
                    break;
                case EBlockColor.Pink:
                    animatorTriggerValue = 6;
                    break;
                case EBlockColor.Purple:
                    animatorTriggerValue = 7;
                    break;
                default:
                    throw new NotImplementedException("not supported");
            }

            GetComponent<Animator>().SetInteger(animatorColorIntParam, animatorTriggerValue);
            Light2D light = GetComponent<Light2D>();
            if (light != null) {
                light.color = UnityColorFromBlockColor(value);
            }
            color = value;
        }
    }

    public override bool MatchesWith(GameObject obj)
    {
        if (base.MatchesWith(obj))
        {
            return true;
        }

        ColorBlock other = obj.GetComponent<ColorBlock>();
        if (other == null)
        {
            return false;
        }

        return other.color == color;
    }

    public override ParticleSystem NewDestroyEfx()
    {
        ParticleSystem particleSystem;

        GameObject efx = Instantiate(EfxOnDestroy, transform.position, Quaternion.identity);
        particleSystem = efx.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule mainModule = particleSystem.main;
        mainModule.startColor = UnityColorFromBlockColor(color);

        return particleSystem;
    }

    public override ParticleSystem NewAttachEfx()
    {
        GameObject efx = Instantiate(EfxOnAttach, transform.position, Quaternion.identity);

        return efx.GetComponent<ParticleSystem>();
    }

    public override AudioClip SfxOnAttach()
    {
        return SfxAttach;
    }
}
