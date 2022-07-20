using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace HotbarTimers
{
    class ConfigurationUI : IDisposable
    {
        private readonly Configuration Configuration;
        
        private int? SelectedJobIndex;

        private Action<Configuration> OnConfigSave { get; init; }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public ConfigurationUI(Configuration configuration, Action<Configuration> onConfigSave)
        {
            Configuration = configuration;
            OnConfigSave = onConfigSave;
        }

        private readonly List<string>? StatusChoices = HotbarTimers.GameStatusList?.Select(s => s.Name.RawString).Distinct().OrderBy(x => x).ToList();
        private List<string>? SkillChoices = null;

        public void Dispose() {}

        public void Draw()
        {
            if (!SettingsVisible || HotbarTimers.Player == null)
            {
                return;
            }

            if (ImGui.Begin("Hotbar Timers Settings", ref this.settingsVisible, ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.BeginTabBar("HotbarTimersTabBar"))
                {
                    DrawJobTimersTab();
                    DrawMiscTab();
                    ImGui.EndTabBar();
                }
                ImGui.End();
            }
        }

        private void DrawJobTimersTab()
        {
            if (ImGui.BeginTabItem("Job timers"))
            {
                string selectedJob = "";
                DrawJobCombo(ref selectedJob);
                ImGui.Separator();
                DrawTableManipulationButtons(selectedJob);
                DrawJobTimersTable(selectedJob);
                DrawAddNewRowButton(selectedJob);
                ImGui.EndTabItem();
            }
        }

        private void DrawJobTimersTable(string selectedJob)
        {
            var tableFlags = ImGuiTableFlags.Borders;
            if (ImGui.BeginTable("ConfigTable", 4, tableFlags))
            {
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Skill name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();

                List<TimerConfig> applicableTimers = Configuration.TimerConfigs
                    .Where(timer => timer.Job == selectedJob).ToList();

                for (int row = 0; row < applicableTimers.Count; row++)
                {
                    TimerConfig timerConfig = applicableTimers[row];

                    ImGui.TableNextRow();
                    DrawRowNamePicker("status", row, StatusChoices, timerConfig.Status, (name) => timerConfig.Status = name);
                    DrawRowNamePicker("skill", row, SkillChoices, timerConfig.Skill, (name) => timerConfig.Skill = name);
                    DrawRowEnabledCheckbox(row, ref timerConfig);
                    DrawRowDeleteButton(row, ref timerConfig);
                }

                ImGui.EndTable();
            }
        }

        private void DrawRowEnabledCheckbox(int row, ref TimerConfig timerConfig)
        {
            bool enabled = timerConfig.Enabled;

            ImGui.TableNextColumn();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2f) - ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.X + 1);
            if (ImGui.Checkbox($"##{row}enabled", ref enabled))
            {
                timerConfig.Enabled = enabled;
                SaveConfig();
            };
        }

        private void DrawRowDeleteButton(int row, ref TimerConfig timerConfig)
        {
            ImGui.TableNextColumn();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255, 0, 0, 255));
            if (ImGui.Button($"X##{row}delete", new Vector2(-1, 20)))
            {
                Configuration.TimerConfigs.Remove(timerConfig);
                SaveConfig();
            };
            ImGui.PopStyleColor(1);
        }

        private void DrawTableManipulationButtons(string selectedJob)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255, 255, 0, 255));
            if (ImGui.Button("Populate with all skills with the same status names"))
            {
                var sameNamesConfigs = GetSameNameConfigs(selectedJob);
                foreach (TimerConfig config in sameNamesConfigs)
                {
                    if (!Configuration.TimerConfigs.Contains(config))
                        Configuration.TimerConfigs.Add(config);
                }
                SaveConfig();
            }
            ImGui.PopStyleColor();

            var removeAllButtonWidth = 130;
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255, 0, 0, 255));
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (removeAllButtonWidth + 20));
            ImGui.SameLine();
            if (ImGui.Button("Remove All", new Vector2(removeAllButtonWidth, 20)))
            {
                Configuration.TimerConfigs.RemoveAll(config => config.Job == selectedJob);
                SaveConfig();
            }
            ImGui.PopStyleColor();
        }

        private void DrawJobCombo(ref string selectedJob)
        {
            string[] jobs = HotbarTimers.GameJobsList!
                .Where(job => job.ItemSoulCrystal.Row != 0 && !job.IsLimitedJob && job.DohDolJobIndex == -1)
                .OrderBy(job => job.Abbreviation.RawString)
                .Select(job => job.Abbreviation.RawString).ToArray();

            string currentJob = HotbarTimers.Player!.ClassJob.GameData!.Abbreviation.RawString;
            int currentJobIndex = Math.Max(0, Array.FindIndex(jobs, job => job == currentJob));

            int selectedJobIndex = SelectedJobIndex ?? currentJobIndex;
            selectedJob = jobs[selectedJobIndex];

            if (ImGui.Combo("###jobSelector", ref selectedJobIndex, jobs, jobs.Length))
            {
                SelectedJobIndex = selectedJobIndex;
                SkillChoices = GetAllJobActions(jobs[selectedJobIndex]);
            };

            if (SkillChoices == null) SkillChoices = GetAllJobActions(selectedJob);
        }

        private void DrawAddNewRowButton(string selectedJob)
        {
            if (ImGui.Button("Add new row"))
            {
                var config = new TimerConfig(selectedJob, "", "", true);
                Configuration.TimerConfigs.Add(config);
            }
        }

        private void DrawMiscTab()
        {
            if (ImGui.BeginTabItem("Misc"))
            {
                DrawFontSettings("Status timer settings:", "statusTimer", Configuration.StatusTimerTextConfig,
                    (TextConfig config) => {
                        Configuration.StatusTimerTextConfig = config;
                        SaveConfig();
                });
                ImGui.Separator();
                DrawFontSettings("Stack count settings:", "stackCount", Configuration.StackCountTextConfig,
                    (TextConfig config) => {
                        Configuration.StackCountTextConfig = config;
                        SaveConfig();
                });
                ImGui.EndTabItem();
            }
        }

        private void DrawFontSettings(string heading, string label, TextConfig currentConfig, Action<TextConfig> onTextConfigSave)
        {
            //status timer
            ImGui.Text(heading);

            var fontSize = currentConfig.FontSize;
            var fontColor = currentConfig.FontColor;
            var fontType = (int)currentConfig.FontType;

            if (ImGui.SliderInt($"Font size##{label}FontSize", ref fontSize, 6, 30))
            {
                onTextConfigSave(new((FontType)fontType, fontSize, fontColor));
            }

            if (ImGui.ColorEdit4($"Font color##{label}TextColorEdit", ref fontColor))
            {
                onTextConfigSave(new((FontType)fontType, fontSize, fontColor));
            }

            var fontTypes = Enum.GetValues<FontType>().Select(f => f.ToString()).ToArray();
            if (ImGui.Combo($"Font type##{label}FontType", ref fontType, fontTypes, fontTypes.Length))
            {
                onTextConfigSave(new((FontType)fontType, fontSize, fontColor));
            };
        }
        private static string SearchText = "";
        private void DrawRowNamePicker(string label, int row, List<string>? choices, string currentValue, Action<string> onSelected)
        {
            ImGui.TableNextColumn();
            ImGui.Text(currentValue);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - 20);
            if (ImGui.BeginCombo($"##{row}{label}Combo", string.Empty, ImGuiComboFlags.NoPreview))
            {
                ImGui.InputText($"Search##{label}", ref SearchText, 512);

                if (choices != null)
                {
                    foreach (string name in choices)
                    {
                        if (!String.IsNullOrEmpty(SearchText) && !name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) continue;
                        bool selected = name.Equals(currentValue, StringComparison.OrdinalIgnoreCase);
                        if (ImGui.Selectable($"{name}##{label}{row}", selected))
                        {
                            onSelected(name);
                            SaveConfig();
                        }
                    }
                }

                ImGui.EndCombo();
            }
        }

        private void SaveConfig()
        {
            Configuration.TimerConfigs.RemoveAll(timer => String.IsNullOrEmpty(timer.Skill) && String.IsNullOrEmpty(timer.Status));
            Configuration.Save();
            HotbarTimers.Configuration = Configuration;
            OnConfigSave(Configuration);
        }

        private static List<TimerConfig> GetSameNameConfigs(string currentJob)
        {
            if (HotbarTimers.GameJobsList == null || HotbarTimers.GameStatusList == null) return new();

            List<string> actions = GetAllJobActions(currentJob);
            
            List<string> statuses = HotbarTimers.GameStatusList
                .Where(status => actions.Contains(status.Name.RawString))
                .Select(status => status.Name.RawString)
                .Distinct().ToList();

            return statuses.Select(status => new TimerConfig(currentJob, status, status, true))
                .OrderBy(config => config.Status).ToList();
        }
        
        private static List<string> GetAllJobActions(string job)
        {
            string? parentJob = HotbarTimers.GameJobsList
                ?.First(j => j.Abbreviation.RawString == job)
                .ClassJobParent.Value?.Abbreviation.RawString;

            List<string> actions = new();
            actions.AddRange(GetJobActions(job));
            if (parentJob != null) actions.AddRange(GetJobActions(parentJob));
            actions.AddRange(GetRoleActions(job));
            actions.Add("Sprint");
            return actions.Distinct().OrderBy(name => name).ToList();
        }

        private static List<string> GetJobActions(string job)
        {
            if (HotbarTimers.GameActionsList == null) return new();

            return HotbarTimers.GameActionsList
                .Where(action => action.ClassJob.Value?.Abbreviation.RawString == job)
                .Select(action => action.Name.RawString).ToList();
        }

        private static List<string> GetRoleActions(string job)
        {
            if (HotbarTimers.GameActionsList == null) return new();

            //access job abbreviation property by string
            PropertyInfo? prop = typeof(ClassJobCategory).GetProperty(job);
            return HotbarTimers.GameActionsList.Where(
                action => action.IsRoleAction && 
                (bool?)prop?.GetValue(action.ClassJobCategory.Value) == true
            )
            .Select(action => action.Name.RawString).ToList();
        }
    }
}
