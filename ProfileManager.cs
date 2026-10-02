using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace LSLMarkerSender
{
    /// <summary>
    /// A saved marker profile containing stream settings and marker definitions.
    /// Profiles are serialized as JSON files for persistence.
    /// </summary>
    public class MarkerProfile
    {
        public string StreamName { get; set; } = "MarkerStream";
        public string StreamType { get; set; } = "Markers";
        public string SourceId { get; set; } = "LSLMarkerSender1";
        public List<MarkerDefinition> Markers { get; set; } = new List<MarkerDefinition>();
    }

    /// <summary>
    /// Manages saving and loading marker profiles to/from JSON files.
    /// Profiles are stored in the application directory under "profiles/".
    /// </summary>
    public static class ProfileManager
    {
        private static readonly string ProfileDirectory;

        static ProfileManager()
        {
            ProfileDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles");
            if (!Directory.Exists(ProfileDirectory))
                Directory.CreateDirectory(ProfileDirectory);
        }

        /// <summary>
        /// Returns the full path of a profile file by name.
        /// </summary>
        public static string GetProfilePath(string profileName)
        {
            return Path.Combine(ProfileDirectory, profileName + ".json");
        }

        /// <summary>
        /// Save a profile to disk as a JSON file.
        /// </summary>
        public static void SaveProfile(string profileName, MarkerProfile profile)
        {
            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(GetProfilePath(profileName), json);
        }

        /// <summary>
        /// Load a profile from disk. Returns null if file not found.
        /// </summary>
        public static MarkerProfile LoadProfile(string profileName)
        {
            string path = GetProfilePath(profileName);
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<MarkerProfile>(json);
        }

        /// <summary>
        /// Returns a list of all saved profile names (without extension).
        /// </summary>
        public static List<string> GetProfileNames()
        {
            var names = new List<string>();
            if (Directory.Exists(ProfileDirectory))
            {
                foreach (var file in Directory.GetFiles(ProfileDirectory, "*.json"))
                {
                    names.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            return names;
        }

        /// <summary>
        /// Delete a profile file by name.
        /// </summary>
        public static void DeleteProfile(string profileName)
        {
            string path = GetProfilePath(profileName);
            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>
        /// Create a default profile with common marker examples.
        /// </summary>
        public static MarkerProfile CreateDefaultProfile()
        {
            return new MarkerProfile
            {
                StreamName = "MarkerStream",
                StreamType = "Markers",
                SourceId = "LSLMarkerSender1",
                Markers = new List<MarkerDefinition>
                {
                    new MarkerDefinition { MarkerValue = "stimulus_start", Shortcut = System.Windows.Forms.Keys.F1, ButtonColor = "#27AE60", ShowAsButton = true },
                    new MarkerDefinition { MarkerValue = "stimulus_end", Shortcut = System.Windows.Forms.Keys.F2, ButtonColor = "#E74C3C", ShowAsButton = true },
                    new MarkerDefinition { MarkerValue = "response", Shortcut = System.Windows.Forms.Keys.Space, ButtonColor = "#3498DB", ShowAsButton = true },
                    new MarkerDefinition { MarkerValue = "block_start", Shortcut = System.Windows.Forms.Keys.F5, ButtonColor = "#F39C12", ShowAsButton = true },
                    new MarkerDefinition { MarkerValue = "block_end", Shortcut = System.Windows.Forms.Keys.F6, ButtonColor = "#9B59B6", ShowAsButton = true },
                }
            };
        }
    }
}
