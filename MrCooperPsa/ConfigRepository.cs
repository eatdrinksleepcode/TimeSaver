using System;
using System.IO;
using Newtonsoft.Json;

namespace MrCooperPsa {
    public class ConfigRepository {
        public Config LoadConfig() {
            var configFile = GetConfigFilePath();
            if (File.Exists(configFile)) {
                var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFile));
                Console.WriteLine($"Config file loaded from {GetConfigFilePath()}");
                return config;
            }

            return default(Config);
        }

        public void SaveConfig(Config config) {
            var configFilePath = GetConfigFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config));
            Console.WriteLine($"Config file saved to {configFilePath}");
        }

        private static string GetConfigFilePath() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimeSaver",
                "config");
        }
    }
}