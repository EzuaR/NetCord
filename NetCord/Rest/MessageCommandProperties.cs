﻿namespace NetCord.Rest;

public partial class MessageCommandProperties : ApplicationCommandProperties
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">Name of the command (1-32 characters).</param>
    public MessageCommandProperties(string name) : base(ApplicationCommandType.Message, name)
    {
    }
}