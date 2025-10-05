using System;
using System.IO;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace AutoRevOption.Tests.Selection
{
    public class SelectionGateSpecs
    {
        private static YamlMappingNode LoadRoot()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "../../../..", "OptionsRadar.yaml");
            Assert.True(File.Exists(path), $"OptionsRadar.yaml not found at: {path}");
            using var sr = new StreamReader(path);
            var yaml = new YamlStream();
            yaml.Load(sr);
            return (YamlMappingNode)yaml.Documents[0].RootNode;
        }

        private static decimal GetDecimal(YamlMappingNode map, string key, decimal? defaultValue = null)
        {
            if (!map.Children.ContainsKey(key))
            {
                if (defaultValue.HasValue) return defaultValue.Value;
                throw new Xunit.Sdk.XunitException($"Missing key: {key}");
            }
            var s = ((YamlScalarNode)map.Children[key]).Value ?? "0";
            return decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }

        [Fact]
        public void min_pop_and_reward_per_day_are_guarded()
        {
            var root = LoadRoot();
            var gates = root.Children.ContainsKey("selection_gates")
                ? (YamlMappingNode)root.Children["selection_gates"]
                : root; // tolerate flat structure

            var minPop = GetDecimal(gates, "min_pop");
            var minRpd = GetDecimal(gates, "min_reward_per_day_pct");

            Assert.True(minPop >= 0.75m, $"min_pop must be >= 0.75 (got {minPop})");
            Assert.True(minRpd >= 2.5m, $"min_reward_per_day_pct must be >= 2.5 (got {minRpd})");
        }

        [Fact]
        public void liquidity_and_spread_caps_are_sane()
        {
            var root = LoadRoot();
            var gates = root.Children.ContainsKey("selection_gates")
                ? (YamlMappingNode)root.Children["selection_gates"]
                : root;

            var minOi = GetDecimal(gates, "min_oi");
            var maxAbs = GetDecimal(gates, "max_spread_bidask_abs");
            var maxPct = GetDecimal(gates, "max_spread_bidask_pct_of_credit");

            Assert.True(minOi >= 100m, $"min_oi should be at least 100 (got {minOi})");
            Assert.True(maxAbs <= 0.10m, $"max_spread_bidask_abs should be <= 0.10 (got {maxAbs})");
            Assert.True(maxPct <= 0.20m, $"max_spread_bidask_pct_of_credit should be <= 0.20 (got {maxPct})");
        }
    }
}
