// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public delegate void EventHandler(object? sender, EventArgs e);

    public delegate void EventHandler<in TEventArgs>(object? sender, TEventArgs e) // Removed TEventArgs constraint post-.NET 4
        where TEventArgs : allows ref struct;

    /// <summary>
    /// Represents the method that will handle an event when the event provides data.
    /// </summary>
    /// <typeparam name="TSender">The type of the object raising the event.</typeparam>
    /// <typeparam name="TEventArgs">The type of the event data generated by the event.</typeparam>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An object that contains the event data.</param>
    public delegate void EventHandler<in TSender, in TEventArgs>(TSender sender, TEventArgs e)
        where TSender : allows ref struct
        where TEventArgs : allows ref struct;
}
