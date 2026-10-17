using System;
using System.Drawing;
using System.Windows.Forms;

namespace MatrixSCR
{
    internal sealed class ConfigForm : Form
    {
        private readonly SaverSettings settings;
        private readonly TrackBar speed;
        private readonly TrackBar density;
        private readonly TrackBar fps;
        private readonly Label speedValue;
        private readonly Label densityValue;
        private readonly Label fpsValue;
        private readonly CheckBox fog;
        private readonly CheckBox waves;
        private readonly CheckBox rotate;
        private readonly ComboBox mode;

        public ConfigForm()
        {
            settings = SaverSettings.Load();
            Text = "GLMatrix para Windows";
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 405);

            Label title = new Label();
            title.Text = "GLMatrix";
            title.Font = new Font("Segoe UI Semibold", 18F);
            title.ForeColor = Color.FromArgb(0, 145, 45);
            title.SetBounds(24, 18, 210, 40);
            Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "Port OpenGL para Windows 11 del salvapantallas de XScreenSaver";
            subtitle.ForeColor = SystemColors.GrayText;
            subtitle.SetBounds(26, 55, 405, 24);
            Controls.Add(subtitle);

            speed = MakeTrackBar(25, 300, (int)Math.Round(settings.Speed * 100), 10);
            speedValue = MakeValueLabel();
            AddTrackRow("Velocidad", speed, speedValue, 90);

            density = MakeTrackBar(1, 100, settings.Density, 5);
            densityValue = MakeValueLabel();
            AddTrackRow("Densidad", density, densityValue, 145);

            fps = MakeTrackBar(15, 60, settings.Fps, 5);
            fpsValue = MakeValueLabel();
            AddTrackRow("Fotogramas/s", fps, fpsValue, 200);

            Label modeLabel = new Label();
            modeLabel.Text = "Alfabeto";
            modeLabel.SetBounds(25, 258, 105, 24);
            Controls.Add(modeLabel);
            mode = new ComboBox();
            mode.DropDownStyle = ComboBoxStyle.DropDownList;
            mode.Items.AddRange(new object[] { "Matrix", "Binary", "Hexadecimal", "Decimal", "DNA" });
            mode.SelectedItem = settings.Mode;
            mode.SetBounds(135, 255, 180, 26);
            Controls.Add(mode);

            fog = MakeCheckBox("Niebla de profundidad", settings.Fog, 26, 300);
            waves = MakeCheckBox("Ondas de brillo", settings.Waves, 230, 300);
            rotate = MakeCheckBox("Movimiento de cámara 3D", settings.Rotate, 26, 329);

            Button cancel = new Button();
            cancel.Text = "Cancelar";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(258, 365, 88, 28);
            Controls.Add(cancel);

            Button accept = new Button();
            accept.Text = "Aceptar";
            accept.SetBounds(352, 365, 88, 28);
            accept.Click += SaveAndClose;
            Controls.Add(accept);
            AcceptButton = accept;
            CancelButton = cancel;

            speed.ValueChanged += UpdateLabels;
            density.ValueChanged += UpdateLabels;
            fps.ValueChanged += UpdateLabels;
            UpdateLabels(null, EventArgs.Empty);
        }

        private void AddTrackRow(string text, TrackBar bar, Label value, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(25, y + 8, 105, 24);
            Controls.Add(label);
            bar.SetBounds(125, y, 250, 45);
            value.SetBounds(380, y + 8, 60, 24);
            Controls.Add(bar);
            Controls.Add(value);
        }

        private static TrackBar MakeTrackBar(int min, int max, int value, int change)
        {
            TrackBar bar = new TrackBar();
            bar.Minimum = min;
            bar.Maximum = max;
            bar.Value = Math.Max(min, Math.Min(max, value));
            bar.TickFrequency = change;
            bar.SmallChange = 1;
            bar.LargeChange = change;
            return bar;
        }

        private static Label MakeValueLabel()
        {
            Label label = new Label();
            label.TextAlign = ContentAlignment.MiddleRight;
            return label;
        }

        private CheckBox MakeCheckBox(string text, bool value, int x, int y)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Checked = value;
            box.AutoSize = true;
            box.Location = new Point(x, y);
            Controls.Add(box);
            return box;
        }

        private void UpdateLabels(object sender, EventArgs e)
        {
            speedValue.Text = (speed.Value / 100.0).ToString("0.00") + "×";
            densityValue.Text = density.Value + "%";
            fpsValue.Text = fps.Value.ToString();
        }

        private void SaveAndClose(object sender, EventArgs e)
        {
            settings.Speed = speed.Value / 100.0;
            settings.Density = density.Value;
            settings.Fps = fps.Value;
            settings.Fog = fog.Checked;
            settings.Waves = waves.Checked;
            settings.Rotate = rotate.Checked;
            settings.Mode = Convert.ToString(mode.SelectedItem);
            settings.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
