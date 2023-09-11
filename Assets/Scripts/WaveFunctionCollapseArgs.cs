using UnityEngine;

public struct WaveFunctionCollapseArgs
{
    public int XSize { get; set; }
    public int YSize { get; set; }
    public int Seed { get; set; }
    public bool Reflections { get; set; }
    public Texture2D InputTexture { get; set; }
    public int LimitInputX { get; set; }
    public int LimitInputY { get; set; }
    public bool Debugging { get; set; }

    public WaveFunctionCollapseArgs(int _void = 0)
    {
        XSize = 0;
        YSize = 0;
        Seed = 0;
        Reflections = false;
        InputTexture = null;
        LimitInputX = 0;
        LimitInputY = 0;
        Debugging = false;
    }
}