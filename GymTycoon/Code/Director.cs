using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code
{
    public class SpawnTimeline
    {
        public int MaxSpawnPerTick = 2;

        public float NewGuestChance = 0.9f;
        public float ReturnGuestChance = 0.9f;

        public float SpawnThrottleDecay = 0.01f;
        public float SpawnCost = 0.1f;

        public bool IsAtStart => _cursor == 0;
        public bool HasTimeline => _timeline.Length > 0;

        private OffscreenGuest[][] _timeline;
        private int _cursor = 0;

        private float[] _cachedGraphData = [];
        private bool _graphIsDirty = true;

        public bool Next(List<OffscreenGuest> value)
        {
            Peek(value);
            _cursor++;
            if (_cursor >= _timeline.Length)
            {
                _cursor = 0;
                return false;
            }

            return true;
        }

        public void Peek(List<OffscreenGuest> value)
        {
            value.Clear();
            value.AddRange(_timeline[_cursor]);
        }

        public void GenerateDay(DateTime today, Director director, int seed = 0)
        {
            _graphIsDirty = true;
            int steps = 24 * 60 - 1;
            DateTime yesterday = today.AddDays(-1);
            float throttle = 0f;
            Random random = seed == 0 ? Random.Shared : new Random(seed);
            _timeline = new OffscreenGuest[steps][];
            _cursor = 0;
            List<OffscreenGuest> events = [];
            for (int i = 0; i < steps; i++)
            {
                throttle -= SpawnThrottleDecay;
                if (throttle < 0f)
                {
                    throttle = 0f;
                }

                events.Clear();
                int hour = (int)MathF.Floor(steps / 60);
                float timeOfDayPopularity = Director.TimeOfDayPopularity[hour];
                float rng = random.NextSingle();

                if (rng < timeOfDayPopularity)
                {
                    foreach (var guest in director.OffscreenGuests)
                    {
                        if (guest.LastVisit <= yesterday && guest.Schedule.TimeInWindow(GameInstance.Instance.Time.GetHour()))
                        {
                            if ((Random.Shared.NextSingle() + throttle) < ReturnGuestChance)
                            {
                                events.Add(guest);
                            }
                        }
                    }


                    if ((Random.Shared.NextSingle() + throttle) < NewGuestChance)
                    {
                        throttle += SpawnCost;
                        events.Add(director.CreateOffscreenGuest());
                    }
                }

                _timeline[i] = events.ToArray();
            }
        }

        public void DrawImGui()
        {
            float max = 0;
            if (_graphIsDirty)
            {
                _cachedGraphData = new float[_timeline.Length / 30];
                for (int i = 0; i < _timeline.Length / 30; i++)
                {
                    int sum = 0;
                    for (int j = 0; j < 30; j++)
                    {
                        sum += _timeline[(i * 30) + j].Length;
                    }

                    _cachedGraphData[i] = sum;
                    if (sum > max)
                    {
                        max = sum;
                    }
                }
            }

            ImGui.PlotLines("Timeline", ref _cachedGraphData[0], _cachedGraphData.Length, 0, null, 0, max, new System.Numerics.Vector2(0, 100));
        }
    }

    public class Director
    {
        public static readonly float[] TimeOfDayPopularity = [
            0.020f, // 12am
            0.015f, // 01am
            0.015f, // 02am
            0.020f, // 03am
            0.040f, // 04am
            0.050f, // 05am
            0.060f, // 06am
            0.065f, // 07am
            0.080f, // 08am
            0.100f, // 09am
            0.120f, // 10am
            0.140f, // 11am
            0.160f, // 12pm
            0.130f, // 01pm
            0.100f, // 02pm
            0.120f, // 03pm
            0.140f, // 04pm
            0.150f, // 05pm
            0.160f, // 06pm
            0.085f, // 07pm
            0.080f, // 08pm
            0.075f, // 09pm
            0.060f, // 10pm
            0.025f, // 11pm
        ];


        public List<OffscreenGuest> OffscreenGuests = [];

        public List<Guest> ActiveGuests = [];

        public SpawnTimeline Timeline;

        public int MinGuestMoney = 0;
        public int MaxguestMoney = 0;

        private bool _autoSpawnEnabled = false;

        private int _maxActiveGuests = 1000;

        public void Initialize()
        {
            Timeline = new SpawnTimeline();
            Timeline.GenerateDay(GameInstance.Instance.Time.GetDateTime(), this);
        }

        public void Update(float deltaTime, bool tick)
        {

            for (int i = ActiveGuests.Count - 1; i >= 0; i--)
            {
                if (ActiveGuests[i].PendingRemoval)
                {
                    RemoveGuest(ActiveGuests[i]);
                    continue;
                }

                ActiveGuests[i].Update(deltaTime);
                if (tick)
                {
                    ActiveGuests[i].Tick();
                }
            }

            if (ActiveGuests.Count > _maxActiveGuests || !_autoSpawnEnabled || deltaTime == 0f)
            {
                return;
            }

            List<OffscreenGuest> toSpawn = [];
            if (GameInstance.Instance.Time.DidChangeMinute)
            {
                if (GameInstance.Instance.Time.DidChangeDay)
                {
                    Timeline.GenerateDay(GameInstance.Instance.Time.GetDateTime(), this);
                }

                if (Timeline.Next(toSpawn))
                {
                    foreach (var offscreen in toSpawn)
                    {
                        if (ActiveGuests.Count < _maxActiveGuests)
                        {
                            SpawnOffscreenGuest(offscreen);
                        }
                    }
                }
            }
        }

        public OffscreenGuest CreateOffscreenGuest()
        {
            OffscreenGuest offscreenGuest = new OffscreenGuest([
                DefaultTraits.GetRandomScheduleTraitForTime(GameInstance.Instance.Time.GetHour()),
                new TraitData()
                {
                    WealthTier = (WealthTier)Random.Shared.Next((int)WealthTier.Low, (int)WealthTier.Premium),
                }
            ]);
            return offscreenGuest;
        }

        public Guest SpawnNewGuest()
        {
            return SpawnOffscreenGuest(CreateOffscreenGuest());
        }

        public Guest SpawnOffscreenGuest(OffscreenGuest offscreenGuest)
        {
            List<int> spawnTiles = GameInstance.Instance.World.FindTilesWithProperties(TileProperties.Spawn, 4);
            if (spawnTiles.Count == 0)
            {
                return null;
            }

            OffscreenGuests.Remove(offscreenGuest);
            int spawnTileWorldIndex = spawnTiles[Random.Shared.Next(spawnTiles.Count)];
            Guest guest = new Guest(offscreenGuest, spawnTileWorldIndex, GameInstance.Instance.Instances.InstantiateSprite(new ScopedName("Sprite.Default.Guest")));
            ActiveGuests.Add(guest);
            guest.OffscreenGuest.LastVisit = GameInstance.Instance.Time.GetDateTime();
            guest.OffscreenGuest.LifetimeVisits++;

            GameInstance.Instance.Economy.Transaction(GameInstance.Instance.Economy.MembershipPrice, TransactionType.Membership);
            guest.UpdateHappiness(guest.OffscreenGuest.GetHappinessChangeFromWealthTierVsCost(GameInstance.Instance.Economy.MembershipPrice));

            return guest;
        }

        public void RemoveGuest(Guest guest)
        {
            ActiveGuests.Remove(guest);

            if (guest.OffscreenGuest.LifetimeHappiness <= OffscreenGuest.MinLifetimeHappiness)
            {
                return;
            }

            OffscreenGuests.Add(guest.OffscreenGuest);
        }

        public void DrawImGui()
        {
            ImGui.Begin("[DEBUG] Director");
            ImGui.Text($"In Gym: {ActiveGuests.Count()}");
            ImGui.Text($"Offscreen: {OffscreenGuests.Count()}");
            ImGui.Checkbox("AutoSpawn", ref _autoSpawnEnabled);
            ImGui.DragFloat("SpawnCost", ref Timeline.SpawnCost, 0.01f, 0f, 10f);
            ImGui.DragFloat("NewGuestChance", ref Timeline.NewGuestChance, 0.01f, 0f, 1f);
            ImGui.DragFloat("RetGuestChance", ref Timeline.ReturnGuestChance, 0.01f, 0f, 1f);
            ImGui.DragInt("MaxActiveGuests", ref _maxActiveGuests, 1, 0, 5000);

            if (ImGui.Button("Spawn New Guest"))
            {
                SpawnNewGuest();
            }

            if (OffscreenGuests.Count > 0 && ImGui.Button("Spawn Offscreen Guest"))
            {
                SpawnOffscreenGuest(OffscreenGuests[Random.Shared.Next(0, OffscreenGuests.Count)]);
            }

            if (ImGui.CollapsingHeader("Today's Timeline"))
            {
                Timeline.DrawImGui();
            }

            if (ImGui.CollapsingHeader("Guest List"))
            {
                GameInstance.Instance.Cursor.GetSelectedGuest(out Guest selectedGuest);
                for (int i = 0; i < ActiveGuests.Count; i++)
                {
                    if (ImGui.CollapsingHeader($"Guest [{i}]") || (selectedGuest != null && ActiveGuests[i] == selectedGuest))
                    {
                        ActiveGuests[i].DrawImGui();
                    }
                }
            }

            ImGui.End();
        }
    }
}
