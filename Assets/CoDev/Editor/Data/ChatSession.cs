using System;
using System.Collections.Generic;

namespace SuperHorizonStudios.CoDev
{
    [Serializable]
    public class ChatEntry
    {
        public string prompt;
        public string response;
        public DateTime timestamp;
    }

    [Serializable]
    public class ChatSession
    {
        public List<ChatEntry> entries = new List<ChatEntry>();
    }
}
