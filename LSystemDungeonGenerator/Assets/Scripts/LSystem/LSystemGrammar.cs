using System.Collections.Generic;

namespace Didionysymus.DungeonGeneration.LSystem
{
    public class LSystemGrammar
    {
        private Dictionary<char, List<string>> _rules;
        private System.Random _random;
        private DungeonConfig _config;
    }
}