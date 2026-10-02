using System;
using System.Windows.Forms;

namespace LSLMarkerSender
{
    /// <summary>
    /// Represents a single marker definition that can be sent via LSL.
    /// Each marker has a name/value, an optional keyboard shortcut, and an optional color for the UI button.
    /// </summary>
    public class MarkerDefinition
    {
        /// <summary>
        /// The marker string value that will be sent over LSL when triggered.
        /// </summary>
        public string MarkerValue { get; set; } = "";

        /// <summary>
        /// The keyboard key assigned to this marker. Press this key to send the marker.
        /// Use Keys.None if no keyboard shortcut is assigned.
        /// </summary>
        public Keys Shortcut { get; set; } = Keys.None;

        /// <summary>
        /// The display color for the marker button in the UI (hex string, e.g. "#FF5733").
        /// </summary>
        public string ButtonColor { get; set; } = "#4A90D9";

        /// <summary>
        /// Whether this marker should be shown as a clickable button on the main form.
        /// </summary>
        public bool ShowAsButton { get; set; } = true;

        /// <summary>
        /// Returns a human-readable shortcut label (e.g., "F1", "A", "NumPad0").
        /// </summary>
        public string ShortcutDisplay
        {
            get
            {
                if (Shortcut == Keys.None) return "(yalnızca buton)";
                return Shortcut.ToString();
            }
        }

        public override string ToString()
        {
            return $"{MarkerValue} [{ShortcutDisplay}]";
        }
    }
}
