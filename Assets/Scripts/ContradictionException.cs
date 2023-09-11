using System;

public class ContradictionException : Exception
{
    public ContradictionException() : base() { }

    public ContradictionException(int _xIndex, int _yIndex) : base("Contradiction occured at " + _xIndex + "/" + _yIndex) { }

    public ContradictionException(string message) : base(message) { }

}