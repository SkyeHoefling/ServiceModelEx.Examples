using System;

namespace ServiceModelEx.Examples.WPF.ViewModelPubSub.Model
{
    public class SubscriptionResult
    {
        public int Id { get; set; }
        public string Payload { get; set; }
        public string Timestamp { get; set; }
    }
}
