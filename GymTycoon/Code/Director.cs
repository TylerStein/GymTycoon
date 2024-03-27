using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code
{
    public class Director
    {
        public float[] TimeOfDayPopularity = [
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

        public int MinGuestMoney = 0;
        public int MaxguestMoney = 0;

        private bool _autoSpawnEnabled = false;

        private float _newGuestChance = 0.24f;
        private float _returnGuestChance = 0.42f;

        private float _spawnThrottle = 0;
        private float _spawnThrottleDecay = 0.01f;
        private float _spawnCost = 0.3f;

        private int _maxActiveGuests = 1000;

        public void Update(float deltaTime, bool tick)
        {
            _spawnThrottle -= _spawnThrottleDecay;
            if (_spawnThrottle < 0f)
            {
                _spawnThrottle = 0f;
            }


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

            // TODO: predictable spawning modified by deltaTime
            float timeOfDayPopularity = TimeOfDayPopularity[GameInstance.Instance.Time.GetHour()];
            float rng = Random.Shared.NextSingle();
            if (rng < timeOfDayPopularity)
            {
                DateTime yesterday = GameInstance.Instance.Time.GetDateTime().AddDays(-1);
                List<OffscreenGuest> toSpawn = [];
                foreach (var guest in OffscreenGuests)
                {
                    if (guest.LastVisit <= yesterday && guest.Schedule.TimeInWindow(GameInstance.Instance.Time.GetHour()))
                    {
                        if ((Random.Shared.NextSingle() + _spawnThrottle) < _returnGuestChance)
                        {
                            toSpawn.Add(guest);
                        }
                    }
                }

                foreach (var guest in toSpawn)
                {
                    SpawnOffscreenGuest(guest);
                }

                if ((Random.Shared.NextSingle() + _spawnThrottle) < _newGuestChance) {
                    SpawnNewGuest();
                }
            }
        }

        public Guest SpawnNewGuest()
        {
            OffscreenGuest offscreenGuest = new OffscreenGuest([
                DefaultTraits.GetRandomScheduleTraitForTime(GameInstance.Instance.Time.GetHour()),
                new TraitData()
                {
                    WealthTier = (WealthTier)Random.Shared.Next((int)WealthTier.Low, (int)WealthTier.Premium),
                }
            ]);
            return SpawnOffscreenGuest(offscreenGuest);
        }

        public Guest SpawnOffscreenGuest(OffscreenGuest offscreenGuest)
        {
            _spawnThrottle += _spawnCost;

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
            ImGui.Text($"Throttle: {_spawnThrottle}");
            ImGui.Checkbox("AutoSpawn", ref _autoSpawnEnabled);
            ImGui.DragFloat("SpawnCost", ref _spawnCost, 0.01f, 0f, 10f);
            ImGui.DragFloat("NewGuestChance", ref _newGuestChance, 0.01f, 0f, 1f);
            ImGui.DragFloat("RetGuestChance", ref _returnGuestChance, 0.01f, 0f, 1f);
            ImGui.DragInt("MaxActiveGuests", ref _maxActiveGuests, 1, 0, 5000);

            if (ImGui.Button("Spawn New Guest"))
            {
                SpawnNewGuest();
            }

            if (OffscreenGuests.Count > 0 && ImGui.Button("Spawn Offscreen Guest"))
            {
                SpawnOffscreenGuest(OffscreenGuests[Random.Shared.Next(0, OffscreenGuests.Count)]);
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
