﻿using Prism.Events;

namespace ThorCyte.GraphicModule.Events
{
    public class RegionUpdateEvent : PubSubEvent<RegionUpdateArgs> { }

    public class GraphUpdateEvent : PubSubEvent<GraphUpdateArgs>  { }
}
