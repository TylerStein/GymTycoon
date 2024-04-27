using GymTycoon.Code.Common;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code
{
    public class Stat
    {
        public string Name;
        public Queue<float> Data = [];
        public int DataMax = 1;

        public Stat(string name)
        {
            Name = name;
        }

        public void AddDatum(int value)
        {
            if (Data.Count > Stats.MaxStatLength)
            {
                Data.Dequeue();
            }

            Data.Enqueue(value);
            if (value > DataMax)
            {
                DataMax = value;
            }
        }

        public void DrawImGui()
        {
            if (Data.Count > 0)
            {
                float[] arr = Data.ToArray();
                ImGui.PlotLines(Name, ref arr[0], arr.Length, 0, null, 0, DataMax, new System.Numerics.Vector2(0, 100));
            }
        }
    }

    public class Stats
    {
        public const int StatMinuteFrequency = 10;
        public const int StatDaysHistory = 2;
        public const int MaxStatLength = 24 * 60 / StatMinuteFrequency * StatDaysHistory; // days

        Stat ActiveGuests = new Stat("Guests");
        Stat Rating = new Stat("Rating");
        Stat Money = new Stat("Money");

        public void Update()
        {
            if (GameInstance.Instance.Time.DidChangeMinute)
            {
                int minute = GameInstance.Instance.Time.GetMinute();
                if (minute % 10 == 0)
                {
                    ActiveGuests.AddDatum(GameInstance.Instance.Director.ActiveGuests.Count());
                    Rating.AddDatum(GameInstance.Instance.Economy.GymRating);
                    Money.AddDatum(GameInstance.Instance.Economy.PlayerMoney);
                }
            }
        }

        public void DrawImGui()
        {
            ImGui.Begin("Stats");
            ActiveGuests.DrawImGui();
            ImGui.Separator();
            Rating.DrawImGui();
            ImGui.Separator();
            Money.DrawImGui();
            ImGui.Separator();
            if (ImGui.CollapsingHeader("Tags"))
            {
                ImGui.Text($"({Tag.CountAllTags()})");
                foreach (string tag in Tag.All())
                {
                    ImGui.Text(tag);
                }
            }
            ImGui.End();
        }
    }
}
