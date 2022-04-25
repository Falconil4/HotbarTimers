using Dalamud.Data;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using TimersOnSkills.Models;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace TimersOnSkills
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class ConfigurationUI : IDisposable
    {
        private Configuration configuration;
        private ExcelSheet<ClassJob>? GameJobsList { get; init; }
        private PlayerCharacter? Player { get; init; }
        private int? SelectedJobIndex;

        private Action<Configuration> OnConfigSave { get; init; }

        // this extra bool exists for ImGui, since you can't ref a property
        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public ConfigurationUI(Configuration configuration, DataManager dataManager, 
            ClientState clientState, Action<Configuration> onConfigSave)
        {
            this.configuration = configuration;
            GameJobsList = dataManager.GetExcelSheet<ClassJob>();
            Player = clientState.LocalPlayer;
            OnConfigSave = onConfigSave;
        }

        public void Dispose() {}

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawSettingsWindow();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible || GameJobsList == null || Player?.ClassJob?.GameData == null)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.Appearing);
            if (ImGui.Begin("Timers on Skills Settings", ref this.settingsVisible,
                ImGuiWindowFlags.NoCollapse))
            {
                ImGui.Separator();

                string[] jobs = GameJobsList
                    .Where(job => job.ItemSoulCrystal.Row != 0 && !job.IsLimitedJob && job.CanQueueForDuty)
                    .OrderBy(job => job.Abbreviation.RawString)
                    .Select(job => job.Abbreviation.RawString).ToArray();
                
                string currentJob = Player.ClassJob.GameData.Abbreviation.RawString;
                int currentJobIndex = Math.Max(0, Array.FindIndex(jobs, job => job == currentJob));

                int selectedJobIndex = SelectedJobIndex ?? currentJobIndex;
                
                if (ImGui.Combo("###jobSelector", ref selectedJobIndex, jobs, jobs.Length)) 
                {
                    SelectedJobIndex = selectedJobIndex;
                }; 
                ImGui.Separator();

                var tableFlags = ImGuiTableFlags.Borders;
                if (ImGui.BeginTable("ConfigTable", 4, tableFlags))
                {
                    ImGui.TableSetupColumn("Status",  ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Skill", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableHeadersRow();

                    List<TimerConfig> applicableTimers = configuration.TimerConfigs
                        .Where(timer => timer.Job == jobs[selectedJobIndex]).ToList();

                    for (int row = 0; row < applicableTimers.Count; row++)
                    {
                        TimerConfig timerConfig = applicableTimers[row];
                        string status = timerConfig.Status;
                        string skill = timerConfig.Skill;
                        bool enabled = timerConfig.Enabled;
                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);                        
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputTextWithHint($"###{row}statusNameInput", "Status name...", ref status, 100))
                        {
                            timerConfig.Status = status.Trim();
                            SaveConfig();
                        }

                        ImGui.TableSetColumnIndex(1);
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputTextWithHint($"###{row}skillNameInput", "Skill name...", ref skill, 100))
                        {
                            timerConfig.Skill = skill.Trim();
                            SaveConfig();
                        }

                        ImGui.TableSetColumnIndex(2);
                        if (ImGui.Checkbox($"###{row}enabled", ref enabled))
                        {
                            timerConfig.Enabled = enabled;
                            SaveConfig();
                        };

                        ImGui.TableSetColumnIndex(3);
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255, 0, 0, 255));
                        if (ImGui.Button($"X###{row}delete"))
                        {
                            configuration.TimerConfigs.Remove(timerConfig);
                            SaveConfig();
                        };
                        ImGui.PopStyleColor(1);
                    }

                    ImGui.EndTable();
                }

                if (ImGui.Button("Add new row"))
                {
                    configuration.TimerConfigs.Add(new TimerConfig("", "", true, jobs[selectedJobIndex]));
                }
            }
            ImGui.End();
        }

        private void SaveConfig()
        {
            configuration.TimerConfigs.RemoveAll(timer => String.IsNullOrEmpty(timer.Skill) && String.IsNullOrEmpty(timer.Status));
            configuration.Save();
            OnConfigSave(configuration);
        }
    }
}
