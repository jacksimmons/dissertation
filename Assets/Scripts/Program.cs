using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


public enum Winner
{
    None = 0,
    Left = 1, // == Left | None
    Right = 2, // == Right | None
    Draw = 3 // == Left | Right
}