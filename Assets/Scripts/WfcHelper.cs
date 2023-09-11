using System.Collections.Generic;
using UnityEngine;

public static class WfcHelper
{
    #region Colors -----------------------------------------------------------------

    public static byte GetColorHash(Dictionary<Color, byte> _colors, Color _newColor)
    {
        if (_colors.TryGetValue(_newColor, out byte existingByte))
        {
            return existingByte;
        }
        else
        {
            if (_colors.Count > 255)
            {
                Debug.LogError("Ran out of unique byte identifiers for colors!");
                return 0; // Return some default value
            }

            return (byte)_colors.Count;
        }
    }

    public static bool IsColorAlmostEqual(Color _this, Color _other, float _precision)
    {
        if (Mathf.Abs(_this.r - _other.r) > _precision)
            return false;

        if (Mathf.Abs(_this.g - _other.g) > _precision)
            return false;

        if (Mathf.Abs(_this.b - _other.b) > _precision)
            return false;

        if (Mathf.Abs(_this.a - _other.a) > _precision)
            return false;

        return true;
    }

    public static Color MeanColor(List<Color> _colors)
    {
        if (_colors.Count == 0)
        {
            return Color.black; // Return a default color when the list is empty
        }

        float sumR = 0f;
        float sumG = 0f;
        float sumB = 0f;
        float sumA = 0f;

        foreach (Color color in _colors)
        {
            sumR += color.r;
            sumG += color.g;
            sumB += color.b;
            sumA += color.a;
        }

        float count = _colors.Count;
        return new Color(sumR / count, sumG / count, sumB / count, sumA / count);
    }
    #endregion Colors-----------------------------------------------------------------

    #region Initialize Patterns -----------------------------------------------------------------
    public static byte[] GetOriginalPattern(byte[,] _textureColorMatrix, int _xIndex, int _yIndex, int _waveLength)
    {
        byte[] originalPattern = new byte[_waveLength * _waveLength];

        int xMin = _xIndex - 1;
        int xMax = _xIndex + 1;
        int yMin = _yIndex - 1;
        int yMax = _yIndex + 1;

        int inputTextureXSize = _textureColorMatrix.GetLength(0);
        int inputTextureYSize = _textureColorMatrix.GetLength(1);

        int xOut = 0;
        int yOut = 0;

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                xOut = x;
                yOut = y;

                if (x >= inputTextureXSize)
                    xOut = x - inputTextureXSize;
                if (y >= inputTextureYSize)
                    yOut = y - inputTextureYSize;

                originalPattern[x - xMin + (y - yMin) * _waveLength] = _textureColorMatrix[xOut, yOut];
            }
        }

        return originalPattern;
    }

    public static byte[] CreatePatternTurn90Degree(byte[] _patternMatrix, int _waveLength)
    {
        byte[] turned90 = new byte[_waveLength * _waveLength];

        for (int xOut = 0, yIn = _waveLength - 1; xOut < _waveLength; xOut++, yIn--)
        {
            for (int yOut = 0, xIn = 0; yOut < _waveLength; yOut++, xIn++)
            {
                turned90[xOut + yOut * _waveLength] = _patternMatrix[xIn + yIn * _waveLength];
            }
        }

        return turned90;
    }

    public static byte[] CreatePatternTurn180Degree(byte[] _patternMatrix, int _waveLength)
    {
        byte[] turned180 = new byte[_waveLength * _waveLength];

        for (int xOut = 0, xIn = _waveLength - 1; xOut < _waveLength; xOut++, xIn--)
        {
            for (int yOut = 0, yIn = _waveLength - 1; yOut < _waveLength; yOut++, yIn--)
            {
                turned180[xOut + yOut * _waveLength] = _patternMatrix[xIn + yIn * _waveLength];
            }
        }

        return turned180;
    }

    public static byte[] CreatePatternTurn270Degree(byte[] _patternMatrix, int _waveLength)
    {
        byte[] turned270 = new byte[_waveLength * _waveLength];

        for (int xOut = 0, yIn = 0; xOut < _waveLength; xOut++, yIn++)
        {
            for (int yOut = 0, xIn = _waveLength - 1; yOut < _waveLength; yOut++, xIn--)
            {
                turned270[xOut + yOut * _waveLength] = _patternMatrix[xIn + yIn * _waveLength];
            }
        }

        return turned270;
    }

    /// <summary>
    /// WARNING HARDCODED BS
    /// </summary>
    /// <param name="_patternMatrix"></param>
    /// <param name="_waveLength"></param>
    /// <returns></returns>
    public static byte[] CreatePatternHorizontalFlip(byte[] _patternMatrix, int _waveLength)
    {
        byte[] horizontalFlip = new byte[_waveLength * _waveLength];
        for (int y = 0; y < _waveLength; y++)
        {
            for (int x = 0; x < _waveLength; x++)
            {
                if (x == 0)
                    horizontalFlip[x + y * _waveLength] = _patternMatrix[2 + y * _waveLength];
                else if (x == 2)
                    horizontalFlip[x + y * _waveLength] = _patternMatrix[0 + y * _waveLength];
                else
                    horizontalFlip[x + y * _waveLength] = _patternMatrix[x + y * _waveLength];
            }
        }
        return horizontalFlip;
    }

    /// <summary>
    /// WARNING HARDCODED BS
    /// </summary>
    /// <param name="_patternMatrix"></param>
    /// <param name="_waveLength"></param>
    /// <returns></returns>
    public static byte[] CreatePatternVerticalFlip(byte[] _patternMatrix, int _waveLength)
    {
        byte[] verticalFlip = new byte[_waveLength * _waveLength];
        for (int x = 0; x < _waveLength; x++)
        {
            for (int y = 0; y < _waveLength; y++)
            {
                if (y == 0)
                    verticalFlip[x + y * _waveLength] = _patternMatrix[x + 2 * _waveLength];
                else if (y == 2)
                    verticalFlip[x + y * _waveLength] = _patternMatrix[x + 0 * _waveLength];
                else
                    verticalFlip[x + y * _waveLength] = _patternMatrix[x + y * _waveLength];
            }
        }
        return verticalFlip;
    }
    #endregion Initialize Patterns -----------------------------------------------------------------

    public static bool OverlapsInDirection(byte[] _p1, byte[] _p2, int _dx, int _dy, int _waveLength)
    {
        int xStart = (_dx <= 0) ? 0 : 1;
        int xMaxExc = (_dx >= 0) ? _waveLength : _waveLength - 1;

        int yStart = (_dy >= 0) ? 0 : 1;
        int yMaxExc = (_dy > 0) ? _waveLength - 1 : _waveLength;

        for (int x = xStart; x < xMaxExc; x++)
        {
            for (int y = yStart; y < yMaxExc; y++)
            {
                if (_p1[x + _waveLength * y] != _p2[x - _dx + _waveLength * (y + _dy)])
                    return false;
            }
        }

        return true;
    }

    public static double Entropy(int _sumOfPossiblePatternWeights, double _sumOfPossiblePatternLogWeights)
    {
        return Mathf.Log(_sumOfPossiblePatternWeights, 2) - (_sumOfPossiblePatternLogWeights / _sumOfPossiblePatternWeights);
    }

    public static double NoisedEntropy(int _sumOfPossiblePatternWeights,
        double _sumOfPossiblePatternLogWeights, double _noiseMaxRange, System.Random _prng)
    {
        return Entropy(_sumOfPossiblePatternWeights, _sumOfPossiblePatternLogWeights) + _prng.NextDouble() * _noiseMaxRange;
    }

    public static Texture2D ToTextureApplied(this Color[] _colorAr, int _width, int _height,
            FilterMode _filterMode = FilterMode.Point, TextureWrapMode _wrapMode = TextureWrapMode.Clamp)
    {
        Texture2D texture = new Texture2D(_width, _height);
        texture.filterMode = _filterMode;
        texture.wrapMode = _wrapMode;
        texture.SetPixels(_colorAr);
        texture.Apply();
        return texture;
    }
}