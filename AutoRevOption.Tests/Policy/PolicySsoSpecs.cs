using System;
using System.IO;
using System.Linq;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace AutoRevOption.Tests.Policy
{
    public class PolicySsoSpecs
    {
        private static YamlMappingNode LoadRoot(string path)
        {
            Assert.True(File.Exists(path), $"OptionsRadar.yaml not found at: {path}");
            using var sr = new StreamReader(path);
            var yaml = new YamlStream();
            yaml.Load(sr);
            return (YamlMappingNode)yaml.Documents[0].RootNode;
        }

        private static YamlMappingNode Map(YamlMappingNode parent, string key)
        {
            Assert.True(parent.Children.ContainsKey(key), $"Missing top-level key: {key}");
            return (YamlMappingNode)parent.Children[key];
        }

        private static string? Scalar(YamlMappingNode parent, string key, bool required = true)
        {
            if (!parent.Children.ContainsKey(key))
            {
                if (required) Assert.True(false, $"Missing key: {key}");
                return null;
            }
            return ((YamlScalarNode)parent.Children[key]).Value;
        }

        [Fact]
        public void options_radar_has_policy_version_and_hash_and_run_mode()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "../../../..", "OptionsRadar.yaml");
            var root = LoadRoot(path);

            var policy = Map(root, "policy");
            var version = Scalar(policy, "version");
            var hash = Scalar(policy, "hash");
            Assert.False(string.IsNullOrWhiteSpace(version), "policy.version must be set");
            Assert.False(string.IsNullOrWhiteSpace(hash), "policy.hash must be set");

            var run = Map(root, "run");
            var mode = Scalar(run, "mode");
            Assert.Contains(mode, new[] { "DRY_RUN", "PAPER", "LIVE_LOCKED" });
        }

        [Fact]
        public void options_radar_has_minimum_required_selection_gates_and_caps()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "../../../..", "OptionsRadar.yaml");
            var root = LoadRoot(path);

            // gates may live either under selection_gates or at root in your current repo.
            // Support both to be non-breaking.
            YamlMappingNode gates;
            if (root.Children.ContainsKey("selection_gates"))
                gates = Map(root, "selection_gates");
            else
                gates = root; // tolerate flat structure for now

            // ensure keys exist
            foreach (var key in new[] {
                "min_pop",
                "min_reward_per_day_pct",
                "min_ivr",
                "min_oi",
                "max_spread_bidask_abs",
                "max_spread_bidask_pct_of_credit"
            })
            {
                Assert.True(gates.Children.ContainsKey(key), $"Missing selection gate: {key}");
            }

            // caps section
            YamlMappingNode caps;
            if (root.Children.ContainsKey("caps"))
                caps = Map(root, "caps");
            else
                caps = root; // tolerate flat structure

            foreach (var key in new[] {
                "per_trade_max_loss_usd",
                "portfolio_defined_risk_pct"
            })
            {
                Assert.True(caps.Children.ContainsKey(key), $"Missing risk cap: {key}");
            }
        }
    }
}
