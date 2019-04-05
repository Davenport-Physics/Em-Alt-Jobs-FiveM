﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace AltJobs
{
    class MorgueDelivery : BaseScript
    {
        private readonly Vector3 morgue_point = new Vector3(264.7073f, -1351.095f, 32.93513f);
        private float distance_to_marker;

        private Vector3 marker_pos   = new Vector3(264.7073f, -1351.095f, 30.93513f);
        private Vector3 marker_dir   = new Vector3(0, 0, 0);
        private Vector3 marker_rot   = new Vector3(0, 0, 0);
        private Vector3 marker_scale = new Vector3(4f, 4f, 4f);
        private Color  marker_color  = Color.FromArgb(150, 255, 255, 0);

        private bool job_started = false;

        private Random rand = new Random();

        private int mission_idx  = 0;
        private List<Mission> missions = new List<Mission>()
        {
            new Mission("blackmarket", 2500, 210*1000, new Vector3(3788.968f, 4462.44f, 5.27f), new ItemIdentifiers(new List<string>(){ "sounds/AltJobs/blackmarket-hearts.mp3", "sounds/AltJobs/blackmarket-brain.mp3", "sounds/AltJobs/SandyBM1.mp3" }, new List<string>(){ "Human heart", "Human brain", "Human bones" })),
            new Mission("docks", 1250, 95*1000, new Vector3(166.0794f, -3299.002f, 5.28f), new ItemIdentifiers(new List<string>(){ "sounds/AltJobs/docks-juice.mp3", "sounds/AltJobs/docks-chemicals.mp3", "sounds/AltJobs/SandyD1.mp3" }, new List<string>(){ "Human juice", "Questionable Chemicals", "Embalming Fluid" })),
            new Mission("airport", 1000 , 75*1000, new Vector3(-1022.813f, -2706.281f, 12.607f), new ItemIdentifiers(new List<string>(){ "sounds/AltJobs/SandyA1.mp3", "sounds/AltJobs/airport-teeth.mp3", "sounds/AltJobs/airport-liver.mp3" }, new List<string>(){ "Human Bones", "Human Teeth", "Human Liver" })),
            new Mission("humanelabs", 2500 , 230*1000, new Vector3(3568.353f, 3664.556f, 33.20224f), new ItemIdentifiers(new List<string>(){ "sounds/AltJobs/SandyA1.mp3", "sounds/AltJobs/SandyA1.mp3", "sounds/AltJobs/humanelabs-brain.mp3" }, new List<string>(){ "Human juice", "Questionable Chemicals", "Human brain" })),
            new Mission("nudistcolony", 3500 , 290*1000, new Vector3(-1097.769f, 4945.581f, 217.5335f), new ItemIdentifiers(new List<string>(){ "sounds/AltJobs/nudistcolony-bones.mp3", "sounds/AltJobs/SandyA1.mp3", "sounds/AltJobs/nudistcolony-juice.mp3" }, new List<string>(){ "Human Bones", "Human Teeth", "Human juice" }))
        };

        public MorgueDelivery()
        {
            CheckDistanceToPoint();
        }

        private async void CheckDistanceToPoint()
        {
            while (true)
            {
                this.distance_to_marker = Vector3.Distance(Game.PlayerPed.Position, this.morgue_point);
                if (this.distance_to_marker >= 75)
                {
                    await Delay(5000);
                }
                else
                {
                    await CheckForNewJobOrExisting();
                }
            }
        }

        private async Task CheckForNewJobOrExisting()
        {
            if (this.job_started)
            {
                this.job_started = !this.missions[this.mission_idx].IsJobDone();
                await Delay(1000);
            }
            else
            {
                await Delay(5);
                HandleDrawingAndInput();
            }
        }

        private void HandleDrawingAndInput()
        {
            DrawSceneMarkerIfNeeded();
            DrawTextIfNecessary();
            HandleInput();
        }

        private void DrawSceneMarkerIfNeeded()
        {
            if (this.distance_to_marker <= 25)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, this.marker_pos, this.marker_dir, this.marker_rot, this.marker_scale, this.marker_color);
            }
        }

        private void DrawTextIfNecessary()
        {
            if (this.distance_to_marker <= 3 && !this.job_started)
            {
                Shared.DrawTextSimple("Press ~g~Enter~w~ to start job.");
            }
        }

        private void HandleInput()
        {
            if (this.distance_to_marker <= 3 && !this.job_started && API.IsControlJustPressed(1, 18))
            {
                if (API.IsPedInAnyVehicle(Game.PlayerPed.Handle, false) && API.GetPedInVehicleSeat(API.GetVehiclePedIsIn(Game.PlayerPed.Handle, false), -1) == Game.PlayerPed.Handle)
                {
                    StartJob();
                } else
                {
                    TriggerEvent("ShowInformationLeft", 5000, "You must be in a vehicle.");
                }
            }
        }

        private void StartJob()
        {
            this.job_started = true;
            this.mission_idx = rand.Next(this.missions.Count);
            this.missions[this.mission_idx].RunJob();
        }

    }

    public class Mission : BaseScript
    {
        public readonly int base_pay;
        public readonly int base_timer;
        public readonly string mission_name;
        public readonly Vector3 end_point;
        public readonly ItemIdentifiers items;
        
        private static Random rand = new Random();

        private bool is_job_done = false;

        private int mission_timer;
        private int mission_item;
        private string mission_sound_file;
        private string mission_item_name;

        private float distance_to_end;

        private static Vector3 marker_dir = new Vector3(0, 0, 0);
        private static Vector3 marker_rot = new Vector3(0, 0, 0);
        private static Vector3 marker_scale = new Vector3(4f, 4f, 4f);
        private static Color marker_color = Color.FromArgb(150, 255, 255, 0);

        public Mission()
        {

        }

        public Mission(string mission_name, int base_pay, int base_timer, Vector3 end_point, ItemIdentifiers items)
        {
            this.mission_name = mission_name;
            this.base_pay     = base_pay;
            this.base_timer   = base_timer;
            this.end_point    = end_point;
            this.items        = items;
        }

        public async void RunJob()
        {
            await InitJob();
            while (!is_job_done)
            {
                await Delay(5);
                DrawTimeLeft();
                DrawEndPointIfNearby();
                HandleIfPlayerIsOnMarker();
                CheckIfRanOutOfTime();
            }
        }

        public async Task InitJob()
        {
            this.is_job_done        = false;
            this.mission_item       = rand.Next(this.items.item_names.Count);
            this.mission_sound_file = this.items.sound_files[this.mission_item];
            this.mission_item_name  = this.items.item_names[this.mission_item];

            TriggerEvent("addItem", this.mission_item_name, 1, true);

            PlayMissionDialog();
            await FadeOut();
            SetTimer();
            API.SetNewWaypoint(this.end_point[0], this.end_point[1]);
        }

        private void PlayMissionDialog()
        {
            Exports["PlayExternalSounds"].PlaySound(this.mission_sound_file, .3f);
        }

        private async Task FadeOut()
        {
            API.DoScreenFadeOut(1000);
            await Delay(7500);
            API.DoScreenFadeIn(2500);
        }

        public bool IsJobDone()
        {
            return this.is_job_done;
        }

        private void DrawTimeLeft()
        {
            int diff = (int)((this.mission_timer - API.GetGameTimer()) * 0.001);
            if (diff >= 120)
            {
                Shared.DrawTextSimple(string.Format("~g~{0}~w~ seconds left", diff));
            }
            else if (diff >= 60)
            {
                Shared.DrawTextSimple(string.Format("~y~{0}~w~ seconds left", diff));
            }
            else
            {
                Shared.DrawTextSimple(string.Format("~r~{0}~w~ seconds left", diff));
            }
        }

        private void CheckIfRanOutOfTime()
        {
            if (API.GetGameTimer() >= this.mission_timer)
            {
                this.is_job_done = true;
                TriggerEvent("ShowInformationLeft", 3000, string.Format("You have failed to deliver {0} in time.", this.mission_item_name));
                API.ClearGpsPlayerWaypoint();
            }
        }

        private void DrawEndPointIfNearby()
        {
            this.distance_to_end = Vector3.Distance(Game.PlayerPed.Position, this.end_point);
            if (this.distance_to_end <= 30)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, this.end_point, Mission.marker_dir, Mission.marker_rot, Mission.marker_scale, Mission.marker_color);
            }
        }

        private void SetTimer()
        {
            this.mission_timer = API.GetGameTimer() + this.base_timer;
        }

        private void HandleIfPlayerIsOnMarker()
        {
            if (this.distance_to_end <= 3.0)
            {
                this.is_job_done = true;
                TriggerEvent("ShowInformationLeft", 2500, string.Format("You have successfully delivered {0}", this.mission_item_name));
                TriggerEvent("removeItem", this.mission_item_name, 1);
                TriggerEvent("addMoney", CalcPayout());
            }
        }

        private int CalcPayout()
        {
            return this.base_pay + (int)((this.mission_timer - API.GetGameTimer()) * .01);
        }

    }

    public struct ItemIdentifiers
    {
        public readonly List<string> sound_files;
        public readonly List<string> item_names;

        public ItemIdentifiers(List<string> sound_files, List<string> item_names)
        {
            this.sound_files = sound_files;
            this.item_names  = item_names;
        }
    }
}
