using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace HotbarTimers
{
    class ConfigurationUI : IDisposable
    {
        private readonly Configuration Configuration;
        private ExcelSheet<ClassJob>? GameJobsList { get; init; }
        private ExcelSheet<Action>? GameActionsList { get; init; }
        private ExcelSheet<Status>? GameStatusList { get; init; }
        private PlayerCharacter? Player { get; init; }
        private int? SelectedJobIndex;

        private Action<Configuration> OnConfigSave { get; init; }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public ConfigurationUI(Configuration configuration, DataManager dataManager, 
            ClientState clientState, Action<Configuration> onConfigSave)
        {
            this.Configuration = configuration;
            GameJobsList = dataManager.GetExcelSheet<ClassJob>();
            GameActionsList = dataManager.GetExcelSheet<Action>();
            GameStatusList = dataManager.GetExcelSheet<Status>();
            Player = clientState.LocalPlayer;
            OnConfigSave = onConfigSave;
        }

        public void Dispose() {}
        public void Draw() => DrawSettingsWindow();

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible || GameJobsList == null || Player?.ClassJob?.GameData == null)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(600, 600), ImGuiCond.Appearing);
            if (ImGui.Begin("Hotbar Timers Settings", ref this.settingsVisible,
                ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.BeginTabBar("HotbarTimersTabBar"))
                {
                    if (ImGui.BeginTabItem("Job timers"))
                    {
                        string[] jobs = GameJobsList
                            .Where(job => job.ItemSoulCrystal.Row != 0 && !job.IsLimitedJob && job.DohDolJobIndex == -1)
                            .OrderBy(job => job.Abbreviation.RawString)
                            .Select(job => job.Abbreviation.RawString).ToArray();

                        string currentJob = Player.ClassJob.GameData.Abbreviation.RawString;
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
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (removeAllButtonWidth + 20));
                        if (ImGui.Button("Remove All", new Vector2(removeAllButtonWidth, 20)))
                        {
                            Configuration.TimerConfigs.RemoveAll(config => config.Job == selectedJob);
                            SaveConfig();
                        }
                        ImGui.PopStyleColor();

                        var tableFlags = ImGuiTableFlags.Borders;
                        if (ImGui.BeginTable("ConfigTable", 5, tableFlags))
                        {
                            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableSetupColumn("Skill name", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableSetupColumn("Only applied by YOU", ImGuiTableColumnFlags.WidthFixed);
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
                                bool selfOnly = timerConfig.SelfOnly;
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

                                //Self applied
                                ImGui.TableSetColumnIndex(columnIndex++);
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2f) - ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.X + 1);
                                if (ImGui.Checkbox($"###{row}selfOnly", ref selfOnly))
                                {
                                    timerConfig.SelfOnly = selfOnly;
                                    SaveConfig();
                                };

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
                            var config = new TimerConfig(jobs[selectedJobIndex], "", "", true, true);
                            Configuration.TimerConfigs.Add(config);
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Misc"))
                    {
                        //status timer
                        ImGui.Text("Status timer settings:");
                        var statusTimerFontSize = Configuration.StatusTimerFontSize;
                        if (ImGui.SliderInt("Font size: ###statusTimerFontSize", ref statusTimerFontSize, 6, 30))
                        {
                            Configuration.StatusTimerFontSize = statusTimerFontSize;
                            SaveConfig();
                        }

                        var statusTimerFontColor = Configuration.StatusTimerFontColor;
                        if (ImGui.ColorEdit4("Font color: ###statusTimerTextColorEdit", ref statusTimerFontColor))
                        {
                            Configuration.StatusTimerFontColor = statusTimerFontColor;
                            SaveConfig();
                        }

                        ImGui.Separator();

                        //stack count
                        ImGui.Text("Stack count settings:");
                        var stackCountFontSize = Configuration.StackCountFontSize;
                        if (ImGui.SliderInt("Font size: ###stackCountFontSize", ref stackCountFontSize, 6, 30))
                        {
                            Configuration.StackCountFontSize = stackCountFontSize;
                            SaveConfig();
                        }

                        var stackCountFontColor = Configuration.StackCountFontColor;
                        if (ImGui.ColorEdit4("Font color: ###stackCountColorEdit", ref stackCountFontColor))
                        {
                            Configuration.StackCountFontColor = stackCountFontColor;
                            SaveConfig();
                        }

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
            OnConfigSave(Configuration);
        }

        private List<TimerConfig> GetSameNameConfigs(string currentJob)
        {
            if (GameJobsList == null || GameStatusList == null) return new();

            string? parentJob = GameJobsList
                .First(job => job.Abbreviation.RawString == currentJob)
                .ClassJobParent.Value?.Abbreviation.RawString;

            List<string> actions = new();
            actions.AddRange(GetJobActions(currentJob));
            if (parentJob != null) actions.AddRange(GetJobActions(parentJob));
            actions.AddRange(GetRoleActions(currentJob));
            
            List<string> statuses = GameStatusList
                .Where(status => actions.Contains(status.Name.RawString))
                .Select(status => status.Name.RawString)
                .Distinct().ToList();

            return statuses.Select(status => new TimerConfig(currentJob, status, status, true, true))
                .OrderBy(config => config.Status).ToList();
        }

        private List<string> GetJobActions(string job)
        {
            if (GameActionsList == null) return new();

            return GameActionsList
                .Where(action => action.ClassJob.Value?.Abbreviation.RawString == job)
                .Select(action => action.Name.RawString).ToList();
        }

        private List<string> GetRoleActions(string job)
        {
            if (GameActionsList == null) return new();

            //access job abbreviation property by string
            PropertyInfo? prop = typeof(ClassJobCategory).GetProperty(job);
            return GameActionsList.Where(
                action => action.IsRoleAction && 
                (bool?)prop?.GetValue(action.ClassJobCategory.Value) == true
            )
            .Select(action => action.Name.RawString).ToList();
        }
    }
}
