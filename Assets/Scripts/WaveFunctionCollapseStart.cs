using LevelEditing.WaveFunctionCollapsing;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class WaveFunctionCollapseStart : MonoBehaviour
{
    [SerializeField] private RawImage image;
    [Header("Arguments")]
    [SerializeField] private int xSize = 32;
    [SerializeField] private int ySize = 32;
    [SerializeField] private int seed = 0;
    [SerializeField] private bool reflections = true;
    [SerializeField] private Texture2D inputTexture;
    [SerializeField] private int limitInputX = 0;
    [SerializeField] private int limitInputY = 0;
    [SerializeField] private bool debugging = true;

    public void Run()
    {
        WaveFunctionCollapseArgs args = new WaveFunctionCollapseArgs();
        args.XSize = xSize;
        args.YSize = ySize;
        args.Seed = seed;
        args.Reflections = reflections;
        args.InputTexture = inputTexture;
        args.LimitInputX = limitInputX;
        args.LimitInputY = limitInputY;
        args.Debugging = debugging;

        Wfc waveFunctionCollapseNew = new Wfc();
        Color[] pixels = waveFunctionCollapseNew.Run(args);
        image.texture = pixels.ToTextureApplied(args.XSize, args.YSize);
        image.enabled = true;
    }
}