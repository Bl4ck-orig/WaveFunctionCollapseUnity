public struct RemovalUpdate
{
    public int CellIndex { get; private set; }
    public int Pattern { get; private set; }
    public int XIndex { get; private set; }
    public int YIndex { get; private set; }

    public RemovalUpdate(int _cellIndex, int _pattern, int _xIndex, int _yIndex)
    {
        CellIndex = _cellIndex;
        Pattern = _pattern;
        XIndex = _xIndex;
        YIndex = _yIndex;
    }

}