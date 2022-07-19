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

        public void Dispose() {}
        public void Draw() => DrawSettingsWindow();

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible || HotbarTimers.GameJobsList == null || HotbarTimers.Player?.ClassJob?.GameData == null)
            {
                return;
            }

            if (ImGui.Begin("Hotbar Timers Settings", ref this.settingsVisible,
                ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.BeginTabBar("HotbarTimersTabBar"))
                {
                    if (ImGui.BeginTabItem("Job timers"))
                    {
                        string[] jobs = HotbarTimers.GameJobsList
                            .Where(job => job.ItemSoulCrystal.Row != 0 && !job.IsLimitedJob && job.DohDolJobIndex == -1)
                            .OrderBy(job => job.Abbreviation.RawString)
                            .Select(job => job.Abbreviation.RawString).ToArray();

                        string currentJob = HotbarTimers.Player.ClassJob.GameData.Abbreviation.RawString;
                        int currentJobIndex = Math.Max(0, Array.FindIndex(jobs, job => job == currentJob));

                        int selectedJobIndex = SelectedJobIndex ?? currentJobIndex;
                        string selectedJob = jobs[selectedJobIndex];

                        if (ImGui.Combo("###jobSelector", ref selectedJobIndex, jobs, jobs.Length))
                        {
                            SelectedJobIndex = selectedJobIndex;
                        };
                        ImGui.Separator();
                        
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
                                string status = timerConfig.Status;
                                string skill = timerConfig.Skill;
                                bool enabled = timerConfig.Enabled;
                                ImGui.TableNextRow();

                                //Status
                                int columnIndex = 0;
                                ImGui.TableSetColumnIndex(columnIndex++);
                                ImGui.SetNextItemWidth(-1);
                                if (ImGui.InputTextWithHint($"###{row}statusNameInput", "Status name...", ref status, 100))
                                {
                                    timerConfig.Status = status.Trim();
                                    SaveConfig();
                                }

                                //Skill name
                                ImGui.TableSetColumnIndex(columnIndex++);
                                ImGui.SetNextItemWidth(-1);
                                if (ImGui.InputTextWithHint($"###{row}skillNameInput", "Skill name...", ref skill, 100))
                                {
                                    timerConfig.Skill = skill.Trim();
                                    SaveConfig();
                                }

                                //Enabled
                                ImGui.TableSetColumnIndex(columnIndex++);
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2f) - ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.X + 1);
                                if (ImGui.Checkbox($"###{row}enabled", ref enabled))
                                {
                                    timerConfig.Enabled = enabled;
                                    SaveConfig();
                                };

                                //Delete
                                ImGui.TableSetColumnIndex(columnIndex++);
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255, 0, 0, 255));
                                if (ImGui.Button($"X###{row}delete", new Vector2(-1, 20)))
                                {
                                    Configuration.TimerConfigs.Remove(timerConfig);
                                    SaveConfig();
                                };
                                ImGui.PopStyleColor(1);
                            }

                            ImGui.EndTable();
                        }

                        if (ImGui.Button("Add new row"))
                        {
                            var config = new TimerConfig(jobs[selectedJobIndex], "", "", true);
                            Configuration.TimerConfigs.Add(config);
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Misc"))
                    {
                        //status timer
                        ImGui.Text("Status timer settings:");
                        var statusTimerFontSize = Configuration.StatusTimerTextConfig.FontSize;
                        if (ImGui.SliderInt("Font size###statusTimerFontSize", ref statusTimerFontSize, 6, 30))
                        {
                            Configuration.StatusTimerTextConfig.FontSize = statusTimerFontSize;
                            SaveConfig();
                        }

                        var statusTimerFontColor = Configuration.StatusTimerTextConfig.FontColor;
                        if (ImGui.ColorEdit4("Font color###statusTimerTextColorEdit", ref statusTimerFontColor))
                        {
                            Configuration.StatusTimerTextConfig.FontColor = statusTimerFontColor;
                            SaveConfig();
                        }

                        var fontTypes = Enum.GetValues<FontType>().Select(f => f.ToString()).ToArray();
                        var statusTimerFontType = (int)Configuration.StatusTimerTextConfig.FontType;
                        if (ImGui.Combo("Font type###statusTimerFontType", ref statusTimerFontType, fontTypes, fontTypes.Length))
                        {
                            Configuration.StatusTimerTextConfig.FontType = (FontType)statusTimerFontType;
                            SaveConfig();
                        };

                        ImGui.Separator();

                        //stack count
                        ImGui.Text("Stack count settings:");
                        var stackCountFontSize = Configuration.StackCountTextConfig.FontSize;
                        if (ImGui.SliderInt("Font size###stackCountFontSize", ref stackCountFontSize, 6, 30))
                        {
                            Configuration.StackCountTextConfig.FontSize = stackCountFontSize;
                            SaveConfig();
                        }

                        var stackCountFontColor = Configuration.StackCountTextConfig.FontColor;
                        if (ImGui.ColorEdit4("Font color###stackCountColorEdit", ref stackCountFontColor))
                        {
                            Configuration.StackCountTextConfig.FontColor = stackCountFontColor;
                            SaveConfig();
                        }

                        var stackCountFontType = (int)Configuration.StackCountTextConfig.FontType;
                        if (ImGui.Combo("Font type###stackCountFontType", ref stackCountFontType, fontTypes, fontTypes.Length))
                        {
                            Configuration.StackCountTextConfig.FontType = (FontType)stackCountFontType;
                            SaveConfig();
                        };

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
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

            string? parentJob = HotbarTimers.GameJobsList
                .First(job => job.Abbreviation.RawString == currentJob)
                .ClassJobParent.Value?.Abbreviation.RawString;

            List<string> actions = new();
            actions.AddRange(GetJobActions(currentJob));
            if (parentJob != null) actions.AddRange(GetJobActions(parentJob));
            actions.AddRange(GetRoleActions(currentJob));
            
            List<string> statuses = HotbarTimers.GameStatusList
                .Where(status => actions.Contains(status.Name.RawString))
                .Select(status => status.Name.RawString)
                .Distinct().ToList();

            return statuses.Select(status => new TimerConfig(currentJob, status, status, true))
                .OrderBy(config => config.Status).ToList();
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
