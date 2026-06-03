using Flipit.Core;

namespace Flipit.Settings;

/// <summary>
/// Small modal dialog that captures a single hotkey combination from the user.
/// Shows real-time feedback as the user holds modifier keys and presses a main key.
/// </summary>
public sealed class HotkeyCaptureDialog : Form
{
    private Label _instructionLabel = null!;
    private Label _previewLabel     = null!;
    private Label _errorLabel       = null!;
    private Button _cancelButton    = null!;

    private Keys _currentModifiers = Keys.None;

    /// <summary>
    /// The captured hotkey. Only valid when <see cref="DialogResult"/> is <see cref="DialogResult.OK"/>.
    /// </summary>
    public HotkeyDefinition? CapturedHotkey { get; private set; }

    public HotkeyCaptureDialog()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        Text            = "Change Hotkey";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new Size(340, 170);
        ShowInTaskbar   = false;
        KeyPreview      = true;  // Receive key events before child controls

        _instructionLabel = new Label
        {
            Text      = "Press the key combination you want to use as your hotkey.\n"
                      + "Supported: F1–F12, Insert, Pause, Scroll Lock, Home, End, Page Up, Page Down\n"
                      + "(Optionally hold Ctrl, Alt, or Shift)",
            Location  = new Point(12, 12),
            Size      = new Size(316, 56),
            ForeColor = SystemColors.GrayText,
        };

        _previewLabel = new Label
        {
            Text      = "Waiting for key press…",
            Location  = new Point(12, 78),
            Size      = new Size(316, 28),
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.ControlLight,
        };

        _errorLabel = new Label
        {
            Text      = string.Empty,
            Location  = new Point(12, 112),
            Size      = new Size(316, 18),
            ForeColor = Color.Firebrick,
            Font      = new Font("Segoe UI", 8.5f),
        };

        _cancelButton = new Button
        {
            Text          = "Cancel",
            DialogResult  = DialogResult.Cancel,
            Location      = new Point(246, 134),
            Size          = new Size(80, 26),
        };

        Controls.AddRange(new Control[]
            { _instructionLabel, _previewLabel, _errorLabel, _cancelButton });
        CancelButton = _cancelButton;
    }

    // ── Keyboard handling ────────────────────────────────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled    = true;
        e.SuppressKeyPress = true;
        _errorLabel.Text = string.Empty;

        // Track modifier state
        if (HotkeyValidator.IsModifierOnly(e.KeyCode))
        {
            _currentModifiers = GetCurrentModifiers(e);
            UpdatePreview(_currentModifiers, null);
            return;
        }

        // Combine modifiers from the event (most reliable)
        var modifiers = GetCurrentModifiers(e);
        UpdatePreview(modifiers, e.KeyCode);

        if (HotkeyValidator.TryBuild(e.KeyCode, modifiers, out var definition, out string error))
        {
            CapturedHotkey = definition;
            DialogResult   = DialogResult.OK;
            Close();
        }
        else
        {
            _errorLabel.Text = error;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        e.Handled = true;
        if (HotkeyValidator.IsModifierOnly(e.KeyCode))
        {
            _currentModifiers = GetCurrentModifiers(e);
            // If user released all keys without pressing a main key, reset preview
            if (_currentModifiers == Keys.None)
                _previewLabel.Text = "Waiting for key press…";
            else
                UpdatePreview(_currentModifiers, null);
        }
        base.OnKeyUp(e);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Keys GetCurrentModifiers(KeyEventArgs e)
    {
        var mod = Keys.None;
        if (e.Control) mod |= Keys.Control;
        if (e.Alt)     mod |= Keys.Alt;
        if (e.Shift)   mod |= Keys.Shift;
        return mod;
    }

    private void UpdatePreview(Keys modifiers, Keys? mainKey)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(Keys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(Keys.Alt))     parts.Add("Alt");
        if (modifiers.HasFlag(Keys.Shift))   parts.Add("Shift");
        if (mainKey.HasValue)                parts.Add(mainKey.Value.ToString());

        _previewLabel.Text = parts.Count > 0
            ? string.Join(" + ", parts)
            : "Waiting for key press…";
    }
}

