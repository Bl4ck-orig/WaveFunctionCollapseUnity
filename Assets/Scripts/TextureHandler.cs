using System;
using UnityEngine;

public abstract class TextureHandler
{
    private Texture2D texture;
    private Color[,] colorMatrix;
    protected int width = 0;
    protected int height = 0;

    public TextureHandler(Color[,] _colorMatrix)
    {
        width = _colorMatrix.GetLength(0);
        height = _colorMatrix.GetLength(1);

        colorMatrix = GetColorMatrix(_colorMatrix);
        texture = GetTexture(_colorMatrix);
    }

    public TextureHandler(Texture2D _texture)
    {
        width = _texture.width;
        height = _texture.height;

        colorMatrix = GetColorMatrix(_texture);
        texture = GetTexture(_texture);
    }

    public abstract Color[,] GetColorMatrix(Color[,] _colorMatrix);

    public abstract Color[,] GetColorMatrix(Texture2D _texture);

    public abstract Texture2D GetTexture(Color[,] _colorMatrix);

    public abstract Texture2D GetTexture(Texture2D _texture);

    public Color GetPixel(int _x, int _y) => colorMatrix[_x, width - _y - 1];

    public Color[,] GetColorMatrix() => colorMatrix;

    public Texture2D GetAppliedTexture(FilterMode _filterMode = FilterMode.Point,
        TextureWrapMode _wrapMode = TextureWrapMode.Clamp)
    {
        texture.filterMode = _filterMode;
        texture.wrapMode = _wrapMode;
        texture.Apply();
        return texture;
    }
}

public class TextureHandlerReversed : TextureHandler
{
    public TextureHandlerReversed(Color[,] _colorMatrix) : base(_colorMatrix)
    {
    }

    public TextureHandlerReversed(Texture2D _texture) : base(_texture)
    {
    }

    public override Color[,] GetColorMatrix(Color[,] _colorMatrix)
    {
        if (_colorMatrix.GetLength(0) != width)
            throw new ArgumentException();

        if (_colorMatrix.GetLength(1) != height)
            throw new ArgumentException();

        return _colorMatrix;
    }

    public override Color[,] GetColorMatrix(Texture2D _texture)
    {
        if (_texture.width != width)
            throw new ArgumentException();

        if (_texture.height != height)
            throw new ArgumentException();

        Color[,] colorMatrix = new Color[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int yOut = 0, yIn = height - 1; yIn >= 0; yOut++, yIn--)
            {
                colorMatrix[x, yOut] = _texture.GetPixel(x, yIn);
            }
        }

        return colorMatrix;
    }

    public override Texture2D GetTexture(Color[,] _colorMatrix)
    {
        if (_colorMatrix.GetLength(0) != width)
            throw new ArgumentException();

        if (_colorMatrix.GetLength(1) != height)
            throw new ArgumentException();

        Texture2D texture = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int yIn = 0, yOut = height - 1; yOut >= 0; yIn++, yOut--)
            {
                texture.SetPixel(x, yOut, _colorMatrix[x, yIn]);
            }
        }

        return texture;
    }

    public override Texture2D GetTexture(Texture2D _texture) => _texture;
}

public class TextureHandlerNonReversed : TextureHandler
{
    public TextureHandlerNonReversed(Color[,] _colorMatrix) : base(_colorMatrix)
    {
    }

    public TextureHandlerNonReversed(Texture2D _texture) : base(_texture)
    {
    }

    public override Color[,] GetColorMatrix(Color[,] _colorMatrix)
    {
        if (_colorMatrix.GetLength(0) != width)
            throw new ArgumentException();

        if (_colorMatrix.GetLength(1) != height)
            throw new ArgumentException();

        return _colorMatrix;
    }

    public override Color[,] GetColorMatrix(Texture2D _texture)
    {
        if (_texture.width != width)
            throw new ArgumentException();

        if (_texture.height != height)
            throw new ArgumentException();

        Color[,] colorMatrix = new Color[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colorMatrix[x, y] = _texture.GetPixel(x, y);
            }
        }

        return colorMatrix;
    }

    public override Texture2D GetTexture(Color[,] _colorMatrix)
    {
        if (_colorMatrix.GetLength(0) != width)
            throw new ArgumentException();

        if (_colorMatrix.GetLength(1) != height)
            throw new ArgumentException();

        Texture2D texture = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int yIn = 0, yOut = height - 1; yOut >= 0; yIn++, yOut--)
            {
                texture.SetPixel(x, yOut, _colorMatrix[x, yIn]);
            }
        }

        return texture;
    }

    public override Texture2D GetTexture(Texture2D _texture) => _texture;
}