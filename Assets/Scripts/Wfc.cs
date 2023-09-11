using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LevelEditing.WaveFunctionCollapsing
{
    public class Wfc
    {
        private const int WAVELENGTH = 3;
        private const float UNITIALIZED_ALPHA_VALUE = 0.001f;
        private const float COLOR_COMPARISON_PRECISION = 0.001f;
        private const double ENTROPY_NOISE_MAX_RANGE = 0.0000001f;

        private Color32 currentCellColor = Color.yellow;
        private Color32 maskColor = new Color32(1, 2, 3, 255);
        private Color32 uninitializedColor = Color.clear;
        private static int[] opposite = { 2, 3, 0, 1 };
        private static int[] directionX = { -1, 0, 1, 0 };
        private static int[] directionY = { 0, 1, 0, -1 };

        private int xSize = 0;
        private int ySize = 0;
        private int limitInputX = 0;
        private int limitInputY = 0;
        private int inputTextureXSize = 0;
        private int inputTextureYSize = 0;
        private bool reflections = false;

        private Dictionary<byte, Color> colorsOfHashes;
        private Dictionary<Color, byte> hashesOfColors;

        private System.Random prng;
        private int removals = 0;
        private int propagationSteps = 0;
        private int steps = 0;
        private Stack<RemovalUpdate> removalUpdates;

        private List<byte[]> patterns;
        private List<double> patternWeights;
        private int[][][] propagators;

        private int sumOfPossibleWeightsAtStart = 0;
        private double sumOfPossibleLogWeightsAtStart = 0f;

        private bool[][] waveCells;
        private int[][][] compatibles;
        private int[] sumOfPossiblePatterns;
        private MinHeap<int> uncollapsedEntropies;
        private double[] sumOfWeights;
        private double[] sumOfLogWeights;

        private System.Diagnostics.Stopwatch sw;

        public Color[] Run(WaveFunctionCollapseArgs _args)
        {
            Initialize(_args);
            sw = System.Diagnostics.Stopwatch.StartNew();
            Color[] map = Execute();
            sw.Stop();
            if (_args.Debugging)
            {
                Debug.Log("Steps: " + steps + ", Removals: " + removals + ", Propagation Steps: " + propagationSteps + ", elapsed: " + sw.Elapsed);
            }

            return map;
        }

        #region Initialization -----------------------------------------------------------------
        private void Initialize(WaveFunctionCollapseArgs _args)
        {
            if (_args.InputTexture.width < WAVELENGTH || _args.InputTexture.height < WAVELENGTH)
                throw new ArgumentException("Input texture too small.");

            if (_args.XSize <= 0 || _args.YSize <= 0)
                throw new ArgumentException("Requested output size too small.");

            xSize = _args.XSize;
            ySize = _args.YSize;
            limitInputX = _args.LimitInputX;
            limitInputY = _args.LimitInputY;
            reflections = _args.Reflections;

            prng = (_args.Seed == 0) ? new System.Random() : new System.Random(_args.Seed);

            steps = 0;
            removals = 0;
            propagationSteps = 0;

            TextureHandler inputTextureHandler = new TextureHandlerReversed(_args.InputTexture);
            Color[,] inputColorMatrix = inputTextureHandler.GetColorMatrix();

            inputTextureXSize = inputColorMatrix.GetLength(0);
            inputTextureYSize = inputColorMatrix.GetLength(1);

            InitializePatterns(inputColorMatrix);

            DistinctAndCountPatterns();

            InitializeAdjacencies();

            InitializeMap();

            InitializeWeights();

            removalUpdates = new Stack<RemovalUpdate>();
        }

        #region Initialize Colors & Patterns -----------------------------------------------------------------
        private void InitializePatterns(Color[,] _textureColorMatrix)
        {
            patterns = new List<byte[]>();

            byte[,] hashedColors = PrepareColors(_textureColorMatrix);

            for (int y = 1; y <= _textureColorMatrix.GetLength(1) - limitInputY; y++)
            {
                for (int x = 1; x <= _textureColorMatrix.GetLength(0) - limitInputX; x++)
                {
                    byte[] originalPattern = WfcHelper.GetOriginalPattern(hashedColors, x, y, WAVELENGTH);
                    patterns.Add(originalPattern);

                    if (!reflections)
                        continue;

                    patterns.Add(WfcHelper.CreatePatternTurn90Degree(originalPattern, WAVELENGTH));
                    patterns.Add(WfcHelper.CreatePatternTurn180Degree(originalPattern, WAVELENGTH));
                    patterns.Add(WfcHelper.CreatePatternTurn270Degree(originalPattern, WAVELENGTH));
                    patterns.Add(WfcHelper.CreatePatternHorizontalFlip(originalPattern, WAVELENGTH));
                    patterns.Add(WfcHelper.CreatePatternVerticalFlip(originalPattern, WAVELENGTH));
                }
            }
        }

        private byte[,] PrepareColors(Color[,] _textureColorMatrix)
        {
            colorsOfHashes = new Dictionary<byte, Color>();
            hashesOfColors = new Dictionary<Color, byte>();

            TryAddNewColorToDicts(maskColor);
            TryAddNewColorToDicts(uninitializedColor);

            byte[,] hashedColors = new byte[inputTextureXSize, inputTextureYSize];

            for (int x = 0; x < inputTextureXSize; x++)
            {
                for (int y = 0; y < inputTextureYSize; y++)
                {
                    if (!hashesOfColors.ContainsKey(_textureColorMatrix[x, y]))
                        TryAddNewColorToDicts(_textureColorMatrix[x, y]);

                    hashedColors[x, y] = hashesOfColors[_textureColorMatrix[x, y]];
                }
            }

            return hashedColors;
        }

        private void TryAddNewColorToDicts(Color _color)
        {
            byte colorHash = (byte)hashesOfColors.Count;
            if (colorsOfHashes.ContainsKey(colorHash))
                throw new ArgumentException("Color " + _color + " already present in colorsOfHashes dict!");
            if (hashesOfColors.ContainsKey(_color))
                throw new ArgumentException("Color " + _color + " already present in colorsOfHashes dict!");

            colorsOfHashes[colorHash] = _color;
            hashesOfColors[_color] = colorHash;
        }

        private void DistinctAndCountPatterns()
        {
            patternWeights = new List<double>();

            int totalPatternsAmount = patterns.Count;
            int listEnd = patterns.Count;

            List<byte[]> patternsCopy = patterns.ToList();

            for (int i = 0; i < listEnd; i++)
            {
                List<byte[]> equalPatterns = patterns
                    .Where(x => patterns[i].SequenceEqual(x))
                    .ToList();

                equalPatterns.Remove(patterns[i]);

                if (equalPatterns.Count > 0)
                {
                    equalPatterns.ForEach(x => patterns.Remove(x));
                    equalPatterns.ForEach(x => patternsCopy.Remove(x));
                    listEnd = patterns.Count;
                }

                patternWeights.Add(equalPatterns.Count + 1);
            }
        }
        #endregion Initialize Colors & Patterns  -----------------------------------------------------------------

        #region Map Initialization -----------------------------------------------------------------
        private void InitializeAdjacencies()
        {
            propagators = new int[4][][];

            for (int d = 0; d < 4; d++)
            {
                propagators[d] = new int[patterns.Count][];

                for (int t1 = 0; t1 < patterns.Count; t1++)
                {
                    List<int> propagateablePatterns = new List<int>();

                    if (d == 1)
                        d = 1;

                    for (int t2 = 0; t2 < patterns.Count; t2++)
                    {
                        if (WfcHelper.OverlapsInDirection(patterns[t1], patterns[t2], directionX[d], directionY[d], WAVELENGTH))
                            propagateablePatterns.Add(t2);
                    }

                    propagators[d][t1] = new int[propagateablePatterns.Count];

                    for (int pP = 0; pP < propagateablePatterns.Count; pP++)
                        propagators[d][t1][pP] = propagateablePatterns[pP];
                }
            }
        }

        private void InitializeMap()
        {
            waveCells = new bool[xSize * ySize][];
            compatibles = new int[waveCells.Length][][];
            sumOfPossiblePatterns = new int[waveCells.Length];

            for (int i = 0; i < waveCells.Length; i++)
            {
                waveCells[i] = new bool[patterns.Count];
                compatibles[i] = new int[patterns.Count][];

                for (int t = 0; t < patterns.Count; t++)
                {
                    waveCells[i][t] = true;
                    compatibles[i][t] = new int[4];
                    sumOfPossiblePatterns[i] = patterns.Count;

                    for (int d = 0; d < 4; d++)
                    {
                        compatibles[i][t][d] = propagators[d][t].Length;
                    }
                }
            }
        }

        private void InitializeWeights()
        {
            for (int i = 0; i < patternWeights.Count; i++)
            {
                sumOfPossibleWeightsAtStart += (int)patternWeights[i];
                sumOfPossibleLogWeightsAtStart += Mathf.Log((float)patternWeights[i], 2) * patternWeights[i];
            }

            double entropyAtStart = WfcHelper.Entropy(sumOfPossibleWeightsAtStart, sumOfPossibleLogWeightsAtStart);

            sumOfWeights = new double[waveCells.Length];
            sumOfLogWeights = new double[waveCells.Length];
            uncollapsedEntropies = new MinHeap<int>();

            for (int i = 0; i < waveCells.Length; i++)
            {
                sumOfWeights[i] = sumOfPossibleWeightsAtStart;
                sumOfLogWeights[i] = sumOfPossibleLogWeightsAtStart;
                uncollapsedEntropies.Add(entropyAtStart, i);
            }
        }

        #endregion Map Initialization -----------------------------------------------------------------

        #endregion Initialization -----------------------------------------------------------------

        #region Execution -----------------------------------------------------------------
        private Color[] Execute()
        {
            int nextCell = GetRandomRange(0, xSize * ySize - 1);

            while (!uncollapsedEntropies.IsEmpty())
            {
                steps++;

                removalUpdates = CollapseCell(nextCell);

                try
                {
                    Propagate(removalUpdates);
                }
                catch (ContradictionException ex)
                {
                    Debug.LogError(ex);
                    break;
                }

                nextCell = PickNextCell();
            }

            return CreateColorMap();
        }

        #region Collapse Cell -----------------------------------------------------------------
        private Stack<RemovalUpdate> CollapseCell(int _cellIndex)
        {
            int patternRandom = GetPatternIndexRandomStripBased(_cellIndex);
            return CollapseCellByPattern(_cellIndex, patternRandom);
        }

        private int GetPatternIndexRandomStripBased(int _cellIndex)
        {
            if (sumOfPossiblePatterns[_cellIndex] == 0)
                throw new ContradictionException("No propagateable patterns at " + _cellIndex);

            if (sumOfPossiblePatterns[_cellIndex] == 1)
                return GetPropagateablePatternIndices(_cellIndex).First();

            double sumPossibleOccurences = 0;

            for (int i = 0; i < patternWeights.Count; i++)
            {
                if (!waveCells[_cellIndex][i])
                    continue;

                sumPossibleOccurences += patternWeights[i];
            }

            List<int> possiblePatternIndices = GetPropagateablePatternIndices(_cellIndex);

            double remaining = GetRandomRange(0, (int)sumOfWeights[_cellIndex] - 1);
            for (int i = 0; i < sumOfPossiblePatterns[_cellIndex]; i++)
            {
                double weight = patternWeights[possiblePatternIndices[i]];
                if (remaining >= weight)
                    remaining -= weight;
                else
                    return possiblePatternIndices[i];
            }

            throw new ContradictionException("No propagateable patterns at " + _cellIndex);
        }

        private List<int> GetPropagateablePatternIndices(int _cellIndex)
        {
            List<int> propagateablePatterns = new List<int>();

            for (int i = 0; i < waveCells[_cellIndex].Length; i++)
            {
                if (waveCells[_cellIndex][i])
                    propagateablePatterns.Add(i);
            }

            return propagateablePatterns;
        }

        private Stack<RemovalUpdate> CollapseCellByPattern(int _cellIndex, int _pattern)
        {
            Stack<RemovalUpdate> removals = new Stack<RemovalUpdate>();

            for (int i = 0; i < patternWeights.Count; ++i)
            {
                if (i == _pattern)
                    continue;

                if (waveCells[_cellIndex][i])
                {
                    waveCells[_cellIndex][i] = false;

                    int yIndex = _cellIndex / xSize;
                    int xIndex = _cellIndex - yIndex * xSize;

                    removals.Push(new RemovalUpdate(_cellIndex, i, xIndex, yIndex));
                }
            }

            sumOfPossiblePatterns[_cellIndex] = 1;

            return removals;
        }
        #endregion Collapse Cell -----------------------------------------------------------------

        #region Propagation -----------------------------------------------------------------
        private void Propagate(Stack<RemovalUpdate> _removals)
        {
            while (_removals.Count > 0)
            {
                RemovalUpdate update = _removals.Pop();

                PropagateUpdate(update);
            }
        }

        private void PropagateUpdate(RemovalUpdate _update)
        {
            List<RemovalUpdate> updatesOfNeighbours = new List<RemovalUpdate>();

            for (int d = 0; d < 4; d++)
            {
                int xNeighbour = directionX[d] + _update.XIndex;
                int yNeighbour = directionY[d] + _update.YIndex;

                if (xNeighbour < 0 || xNeighbour >= xSize || yNeighbour < 0 || yNeighbour >= ySize)
                    continue;

                int neighbourIndex = xNeighbour + yNeighbour * xSize;

                if (sumOfPossiblePatterns[neighbourIndex] <= 1)
                    continue;

                propagationSteps++;

                if (!PropagateToCell(neighbourIndex, xNeighbour, yNeighbour, _update, out List<RemovalUpdate> nextUpdates))
                    continue;

                UpdateEntropies(neighbourIndex);

                if (nextUpdates.Count > 0)
                    updatesOfNeighbours = updatesOfNeighbours.Concat(nextUpdates).ToList();
            }

            if (updatesOfNeighbours.Count > 0)
                updatesOfNeighbours.ForEach(x => removalUpdates.Push(x));
        }

        private bool PropagateToCell(int _cellIndex, int _xIndex, int _yIndex, RemovalUpdate _update, out List<RemovalUpdate> _newUpdate)
        {
            _newUpdate = new List<RemovalUpdate>();

            int directionToThisCellX = _xIndex - _update.XIndex;
            int directionToThisCellY = _yIndex - _update.YIndex; 

            int d = 0;
            if (directionToThisCellX == -1)
                d = 0;
            else if (directionToThisCellY == 1)
                d = 1;
            else if (directionToThisCellX == 1)
                d = 2;
            else
                d = 3;

            int[] propageatablePatternsInThisDirection = propagators[d][_update.Pattern];

            for (int i = 0; i < propageatablePatternsInThisDirection.Length; i++)
            {
                int propagateablePattern = propageatablePatternsInThisDirection[i];

                if (!waveCells[_cellIndex][propagateablePattern])
                    continue;

                compatibles[_cellIndex][propagateablePattern][opposite[d]]--;

                if (compatibles[_cellIndex][propagateablePattern][opposite[d]] > 0)
                    continue;

                _newUpdate.Add(new RemovalUpdate(_cellIndex, propagateablePattern, _xIndex, _yIndex));
                RemovePatternForCell(_cellIndex, propagateablePattern);
                removals++;
            }

            if (sumOfPossiblePatterns[_cellIndex] <= 0)
                throw new ContradictionException(_xIndex, _yIndex);

            return _newUpdate.Count > 0;
        }

        private void RemovePatternForCell(int _cellIndex, int _pattern)
        {
            double removedPatternWeight = patternWeights[_pattern];
            sumOfWeights[_cellIndex] -= removedPatternWeight;
            sumOfLogWeights[_cellIndex] -= Math.Log(removedPatternWeight, 2) * removedPatternWeight;
            sumOfPossiblePatterns[_cellIndex]--;
            waveCells[_cellIndex][_pattern] = false;
        }

        private void UpdateEntropies(int _neighbourIndex)
        {
            double newEntropy = WfcHelper.NoisedEntropy((int)sumOfWeights[_neighbourIndex], sumOfLogWeights[_neighbourIndex], ENTROPY_NOISE_MAX_RANGE, prng);
            uncollapsedEntropies.Add(newEntropy, _neighbourIndex);
        }
        #endregion -----------------------------------------------------------------

        private int PickNextCell()
        {
            while (!uncollapsedEntropies.IsEmpty())
            {
                Tuple<double, int> nextCell = uncollapsedEntropies.Poll();

                if (sumOfPossiblePatterns[nextCell.Item2] >= 1)
                    return nextCell.Item2;
            }

            return 0;
        }

        #region Create Color Map -----------------------------------------------------------------
        private Color[] CreateColorMap(int _cellIndexToDisplay = -1)
        {
            Color[] colorMap = new Color[xSize * ySize];

            for (int i = 0; i < waveCells.Length; i++)
            {
                if (i == _cellIndexToDisplay)
                    colorMap[i] = currentCellColor;
                else if (sumOfPossiblePatterns[i] == 0)
                    colorMap[i] = maskColor;
                else
                    colorMap[i] = GetCellColor(i);
            }

            return colorMap;
        }

        private Color GetCellColor(int _cellIndex)
        {
            List<Color> possibleColors = new List<Color>();

            for (int i = 0; i < patterns.Count; i++)
            {
                if (waveCells[_cellIndex][i])
                    possibleColors.Add(colorsOfHashes[patterns[i][0]]);
            }

            if (possibleColors.Count == 1)
                return possibleColors[0];

            return WfcHelper.MeanColor(possibleColors);
        }
        #endregion Create Color Map -----------------------------------------------------------------

        #endregion Execution -----------------------------------------------------------------

        public int GetRandomRange(int _minInclusive, int _maxInclusive) => prng.Next(_minInclusive, _maxInclusive);

    }
}