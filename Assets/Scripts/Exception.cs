// Commented 18/4
using System;


/// <summary>
/// Thrown when a warning log has no access to the Unity UI.
/// Can be used to detect when a warning log would have occurred in normal execution,
/// if the UI isn't running.
/// </summary>
public sealed class WarnException : Exception { }