﻿using Prism.Events;

namespace ThorCyte.Infrastructure.Events
{
    /// <summary>
    /// Publish when Macro is finished or aborted.
    /// </summary>
    public class MacroFinishEvent : PubSubEvent<int>
    {
    }
}